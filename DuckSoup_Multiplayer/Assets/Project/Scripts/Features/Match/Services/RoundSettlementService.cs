
using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.Match.Contracts;
using DuckSoup.Features.Match.Models;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.Match.Services
{
    /// <summary>
    /// Calcula las recompensas de una ronda,
    /// actualiza el progreso económico
    /// y determina si termina la partida.
    /// </summary>
    public sealed class RoundSettlementService :
        IRoundSettlementService
    {
        private readonly MatchModel match;
        private readonly RestaurantProgressionPolicy progression;
        private readonly int coinsPerCompletedRecipe;

        private readonly Dictionary<string, RoundSettlementResult>
            settledRounds =
                new Dictionary<string, RoundSettlementResult>(
                    StringComparer.Ordinal);

        public RoundSettlementService(
            MatchModel match,
            RestaurantProgressionPolicy progression,
            int coinsPerCompletedRecipe)
        {
            this.match = match ??
                throw new ArgumentNullException(nameof(match));

            this.progression = progression ??
                throw new ArgumentNullException(nameof(progression));

            if (coinsPerCompletedRecipe <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(coinsPerCompletedRecipe));

            this.coinsPerCompletedRecipe =
                coinsPerCompletedRecipe;
        }

        public RoundSettlementResult Settle(
            RoundModel round,
            PlayerId roundWinner)
        {
            if (round == null)
                throw new ArgumentNullException(nameof(round));

            // Si ya fue liquidada, devolver el mismo
            // resultado SIN volver a sumar monedas.
            if (settledRounds.TryGetValue(
                round.Id, out RoundSettlementResult previous))
            {
                return previous;
            }

            if (match.IsFinished)
                throw new InvalidOperationException(
                    "No se pueden liquidar más rondas.");

            if (roundWinner != PlayerId.Player1 &&
                roundWinner != PlayerId.Player2)
            {
                throw new ArgumentException(
                    "El ganador de ronda no es válido.");
            }

            bool winnerCompleted = roundWinner == PlayerId.Player1
                ? round.Player1.HasCompletedAllCards
                : round.Player2.HasCompletedAllCards;

            if (!winnerCompleted)
                throw new InvalidOperationException(
                    "El ganador debe completar sus recetas.");

            // Ambos jugadores reciben monedas
            // por sus recetas terminadas.
            int earned1 = checked(
                round.Player1.CompletedCardCount *
                coinsPerCompletedRecipe);

            int earned2 = checked(
                round.Player2.CompletedCardCount *
                coinsPerCompletedRecipe);

            // Comprobar ambos importes antes de modificar
            // el estado, evitando una aplicación parcial
            // por desbordamiento de enteros.
            if (match.Player1.TotalCoins >
                    int.MaxValue - earned1 ||
                match.Player2.TotalCoins >
                    int.MaxValue - earned2)
            {
                throw new OverflowException(
                    "La cantidad de monedas supera el límite.");
            }

            match.Player1.AddCoins(earned1);
            match.Player2.AddCoins(earned2);

            int level1 = progression.GetLevel(
                match.Player1.TotalCoins);

            int level2 = progression.GetLevel(
                match.Player2.TotalCoins);

            bool max1 = level1 == progression.MaxLevel;
            bool max2 = level2 == progression.MaxLevel;

            bool matchFinished = max1 || max2;
            PlayerId matchWinner = PlayerId.None;

            if (max1 && !max2)
            {
                matchWinner = PlayerId.Player1;
            }
            else if (max2 && !max1)
            {
                matchWinner = PlayerId.Player2;
            }
            else if (max1 && max2)
            {
                // Desempate provisional:
                // gana quien tenga más monedas.
                if (match.Player1.TotalCoins >
                    match.Player2.TotalCoins)
                {
                    matchWinner = PlayerId.Player1;
                }
                else if (match.Player2.TotalCoins >
                         match.Player1.TotalCoins)
                {
                    matchWinner = PlayerId.Player2;
                }
                // Si tienen exactamente lo mismo,
                // la partida queda empatada.
            }

            match.RegisterSettlement(
                matchFinished,
                matchWinner);

            var player1 = new PlayerEconomySnapshot(
                PlayerId.Player1,
                earned1,
                match.Player1.TotalCoins,
                level1,
                progression.GetNextThreshold(level1));

            var player2 = new PlayerEconomySnapshot(
                PlayerId.Player2,
                earned2,
                match.Player2.TotalCoins,
                level2,
                progression.GetNextThreshold(level2));

            var result = new RoundSettlementResult(
                round.Id,
                match.CompletedRounds,
                roundWinner,
                matchWinner,
                matchFinished,
                progression.MaxLevel,
                player1,
                player2);

            settledRounds.Add(round.Id, result);

            return result;
        }
    }
}
