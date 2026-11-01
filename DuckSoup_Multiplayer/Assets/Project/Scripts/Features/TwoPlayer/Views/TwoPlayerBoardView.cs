
using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.Art;
using DuckSoup.Features.Audio;
using DuckSoup.Features.GameFeel.UI;
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
        [SerializeField]
        private Transform player1CardsRoot;

        [SerializeField]
        private Transform player2CardsRoot;

        [Header("Shared Table")]
        [SerializeField]
        private Transform tokensRoot;

        [SerializeField]
        private RectTransform dragLayer;

        [Header("Prefabs")]
        [SerializeField]
        private RecipeCardView recipeCardPrefab;

        [SerializeField]
        private TwoPlayerTokenView tokenPrefab;

        [Header("Game Feel - Deal Origins")]
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

        private TwoPlayerSession session;

        private bool inputEnabled;
        private bool initialized;

        public event Action<PlayerId> RecipeCompleted;

        public void Initialize(
            TwoPlayerSession newSession)
        {
            if (initialized)
            {
                throw new InvalidOperationException(
                    "El tablero ya esta inicializado.");
            }

            session = newSession ??
                throw new ArgumentNullException(
                    nameof(newSession));

            ValidateReferences();

            TwoPlayerSnapshot snapshot =
                session.GetSnapshot();

            // Crear las cartas del jugador 1.
            foreach (RecipeCardSnapshot card
                     in snapshot.Player1Cards)
            {
                RecipeCardView view =
                    Instantiate(
                        recipeCardPrefab,
                        player1CardsRoot);

                EnsureCardFeel(view);

                cards1.Add(view);
            }

            // Crear las cartas del jugador 2.
            foreach (RecipeCardSnapshot card
                     in snapshot.Player2Cards)
            {
                RecipeCardView view =
                    Instantiate(
                        recipeCardPrefab,
                        player2CardsRoot);

                EnsureCardFeel(view);

                cards2.Add(view);
            }

            // Cada token tiene su propio hueco.
            // El Grid organiza los slots, no
            // directamente los tokens arrastrables.
            for (int i = 0;
                 i < TwoPlayerSession.TokenSlots;
                 i++)
            {
                GameObject slot = new GameObject(
                    $"TokenSlot_{i:00}",
                    typeof(RectTransform));

                RectTransform slotRect =
                    slot.GetComponent<RectTransform>();

                slotRect.SetParent(
                    tokensRoot,
                    false);

                TwoPlayerTokenView token =
                    Instantiate(
                        tokenPrefab,
                        slotRect);

                RectTransform tokenRect =
                    token.GetComponent<RectTransform>();

                tokenRect.anchorMin = Vector2.zero;
                tokenRect.anchorMax = Vector2.one;

                tokenRect.offsetMin = Vector2.zero;
                tokenRect.offsetMax = Vector2.zero;

                tokenRect.localScale = Vector3.one;

                token.Initialize(
                    this,
                    dragLayer);

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
                session.TryBeginGesture(
                    tokenId,
                    pointerId);

            if (accepted)
            {
                Refresh();

                DuckSoupSoundPlayer.Play(
                    DuckSoupSoundId.IngredientTap);
            }

            return accepted;
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

            RecipeCardView target = null;
            PlayerId completedBy = PlayerId.None;

            if (inputEnabled &&
                wasDragged &&
                TryFindTargetCard(
                    screenPoint,
                    eventCamera,
                    out target))
            {
                accepted = session.TryDeliver(
                    tokenId,
                    pointerId,
                    target.Owner,
                    target.CardId,
                    out completed);

                if (completed)
                    completedBy = target.Owner;
            }
            else
            {
                session.CancelGesture(
                    tokenId,
                    pointerId);
            }

            Refresh();

            if (target != null)
            {
                RecipeCardFeel feel =
                    target.GetComponent<RecipeCardFeel>();

                if (!accepted)
                    feel?.ShakeWrong();

                if (completed)
                    feel?.Complete();

                DuckSoupSoundPlayer.Play(
                    accepted
                        ? DuckSoupSoundId.CorrectIngredient
                        : DuckSoupSoundId.IncorrectIngredient);
            }

            if (completed)
            {
                DuckSoupSoundPlayer.Play(
                    DuckSoupSoundId.RecipeCompleted);

                RecipeCompleted?.Invoke(completedBy);
            }

            return accepted;
        }

        public void CancelTokenGesture(
            string tokenId,
            int pointerId)
        {
            session?.CancelGesture(
                tokenId,
                pointerId);
        }

        private bool TryFindTargetCard(
            Vector2 screenPoint,
            Camera eventCamera,
            out RecipeCardView target)
        {
            foreach (RecipeCardView card in cards1)
            {
                if (card.ContainsScreenPoint(
                    screenPoint,
                    eventCamera))
                {
                    target = card;
                    return true;
                }
            }

            foreach (RecipeCardView card in cards2)
            {
                if (card.ContainsScreenPoint(
                    screenPoint,
                    eventCamera))
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

            TwoPlayerSnapshot snapshot =
                session.GetSnapshot();

            for (int i = 0;
                 i < TwoPlayerSession.CardsPerPlayer;
                 i++)
            {
                RecipeCardView card1 = cards1[i];
                RecipeCardView card2 = cards2[i];

                card1.Bind(
                    snapshot.Player1Cards[i],
                    PlayerId.Player1);

                card2.Bind(
                    snapshot.Player2Cards[i],
                    PlayerId.Player2);

                card1.GetComponent<RecipeCardArtView>()
                    ?.Render(snapshot.Player1Cards[i]);

                card2.GetComponent<RecipeCardArtView>()
                    ?.Render(snapshot.Player2Cards[i]);

                card1.GetComponent<RecipeCardFeel>()
                    ?.ObserveCard(
                        snapshot.Player1Cards[i].CardId,
                        DealOrigin(player1DealOrigins, i),
                        i);

                card2.GetComponent<RecipeCardFeel>()
                    ?.ObserveCard(
                        snapshot.Player2Cards[i].CardId,
                        DealOrigin(player2DealOrigins, i),
                        i);
            }

            for (int i = 0;
                 i < TwoPlayerSession.TokenSlots;
                 i++)
            {
                tokenViews[i].Refresh(
                    snapshot.Tokens[i]);
            }
        }

        private static void EnsureCardFeel(
            RecipeCardView card)
        {
            if (card.GetComponent<RecipeCardFeel>() == null)
            {
                card.gameObject
                    .AddComponent<RecipeCardFeel>();
            }
        }

        private static RectTransform DealOrigin(
            RectTransform[] origins,
            int index)
        {
            return origins != null &&
                   index < origins.Length
                ? origins[index]
                : null;
        }

        public void CancelAllDrags()
        {
            foreach (TwoPlayerTokenView token
                     in tokenViews)
            {
                if (token != null)
                    token.ForceCancel();
            }

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
                    "[TwoPlayerBoard] " +
                    "Faltan referencias en el Inspector.");
            }
        }

        private void OnDisable()
        {
            CancelAllDrags();
        }
    }
}
