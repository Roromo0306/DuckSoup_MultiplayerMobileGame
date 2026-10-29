using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.GameFeel.UI;
using DuckSoup.Features.Round.Views;
using DuckSoup.Features.SinglePlayer.Models;
using DuckSoup.Features.SinglePlayer.Services;
using UnityEngine;

namespace DuckSoup.Features.SinglePlayer.Views
{
    public sealed class SinglePlayerBoardView : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] private Transform cardsRoot;
        [SerializeField] private Transform tokensRoot;
        [SerializeField] private RectTransform dragLayer;

        [Header("Prefabs")]
        [SerializeField] private RecipeCardView recipeCardPrefab;
        [SerializeField] private SinglePlayerTokenView tokenPrefab;

        [Header("Game Feel - deal origins (left, center, right), optional")]
        [SerializeField] private RectTransform[] dealOrigins = new RectTransform[3];

        private SinglePlayerSession session;
        private readonly List<RecipeCardView> cardViews = new List<RecipeCardView>();
        private readonly List<SinglePlayerTokenView> tokenViews = new List<SinglePlayerTokenView>();
        private bool inputEnabled;

        public event Action RecipeCompleted;

        public void Initialize(SinglePlayerSession newSession)
        {
            session = newSession ?? throw new ArgumentNullException(nameof(newSession));
            ValidateReferences();
            BuildBoard();
        }

        private void BuildBoard()
        {
            SinglePlayerSnapshot snapshot = session.GetSnapshot();
            for (int i = 0; i < snapshot.Cards.Count; i++)
            {
                RecipeCardView card = Instantiate(recipeCardPrefab, cardsRoot);
                if (card.GetComponent<RecipeCardFeel>() == null)
                    card.gameObject.AddComponent<RecipeCardFeel>();
                cardViews.Add(card);
            }
            for (int i = 0; i < snapshot.Tokens.Count; i++)
            {
                SinglePlayerTokenView token = Instantiate(tokenPrefab, tokensRoot);
                token.Initialize(this, dragLayer);
                tokenViews.Add(token);
            }
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

        // Ahora devuelve si la entrega fue correcta (necesario para el retorno animado).
        public bool FinishTokenGesture(
            string tokenId, int pointerId, Vector2 screenPosition,
            Camera eventCamera, bool wasDragged)
        {
            if (session == null) return false;
            bool recipeCompleted = false;
            bool accepted = false;
            RecipeCardView target = null;

            if (inputEnabled && wasDragged &&
                TryFindCard(screenPosition, eventCamera, out target))
            {
                accepted = session.TryDeliver(
                    tokenId, pointerId, target.CardId, out recipeCompleted);
            }
            else
                session.CancelGesture(tokenId, pointerId);

            Refresh();

            if (target != null)
            {
                RecipeCardFeel fx = target.GetComponent<RecipeCardFeel>();
                if (!accepted) fx?.ShakeWrong();
                if (recipeCompleted) fx?.Complete();
            }
            if (recipeCompleted) RecipeCompleted?.Invoke();
            return accepted;
        }

        public void CancelTokenGesture(string tokenId, int pointerId) =>
            session?.CancelGesture(tokenId, pointerId);

        private bool TryFindCard(
            Vector2 position, Camera eventCamera, out RecipeCardView result)
        {
            foreach (RecipeCardView card in cardViews)
            {
                if (card.ContainsScreenPoint(position, eventCamera))
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
            if (session == null) return;
            SinglePlayerSnapshot snap = session.GetSnapshot();
            for (int i = 0; i < cardViews.Count; i++)
            {
                cardViews[i].Bind(snap.Cards[i], PlayerId.Player1);
                RectTransform origin = dealOrigins != null && i < dealOrigins.Length
                    ? dealOrigins[i] : null;
                cardViews[i].GetComponent<RecipeCardFeel>()?.ObserveCard(
                    snap.Cards[i].CardId, origin, i);
            }
            for (int i = 0; i < tokenViews.Count; i++)
                tokenViews[i].Refresh(snap.Tokens[i]);
        }

        public void CancelAllDrags()
        {
            foreach (SinglePlayerTokenView token in tokenViews)
                if (token != null) token.ForceCancel();
            session?.CancelAllGestures();
        }

        private void ValidateReferences()
        {
            if (cardsRoot == null || tokensRoot == null || dragLayer == null ||
                recipeCardPrefab == null || tokenPrefab == null)
                throw new InvalidOperationException(
                    "[SinglePlayerBoard] Faltan referencias en el Inspector.");
        }

        private void OnDisable() => CancelAllDrags();
    }
}
