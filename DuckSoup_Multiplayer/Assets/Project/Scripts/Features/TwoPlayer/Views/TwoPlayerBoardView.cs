using DuckSoup.Features.Audio;
using DuckSoup.Features.PowerUps;
using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.Art;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Views;
using DuckSoup.Features.TwoPlayer.Models;
using DuckSoup.Features.TwoPlayer.Services;
using UnityEngine;

namespace DuckSoup.Features.TwoPlayer.Views
{
    /// <summary>Vista del tablero 2P: 6 cartas y 18 posiciones permanentes.</summary>
    public sealed class TwoPlayerBoardView : MonoBehaviour
    {
        [Header("Player Cards")]
        [SerializeField] private Transform player1CardsRoot;
        [SerializeField] private Transform player2CardsRoot;
        [Header("Shared Table")]
        [SerializeField] private Transform tokensRoot;
        [SerializeField] private RectTransform dragLayer;
        [Header("Prefabs")]
        [SerializeField] private RecipeCardView recipeCardPrefab;
        [SerializeField] private TwoPlayerTokenView tokenPrefab;

        [Header("Game Feel - optional deal points (left, center, right)")]
        [SerializeField] private RectTransform[] player1DealOrigins = new RectTransform[3];
        [SerializeField] private RectTransform[] player2DealOrigins = new RectTransform[3];

        private readonly List<RecipeCardView> cards1 = new List<RecipeCardView>();
        private readonly List<RecipeCardView> cards2 = new List<RecipeCardView>();
        private readonly List<TwoPlayerTokenView> tokenViews = new List<TwoPlayerTokenView>();
        private readonly Dictionary<int, PlayerId> pointerActors =
            new Dictionary<int, PlayerId>();
        private TwoPlayerSession session;
        private bool inputEnabled;
        private PowerUpHud powerUpHud;

        public void SetPowerUpHud(PowerUpHud hud) { powerUpHud = hud; }
        private bool initialized;

        public event Action<PlayerId> RecipeCompleted;

        public void Initialize(TwoPlayerSession newSession)
        {
            if (initialized) throw new InvalidOperationException("El tablero ya esta inicializado.");
            session = newSession ?? throw new ArgumentNullException(nameof(newSession));
            ValidateReferences();
            TwoPlayerSnapshot snapshot = session.GetSnapshot();

            foreach (RecipeCardSnapshot card in snapshot.Player1Cards)
            {
                RecipeCardView view = Instantiate(recipeCardPrefab, player1CardsRoot);
                EnsureCardFeel(view);
                cards1.Add(view);
            }
            foreach (RecipeCardSnapshot card in snapshot.Player2Cards)
            {
                RecipeCardView view = Instantiate(recipeCardPrefab, player2CardsRoot);
                EnsureCardFeel(view);
                cards2.Add(view);
            }

            // El Grid organiza los TokenSlot, nunca directamente los tokens.
            for (int i = 0; i < TwoPlayerSession.TokenSlots; i++)
            {
                var slot = new GameObject($"TokenSlot_{i:00}", typeof(RectTransform));
                RectTransform slotRect = slot.GetComponent<RectTransform>();
                slotRect.SetParent(tokensRoot, false);

                TwoPlayerTokenView token = Instantiate(tokenPrefab, slotRect);
                RectTransform tokenRect = token.GetComponent<RectTransform>();
                tokenRect.anchorMin = Vector2.zero;
                tokenRect.anchorMax = Vector2.one;
                tokenRect.offsetMin = Vector2.zero;
                tokenRect.offsetMax = Vector2.zero;
                tokenRect.localScale = Vector3.one;
                token.Initialize(this, dragLayer);
                tokenViews.Add(token);
            }

            initialized = true;
            Refresh();
            SetInputEnabled(false);
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled) CancelAllDrags();
        }

        public bool TryBeginToken(string tokenId, int pointerId)
        {
            if (!inputEnabled || session == null) return false;
            bool accepted = session.TryBeginGesture(tokenId, pointerId);
            if (accepted)
            {
                Refresh();
                DuckSoupSoundPlayer.Play(DuckSoupSoundId.IngredientTap);
            }
            return accepted;
        }

        // Remember who actually touched the shared token so the lock
        // penalizes the player making the mistake, not the destination.
        public bool TryBeginToken(string tokenId, int pointerId, Vector2 screenPosition)
        {
            if (!CanTouchAt(screenPosition)) return false;
            PlayerId actor = screenPosition.y >= Screen.height * 0.5f
                ? PlayerId.Player1 : PlayerId.Player2;
            bool accepted = TryBeginToken(tokenId, pointerId);
            if (!accepted) return false;
            if (session.GetPowerKind(tokenId) == PowerTokenKind.Poison)
            {
                bool removedTick;
                session.TryTouchPoison(tokenId, pointerId, actor, out removedTick);
                Refresh();
                powerUpHud?.ShowFeedback(removedTick
                    ? "POISON! -1 INGREDIENT" : "POISON!");
                DuckSoupSoundPlayer.Play(DuckSoupSoundId.IncorrectIngredient);
                return false;
            }
            pointerActors[pointerId] = actor;
            return true;
        }

        // Used by the token view before reserving a token from the touch side.
        public bool CanTouchAt(Vector2 screenPosition)
        {
            PlayerId player = screenPosition.y >= Screen.height * 0.5f
                ? PlayerId.Player1 : PlayerId.Player2;
            return !session.IsPlayerLocked(player);
        }

        public void NotifyPowerApplied(RecipeCardView card, bool completed)
        {
            Refresh();
            if (!completed) return;
            if (session.PowerUpsEnabled)
                card.GetComponent<RecipeCardFeel>()?.CelebrateCombo();
            else
                card.GetComponent<RecipeCardFeel>()?.Complete();
            DuckSoupSoundPlayer.Play(DuckSoupSoundId.RecipeCompleted);
            RecipeCompleted?.Invoke(card.Owner);
        }

        // Devuelve true si el ingrediente se entrego con exito.
        public bool FinishTokenGesture(
            string tokenId, int pointerId, Vector2 screenPoint,
            Camera eventCamera, bool wasDragged)
        {
            if (session == null) return false;
            bool completed = false;
            bool accepted = false;
            RecipeCardView target = null;
            PlayerId completedBy = PlayerId.None;
            PlayerId actor = pointerActors.TryGetValue(pointerId, out PlayerId starter)
                ? starter : PlayerId.None;
            pointerActors.Remove(pointerId);
            PowerTokenKind special = session.GetPowerKind(tokenId);

            bool actorLocked = actor != PlayerId.None && session.IsPlayerLocked(actor);
            if (actorLocked)
                session.CancelGesture(tokenId, pointerId);
            else if (inputEnabled && wasDragged &&
                     TryFindTargetCard(screenPoint, eventCamera, out target))
            {
                // Opponent's cards must not be affected by this touch.
                if (actor == PlayerId.None || actor != target.Owner)
                    session.CancelGesture(tokenId, pointerId);
                else if (special != PowerTokenKind.None)
                    accepted = session.TryDeliverSpecial(tokenId, pointerId,
                        actor, target.CardId, out completed);
                else
                    accepted = session.TryDeliver(tokenId, pointerId,
                        actor, target.CardId, out completed);
                if (completed) completedBy = actor;
            }
            else
                session.CancelGesture(tokenId, pointerId);

            Refresh();
            if (target != null)
            {
                RecipeCardFeel feel = target.GetComponent<RecipeCardFeel>();
                if (!accepted)
                {
                    feel?.ShakeWrong();
                    if (!actorLocked && actor != PlayerId.None)
                        session.ApplyWrongDeliveryPenalty(actor);
                }
                if (completed)
                {
                    if (session.PowerUpsEnabled) feel?.CelebrateCombo();
                    else feel?.Complete();
                }
                DuckSoupSoundPlayer.Play(accepted
                    ? DuckSoupSoundId.CorrectIngredient
                    : DuckSoupSoundId.IncorrectIngredient);
                if (accepted && special != PowerTokenKind.None)
                    powerUpHud?.ShowFeedback(special == PowerTokenKind.ExtraComboTime
                        ? "COMBO TIME EXTENDED!" : "POWER-UP!");
            }
            if (completed)
            {
                DuckSoupSoundPlayer.Play(DuckSoupSoundId.RecipeCompleted);
                RecipeCompleted?.Invoke(completedBy);
            }
            return accepted;
        }

        public void CancelTokenGesture(string tokenId, int pointerId)
        {
            pointerActors.Remove(pointerId);
            session?.CancelGesture(tokenId, pointerId);
        }

        private bool TryFindTargetCard(
            Vector2 screenPoint, Camera camera, out RecipeCardView target)
        {
            foreach (RecipeCardView card in cards1)
            {
                if (card.ContainsScreenPoint(screenPoint, camera))
                {
                    target = card;
                    return true;
                }
            }
            foreach (RecipeCardView card in cards2)
            {
                if (card.ContainsScreenPoint(screenPoint, camera))
                {
                    target = card;
                    return true;
                }
            }
            target = null;
            return false;
        }

        public void Refresh()
        {
            if (!initialized) return;
            TwoPlayerSnapshot snap = session.GetSnapshot();
            for (int c = 0; c < TwoPlayerSession.CardsPerPlayer; c++)
            {
                cards1[c].Bind(snap.Player1Cards[c], PlayerId.Player1);
                cards2[c].Bind(snap.Player2Cards[c], PlayerId.Player2);
                cards1[c].GetComponent<RecipeCardArtView>()?.Render(snap.Player1Cards[c]);
                cards2[c].GetComponent<RecipeCardArtView>()?.Render(snap.Player2Cards[c]);
                cards1[c].GetComponent<RecipeCardFeel>()?.ObserveCard(
                    snap.Player1Cards[c].CardId, DealOrigin(player1DealOrigins, c), c);
                cards2[c].GetComponent<RecipeCardFeel>()?.ObserveCard(
                    snap.Player2Cards[c].CardId, DealOrigin(player2DealOrigins, c), c);
            }
            for (int i = 0; i < TwoPlayerSession.TokenSlots; i++)
                tokenViews[i].Refresh(snap.Tokens[i],
                    snap.Tokens[i] == null ? PowerTokenKind.None
                    : session.GetPowerKind(snap.Tokens[i].Id));
        }

        private static void EnsureCardFeel(RecipeCardView card)
        {
            if (card.GetComponent<RecipeCardFeel>() == null)
                card.gameObject.AddComponent<RecipeCardFeel>();
        }

        private static RectTransform DealOrigin(RectTransform[] origins, int index)
        {
            return origins != null && index < origins.Length ? origins[index] : null;
        }

        public void CancelAllDrags()
        {
            foreach (TwoPlayerTokenView token in tokenViews)
                if (token != null) token.ForceCancel();
            pointerActors.Clear();
            session?.CancelAllGestures();
        }

        private void ValidateReferences()
        {
            if (player1CardsRoot == null || player2CardsRoot == null ||
                tokensRoot == null || dragLayer == null ||
                recipeCardPrefab == null || tokenPrefab == null)
                throw new InvalidOperationException("Faltan referencias en TwoPlayerBoardView.");
        }

        private void OnDisable() => CancelAllDrags();
    }
}
