
using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.Art;
using DuckSoup.Features.Audio;
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

        [Header("Game Feel - Deal Origins")]
        [SerializeField]
        private RectTransform[] dealOrigins =
            new RectTransform[3];

        private SinglePlayerSession session;

        private readonly List<RecipeCardView> cardViews =
            new List<RecipeCardView>();

        private readonly List<SinglePlayerTokenView> tokenViews =
            new List<SinglePlayerTokenView>();

        private bool inputEnabled;

        public event Action RecipeCompleted;

        public void Initialize(
            SinglePlayerSession newSession)
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

            // Crear las tres cartas.
            for (int i = 0;
                 i < snapshot.Cards.Count;
                 i++)
            {
                RecipeCardView card =
                    Instantiate(
                        recipeCardPrefab,
                        cardsRoot);

                if (card.GetComponent<RecipeCardFeel>() == null)
                {
                    card.gameObject
                        .AddComponent<RecipeCardFeel>();
                }

                cardViews.Add(card);
            }

            // Crear los tokens del modo individual.
            for (int i = 0;
                 i < snapshot.Tokens.Count;
                 i++)
            {
                SinglePlayerTokenView token =
                    Instantiate(
                        tokenPrefab,
                        tokensRoot);

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

            bool accepted =
                session.TryBeginGesture(
                    tokenId,
                    pointerId);

            if (accepted)
            {
                Refresh();

                // Sonido al tocar una ficha valida.
                DuckSoupSoundPlayer.Play(
                    DuckSoupSoundId.IngredientTap);
            }

            return accepted;
        }

        // Devuelve si la entrega fue correcta.
        // La vista del token usa este resultado
        // para animar el retorno cuando falla.
        public bool FinishTokenGesture(
            string tokenId,
            int pointerId,
            Vector2 screenPosition,
            Camera eventCamera,
            bool wasDragged)
        {
            if (session == null)
                return false;

            bool recipeCompleted = false;
            bool accepted = false;

            RecipeCardView target = null;

            if (inputEnabled &&
                wasDragged &&
                TryFindCard(
                    screenPosition,
                    eventCamera,
                    out target))
            {
                accepted = session.TryDeliver(
                    tokenId,
                    pointerId,
                    target.CardId,
                    out recipeCompleted);
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

                if (recipeCompleted)
                    feel?.Complete();

                // Solo emitimos acierto/error
                // cuando se suelta sobre una carta.
                DuckSoupSoundPlayer.Play(
                    accepted
                        ? DuckSoupSoundId.CorrectIngredient
                        : DuckSoupSoundId.IncorrectIngredient);
            }

            if (recipeCompleted)
            {
                DuckSoupSoundPlayer.Play(
                    DuckSoupSoundId.RecipeCompleted);

                RecipeCompleted?.Invoke();
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

        private bool TryFindCard(
            Vector2 position,
            Camera eventCamera,
            out RecipeCardView result)
        {
            foreach (RecipeCardView card in cardViews)
            {
                if (card.ContainsScreenPoint(
                    position,
                    eventCamera))
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

            for (int i = 0;
                 i < cardViews.Count;
                 i++)
            {
                RecipeCardView card = cardViews[i];

                card.Bind(
                    snapshot.Cards[i],
                    PlayerId.Player1);

                card.GetComponent<RecipeCardArtView>()
                    ?.Render(snapshot.Cards[i]);

                RectTransform origin =
                    dealOrigins != null &&
                    i < dealOrigins.Length
                        ? dealOrigins[i]
                        : null;

                card.GetComponent<RecipeCardFeel>()
                    ?.ObserveCard(
                        snapshot.Cards[i].CardId,
                        origin,
                        i);
            }

            for (int i = 0;
                 i < tokenViews.Count;
                 i++)
            {
                tokenViews[i].Refresh(
                    snapshot.Tokens[i]);
            }
        }

        public void CancelAllDrags()
        {
            foreach (SinglePlayerTokenView token
                     in tokenViews)
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
                    "[SinglePlayerBoard] " +
                    "Faltan referencias en el Inspector.");
            }
        }

        private void OnDisable()
        {
            CancelAllDrags();
        }
    }
}
