using System;
using System.Collections.Generic;
using DuckSoup.Domain;
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

        private readonly List<RecipeCardView> cards1 = new List<RecipeCardView>();
        private readonly List<RecipeCardView> cards2 = new List<RecipeCardView>();
        private readonly List<TwoPlayerTokenView> tokenViews = new List<TwoPlayerTokenView>();
        private TwoPlayerSession session;
        private bool inputEnabled;
        private bool initialized;

        public event Action<PlayerId> RecipeCompleted;

        public void Initialize(TwoPlayerSession newSession)
        {
            if (initialized) throw new InvalidOperationException("El tablero ya esta inicializado.");
            session = newSession ?? throw new ArgumentNullException(nameof(newSession));
            ValidateReferences();
            TwoPlayerSnapshot snapshot = session.GetSnapshot();

            foreach (RecipeCardSnapshot card in snapshot.Player1Cards)
                cards1.Add(Instantiate(recipeCardPrefab, player1CardsRoot));
            foreach (RecipeCardSnapshot card in snapshot.Player2Cards)
                cards2.Add(Instantiate(recipeCardPrefab, player2CardsRoot));

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
            if (accepted) Refresh();
            return accepted;
        }

        public void FinishTokenGesture(
            string tokenId, int pointerId, Vector2 screenPoint,
            Camera eventCamera, bool wasDragged)
        {
            if (session == null) return;
            bool completed = false;
            PlayerId completedBy = PlayerId.None;
            if (inputEnabled && wasDragged &&
                TryFindTargetCard(screenPoint, eventCamera, out RecipeCardView target))
            {
                session.TryDeliver(tokenId, pointerId, target.Owner, target.CardId, out completed);
                if (completed) completedBy = target.Owner;
            }
            else session.CancelGesture(tokenId, pointerId);

            Refresh();
            if (completed) RecipeCompleted?.Invoke(completedBy);
        }

        public void CancelTokenGesture(string tokenId, int pointerId)
        {
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
            }
            for (int i = 0; i < TwoPlayerSession.TokenSlots; i++)
                tokenViews[i].Refresh(snap.Tokens[i]);
        }

        public void CancelAllDrags()
        {
            foreach (TwoPlayerTokenView token in tokenViews)
                if (token != null) token.ForceCancel();
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
