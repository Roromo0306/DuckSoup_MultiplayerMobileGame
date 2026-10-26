
using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Views;
using DuckSoup.Features.SinglePlayer.Models;
using DuckSoup.Features.SinglePlayer.Services;
using UnityEngine;

namespace DuckSoup.Features.SinglePlayer.Views
{
    public sealed class SinglePlayerBoardView : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField]
        private Transform cardsRoot;

        [SerializeField]
        private Transform tokensRoot;

        [SerializeField]
        private RectTransform dragLayer;

        [Header("Prefabs")]
        [SerializeField]
        private RecipeCardView recipeCardPrefab;

        [SerializeField]
        private SinglePlayerTokenView tokenPrefab;

        private SinglePlayerSession session;

        private readonly List<RecipeCardView> cardViews =
            new List<RecipeCardView>();

        private readonly List<SinglePlayerTokenView> tokenViews =
            new List<SinglePlayerTokenView>();

        private bool inputEnabled;

        public event Action RecipeCompleted;

        public void Initialize(SinglePlayerSession newSession)
        {
            session = newSession ??
                throw new ArgumentNullException(
                    nameof(newSession));

            ValidateReferences();
            BuildBoard();
        }

        private void BuildBoard()
        {
            SinglePlayerSnapshot snapshot =
                session.GetSnapshot();

            for (int i = 0; i < snapshot.Cards.Count; i++)
            {
                RecipeCardView card =
                    Instantiate(recipeCardPrefab, cardsRoot);

                cardViews.Add(card);
            }

            for (int i = 0; i < snapshot.Tokens.Count; i++)
            {
                SinglePlayerTokenView token =
                    Instantiate(tokenPrefab, tokensRoot);

                token.Initialize(this, dragLayer);
                tokenViews.Add(token);
            }

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

            bool accepted = session.TryBeginGesture(
                tokenId, pointerId);

            if (accepted)
                Refresh();

            return accepted;
        }

        public void FinishTokenGesture(
            string tokenId,
            int pointerId,
            Vector2 screenPosition,
            Camera eventCamera,
            bool wasDragged)
        {
            if (session == null)
                return;

            bool recipeCompleted = false;

            if (inputEnabled &&
                wasDragged &&
                TryFindCard(
                    screenPosition,
                    eventCamera,
                    out RecipeCardView targetCard))
            {
                session.TryDeliver(
                    tokenId,
                    pointerId,
                    targetCard.CardId,
                    out recipeCompleted);
            }
            else
            {
                session.CancelGesture(
                    tokenId, pointerId);
            }

            Refresh();

            if (recipeCompleted)
                RecipeCompleted?.Invoke();
        }

        public void CancelTokenGesture(
            string tokenId,
            int pointerId)
        {
            session?.CancelGesture(
                tokenId, pointerId);
        }

        private bool TryFindCard(
            Vector2 position,
            Camera eventCamera,
            out RecipeCardView result)
        {
            foreach (RecipeCardView card in cardViews)
            {
                if (card.ContainsScreenPoint(
                    position, eventCamera))
                {
                    result = card;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public void Refresh()
        {
            if (session == null)
                return;

            SinglePlayerSnapshot snapshot =
                session.GetSnapshot();

            for (int i = 0; i < cardViews.Count; i++)
            {
                cardViews[i].Bind(
                    snapshot.Cards[i],
                    PlayerId.Player1);
            }

            for (int i = 0; i < tokenViews.Count; i++)
            {
                tokenViews[i].Refresh(
                    snapshot.Tokens[i]);
            }
        }

        public void CancelAllDrags()
        {
            foreach (SinglePlayerTokenView token in tokenViews)
            {
                if (token != null)
                    token.ForceCancel();
            }

            session?.CancelAllGestures();
        }

        private void ValidateReferences()
        {
            if (cardsRoot == null ||
                tokensRoot == null ||
                dragLayer == null ||
                recipeCardPrefab == null ||
                tokenPrefab == null)
            {
                throw new InvalidOperationException(
                    "[SinglePlayerBoard] Faltan " +
                    "referencias en el Inspector.");
            }
        }

        private void OnDisable()
        {
            CancelAllDrags();
        }
    }
}
