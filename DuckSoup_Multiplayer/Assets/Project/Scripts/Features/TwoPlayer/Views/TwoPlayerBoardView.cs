using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.Audio;
using DuckSoup.Features.Art;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.PowerUps;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Views;
using DuckSoup.Features.TwoPlayer.Models;
using DuckSoup.Features.TwoPlayer.Services;
using UnityEngine;

namespace DuckSoup.Features.TwoPlayer.Views
{
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

        [Header("Lock Feedback")]
        [SerializeField] private Sprite cardLockIcon;

        [Header("Game Feel")]
        [SerializeField]
        private RectTransform[] player1DealOrigins =
            new RectTransform[3];

        [SerializeField]
        private RectTransform[] player2DealOrigins =
            new RectTransform[3];

        private readonly List<RecipeCardView> cards1 =
            new List<RecipeCardView>();

        private readonly List<RecipeCardView> cards2 =
            new List<RecipeCardView>();

        private readonly List<TwoPlayerTokenView> tokenViews =
            new List<TwoPlayerTokenView>();

        private readonly Dictionary<int, PlayerId> pointerActors =
            new Dictionary<int, PlayerId>();

        private TwoPlayerSession session;
        private PowerUpHud powerUpHud;

        private bool inputEnabled;
        private bool initialized;

        public event Action<PlayerId> RecipeCompleted;

        public void SetPowerUpHud(PowerUpHud hud)
        {
            powerUpHud = hud;
        }

        public void Initialize(TwoPlayerSession newSession)
        {
            if (initialized)
            {
                throw new InvalidOperationException(
                    "El tablero ya esta inicializado.");
            }

            session = newSession ??
                throw new ArgumentNullException(nameof(newSession));

            ValidateReferences();

            TwoPlayerSnapshot snapshot = session.GetSnapshot();

            foreach (RecipeCardSnapshot card in snapshot.Player1Cards)
            {
                RecipeCardView view =
                    Instantiate(recipeCardPrefab, player1CardsRoot);

                EnsureCardFeel(view);
                cards1.Add(view);
            }

            foreach (RecipeCardSnapshot card in snapshot.Player2Cards)
            {
                RecipeCardView view =
                    Instantiate(recipeCardPrefab, player2CardsRoot);

                EnsureCardFeel(view);
                cards2.Add(view);
            }

            for (int i = 0; i < TwoPlayerSession.TokenSlots; i++)
            {
                var slot = new GameObject(
                    $"TokenSlot_{i:00}",
                    typeof(RectTransform));

                RectTransform slotRect =
                    slot.GetComponent<RectTransform>();

                slotRect.SetParent(tokensRoot, false);

                TwoPlayerTokenView token =
                    Instantiate(tokenPrefab, slotRect);

                RectTransform tokenRect =
                    token.GetComponent<RectTransform>();

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

            if (!enabled)
                CancelAllDrags();
        }

        public bool TryBeginToken(
            string tokenId,
            int pointerId)
        {
            if (!inputEnabled || session == null)
                return false;

            bool accepted =
                session.TryBeginGesture(tokenId, pointerId);

            if (accepted)
            {
                Refresh();
                DuckSoupSoundPlayer.Play(DuckSoupSoundId.IngredientTap);
            }

            return accepted;
        }

        public bool TryBeginToken(
            string tokenId,
            int pointerId,
            Vector2 screenPosition)
        {
            if (!CanTouchAt(screenPosition))
                return false;

            PlayerId actor =
                screenPosition.y >= Screen.height * 0.5f
                    ? PlayerId.Player1
                    : PlayerId.Player2;

            bool accepted = TryBeginToken(tokenId, pointerId);

            if (!accepted)
                return false;

            pointerActors[pointerId] = actor;
            return true;
        }

        public bool CanTouchAt(Vector2 screenPosition)
        {
            return inputEnabled &&
                   session != null &&
                   !session.IsTimeUp;
        }

        public void NotifyPowerApplied(
            RecipeCardView card,
            bool completed)
        {
            Refresh();

            if (!completed)
                return;

            if (session.PowerUpsEnabled)
                card.GetComponent<RecipeCardFeel>()?.CelebrateCombo();
            else
                card.GetComponent<RecipeCardFeel>()?.Complete();

            DuckSoupSoundPlayer.Play(DuckSoupSoundId.RecipeCompleted);
            RecipeCompleted?.Invoke(card.Owner);
        }

        public bool FinishTokenGesture(
            string tokenId,
            int pointerId,
            Vector2 screenPoint,
            Camera eventCamera,
            bool wasDragged)
        {
            if (session == null)
                return false;

            bool completed = false;
            bool accepted = false;
            bool cardWasLocked = false;

            RecipeCardView target = null;
            PlayerId completedBy = PlayerId.None;

            pointerActors.Remove(pointerId);

            PowerTokenKind special = session.GetPowerKind(tokenId);

            if (inputEnabled &&
                wasDragged &&
                TryFindTargetCard(screenPoint, eventCamera, out target))
            {
                cardWasLocked = session.IsCardLocked(
                    target.Owner,
                    target.CardId);

                if (cardWasLocked)
                {
                    session.CancelGesture(tokenId, pointerId);
                    powerUpHud?.ShowFeedback("CARD LOCKED!");
                }
                else
                {
                    bool wrongIngredient =
                        special == PowerTokenKind.None &&
                        session.IsWrongIngredientForCard(
                            tokenId,
                            target.Owner,
                            target.CardId);

                    if (special != PowerTokenKind.None)
                    {
                        accepted = session.TryDeliverSpecial(
                            tokenId,
                            pointerId,
                            target.Owner,
                            target.CardId,
                            out completed);
                    }
                    else
                    {
                        accepted = session.TryDeliver(
                            tokenId,
                            pointerId,
                            target.Owner,
                            target.CardId,
                            out completed);
                    }

                    if (!accepted && wrongIngredient)
                    {
                        bool locked = session.ApplyWrongDeliveryPenalty(
                            target.Owner,
                            target.CardId);

                        if (locked)
                            powerUpHud?.ShowFeedback("CARD LOCKED!");
                    }
                }

                if (completed)
                    completedBy = target.Owner;
            }
            else
            {
                session.CancelGesture(tokenId, pointerId);
            }

            Refresh();

            if (target != null)
            {
                RecipeCardFeel feel =
                    target.GetComponent<RecipeCardFeel>();

                if (!accepted && !cardWasLocked)
                    feel?.ShakeWrong();

                if (completed)
                {
                    if (session.PowerUpsEnabled)
                        feel?.CelebrateCombo();
                    else
                        feel?.Complete();
                }

                DuckSoupSoundPlayer.Play(
                    accepted
                        ? DuckSoupSoundId.CorrectIngredient
                        : DuckSoupSoundId.IncorrectIngredient);

                if (accepted && special != PowerTokenKind.None)
                {
                    powerUpHud?.ShowFeedback(
                        special == PowerTokenKind.ExtraComboTime
                            ? "COMBO TIME EXTENDED!"
                            : special == PowerTokenKind.Poison
                                ? "POOP! -1 INGREDIENT"
                                : "POWER-UP!");
                }
            }

            if (completed)
            {
                DuckSoupSoundPlayer.Play(DuckSoupSoundId.RecipeCompleted);
                RecipeCompleted?.Invoke(completedBy);
            }

            return accepted;
        }

        public void CancelTokenGesture(
            string tokenId,
            int pointerId)
        {
            pointerActors.Remove(pointerId);
            session?.CancelGesture(tokenId, pointerId);
        }

        private bool TryFindTargetCard(
            Vector2 screenPoint,
            Camera camera,
            out RecipeCardView target)
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
            if (!initialized)
                return;

            TwoPlayerSnapshot snapshot = session.GetSnapshot();

            for (int i = 0; i < TwoPlayerSession.CardsPerPlayer; i++)
            {
                cards1[i].Bind(snapshot.Player1Cards[i], PlayerId.Player1);
                cards2[i].Bind(snapshot.Player2Cards[i], PlayerId.Player2);

                cards1[i].GetComponent<RecipeCardArtView>()?
                    .Render(snapshot.Player1Cards[i]);

                cards2[i].GetComponent<RecipeCardArtView>()?
                    .Render(snapshot.Player2Cards[i]);

                cards1[i].GetComponent<RecipeCardFeel>()?.ObserveCard(
                    snapshot.Player1Cards[i].CardId,
                    DealOrigin(player1DealOrigins, i),
                    i);

                cards2[i].GetComponent<RecipeCardFeel>()?.ObserveCard(
                    snapshot.Player2Cards[i].CardId,
                    DealOrigin(player2DealOrigins, i),
                    i);
            }

            for (int i = 0; i < TwoPlayerSession.TokenSlots; i++)
            {
                tokenViews[i].Refresh(
                    snapshot.Tokens[i],
                    snapshot.Tokens[i] == null
                        ? PowerTokenKind.None
                        : session.GetPowerKind(snapshot.Tokens[i].Id));
            }
        }

        private void EnsureCardFeel(RecipeCardView card)
        {
            if (card.GetComponent<RecipeCardFeel>() == null)
                card.gameObject.AddComponent<RecipeCardFeel>();

            RecipeCardLockOverlay overlay =
                card.GetComponent<RecipeCardLockOverlay>();

            if (overlay == null)
                overlay = card.gameObject.AddComponent<RecipeCardLockOverlay>();

            overlay.Configure(cardLockIcon);
        }

        private void Update()
        {
            if (!initialized || session == null)
                return;

            UpdateCardLocks(cards1);
            UpdateCardLocks(cards2);
        }

        private void UpdateCardLocks(List<RecipeCardView> views)
        {
            foreach (RecipeCardView card in views)
            {
                if (card == null)
                    continue;

                RecipeCardLockOverlay overlay =
                    card.GetComponent<RecipeCardLockOverlay>();

                if (overlay != null)
                {
                    overlay.SetRemaining(
                        session.GetCardLockRemaining(
                            card.Owner,
                            card.CardId));
                }
            }
        }

        private static RectTransform DealOrigin(
            RectTransform[] origins,
            int index)
        {
            return origins != null && index < origins.Length
                ? origins[index]
                : null;
        }

        public void CancelAllDrags()
        {
            foreach (TwoPlayerTokenView token in tokenViews)
            {
                if (token != null)
                    token.ForceCancel();
            }

            pointerActors.Clear();
            session?.CancelAllGestures();
        }

        private void ValidateReferences()
        {
            if (player1CardsRoot == null ||
                player2CardsRoot == null ||
                tokensRoot == null ||
                dragLayer == null ||
                recipeCardPrefab == null ||
                tokenPrefab == null)
            {
                throw new InvalidOperationException(
                    "Faltan referencias en TwoPlayerBoardView.");
            }
        }

        private void OnDisable()
        {
            CancelAllDrags();
        }
    }
}