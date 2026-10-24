
using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.Round.Services
{
    /// <summary>
    /// Servicio de interacción de rondas.
    ///
    /// Gestiona reservas, revelado temporal,
    /// entregas de ingredientes y victoria.
    /// </summary>
    public sealed class RoundInteractionService :
        IRoundInteractionService
    {
        private sealed class RevealTimer
        {
            public float RemainingSeconds;

            public RevealTimer(float seconds)
            {
                RemainingSeconds = seconds;
            }
        }

        private readonly RoundModel round;
        private readonly float revealDurationSeconds;

        private readonly Dictionary<string, int>
            tokenReservations = new Dictionary<string, int>();

        private readonly Dictionary<int, string>
            pointerReservations = new Dictionary<int, string>();

        // Cada token tiene su propio temporizador.
        private readonly Dictionary<string, RevealTimer>
            revealTimers =
                new Dictionary<string, RevealTimer>();

        // Lista reutilizable para evitar modificar
        // el diccionario mientras lo recorremos.
        private readonly List<string> expiredTokens =
            new List<string>();

        public bool IsFinished { get; private set; }

        public PlayerId Winner { get; private set; } =
            PlayerId.None;

        public RoundInteractionService(
            RoundModel round,
            float revealDurationSeconds = 3f)
        {
            this.round = round ??
                throw new ArgumentNullException(nameof(round));

            if (revealDurationSeconds <= 0f ||
                float.IsNaN(revealDurationSeconds) ||
                float.IsInfinity(revealDurationSeconds))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(revealDurationSeconds));
            }

            this.revealDurationSeconds =
                revealDurationSeconds;
        }

        /// <summary>
        /// Reserva y revela un ingrediente.
        /// Detiene su temporizador mientras
        /// un dedo lo está utilizando.
        /// </summary>
        public bool TryBeginGesture(
            string tokenId,
            int pointerId)
        {
            if (IsFinished ||
                string.IsNullOrWhiteSpace(tokenId))
            {
                return false;
            }

            if (tokenReservations.ContainsKey(tokenId) ||
                pointerReservations.ContainsKey(pointerId))
            {
                return false;
            }

            if (!round.TryGetToken(
                tokenId, out IngredientToken token))
            {
                return false;
            }

            if (token.IsConsumed)
                return false;

            if (!token.IsRevealed && !token.TryReveal())
                return false;

            // Detenemos cualquier cuenta atrás
            // anterior de este ingrediente.
            revealTimers.Remove(tokenId);

            tokenReservations.Add(tokenId, pointerId);
            pointerReservations.Add(pointerId, tokenId);

            return true;
        }

        /// <summary>
        /// Entrega un ingrediente a una receta.
        /// Una entrega incorrecta no consume el token.
        /// </summary>
        public bool TryDeliver(
            string tokenId,
            int pointerId,
            PlayerId playerId,
            string cardId)
        {
            if (!HasReservation(tokenId, pointerId))
                return false;

            try
            {
                if (IsFinished)
                    return false;

                if (!round.TryGetToken(
                    tokenId, out IngredientToken token))
                {
                    return false;
                }

                if (!round.TryGetPlayer(
                    playerId, out RoundPlayerModel player))
                {
                    return false;
                }

                if (!round.TryGetCard(
                    playerId,
                    cardId,
                    out RecipeCardModel card))
                {
                    return false;
                }

                if (!token.IsRevealed || token.IsConsumed)
                    return false;

                if (!card.Progress.NeedsIngredient(
                    token.Ingredient))
                {
                    return false;
                }

                if (!token.TryConsume())
                    return false;

                bool added = card.Progress.TryAddIngredient(
                    token.Ingredient);

                if (!added)
                {
                    throw new InvalidOperationException(
                        "Error al actualizar la receta.");
                }

                // El token consumido ya no necesita
                // ningún temporizador.
                revealTimers.Remove(tokenId);

                if (player.HasCompletedAllCards)
                {
                    Winner = playerId;
                    IsFinished = true;

                    CancelAllGestures();
                }

                return true;
            }
            finally
            {
                CancelGesture(tokenId, pointerId);
            }
        }

        /// <summary>
        /// Libera el token y comienza la cuenta atrás
        /// si sigue revelado y disponible.
        /// </summary>
        public void CancelGesture(
            string tokenId,
            int pointerId)
        {
            if (!HasReservation(tokenId, pointerId))
                return;

            tokenReservations.Remove(tokenId);
            pointerReservations.Remove(pointerId);

            if (IsFinished)
                return;

            StartHideTimer(tokenId);
        }

        /// <summary>
        /// Libera todas las reservas activas.
        /// </summary>
        public void CancelAllGestures()
        {
            if (!IsFinished)
            {
                foreach (string tokenId
                         in tokenReservations.Keys)
                {
                    StartHideTimer(tokenId);
                }
            }

            tokenReservations.Clear();
            pointerReservations.Clear();

            if (IsFinished)
            {
                revealTimers.Clear();
            }
        }

        /// <summary>
        /// Comienza o reinicia la cuenta atrás
        /// de un token liberado.
        /// </summary>
        private void StartHideTimer(string tokenId)
        {
            if (!round.TryGetToken(
                tokenId, out IngredientToken token))
            {
                return;
            }

            if (token.IsConsumed || !token.IsRevealed)
            {
                revealTimers.Remove(tokenId);
                return;
            }

            revealTimers[tokenId] =
                new RevealTimer(revealDurationSeconds);
        }

        /// <summary>
        /// Actualiza todos los temporizadores.
        ///
        /// Devuelve true si uno o varios tokens
        /// han vuelto a ponerse boca abajo.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (IsFinished || deltaTime <= 0f)
                return false;

            if (float.IsNaN(deltaTime) ||
                float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime));
            }

            if (revealTimers.Count == 0)
                return false;

            expiredTokens.Clear();

            foreach (KeyValuePair<string, RevealTimer>
                     entry in revealTimers)
            {
                // Por seguridad, nunca ocultamos
                // una ficha que está reservada.
                if (tokenReservations.ContainsKey(entry.Key))
                    continue;

                entry.Value.RemainingSeconds -= deltaTime;

                if (entry.Value.RemainingSeconds <= 0f)
                {
                    expiredTokens.Add(entry.Key);
                }
            }

            bool visualStateChanged = false;

            foreach (string tokenId in expiredTokens)
            {
                revealTimers.Remove(tokenId);

                if (round.TryGetToken(
                    tokenId, out IngredientToken token))
                {
                    if (token.TryHide())
                    {
                        visualStateChanged = true;
                    }
                }
            }

            expiredTokens.Clear();

            return visualStateChanged;
        }

        private bool HasReservation(
            string tokenId,
            int pointerId)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            return tokenReservations.TryGetValue(
                tokenId, out int ownerPointerId)
                && ownerPointerId == pointerId
                && pointerReservations.TryGetValue(
                    pointerId, out string reservedTokenId)
                && reservedTokenId == tokenId;
        }
    }
}
