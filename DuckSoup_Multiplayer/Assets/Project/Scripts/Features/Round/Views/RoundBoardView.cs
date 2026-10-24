
using DuckSoup.Domain;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Controllers;
using System;
using System.Collections.Generic;

using TMPro;
using UnityEngine;

namespace DuckSoup.Features.Round.Views
{
    /// <summary>
    /// Vista principal de la ronda.
    ///
    /// Crea las cartas y tokens, detecta
    /// los destinos visuales y presenta
    /// los resultados de las acciones.
    /// </summary>
    public sealed class RoundBoardView :
        MonoBehaviour, IRoundPreviewView
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
        private IngredientTokenView ingredientTokenPrefab;

        [Header("Results")]
        [SerializeField]
        private TextMeshProUGUI winnerText;

        private RoundGameplayController controller;

        private readonly List<GameObject> spawnedObjects =
            new List<GameObject>();

        private readonly List<RecipeCardView> cardViews =
            new List<RecipeCardView>();

        private readonly Dictionary<string, RecipeCardView>
            cardsById =
                new Dictionary<string, RecipeCardView>();

        private readonly Dictionary<string, IngredientTokenView>
            tokensById =
                new Dictionary<string, IngredientTokenView>();

        /// <summary>
        /// Inyecta el controlador antes
        /// de construir la interfaz.
        /// </summary>
        public void Initialize(
            RoundGameplayController gameplayController)
        {
            controller = gameplayController ??
                throw new ArgumentNullException(
                    nameof(gameplayController));
        }

        public void Render(RoundSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(
                    nameof(snapshot));

            if (controller == null)
                throw new InvalidOperationException(
                    "Falta inicializar el controlador.");

            ValidateReferences();
            Clear();

            CreateCards(
                snapshot.Player1.Cards,
                player1CardsRoot,
                PlayerId.Player1);

            CreateCards(
                snapshot.Player2.Cards,
                player2CardsRoot,
                PlayerId.Player2);

            CreateTokens(snapshot.Tokens);

            Refresh();

            Debug.Log(
                $"[RoundBoard] Ronda {snapshot.Id} mostrada.",
                this);
        }

        private void CreateCards(
            IReadOnlyList<RecipeCardSnapshot> cards,
            Transform parent,
            PlayerId playerId)
        {
            foreach (RecipeCardSnapshot snapshot in cards)
            {
                RecipeCardView cardView =
                    Instantiate(recipeCardPrefab, parent);

                cardView.Bind(snapshot, playerId);

                cardViews.Add(cardView);
                cardsById.Add(snapshot.CardId, cardView);
                spawnedObjects.Add(cardView.gameObject);
            }
        }

        private void CreateTokens(
            IReadOnlyList<IngredientTokenSnapshot> tokens)
        {
            foreach (IngredientTokenSnapshot snapshot in tokens)
            {
                IngredientTokenView tokenView =
                    Instantiate(ingredientTokenPrefab, tokensRoot);

                tokenView.Initialize(
                    snapshot,
                    this,
                    dragLayer);

                tokensById.Add(snapshot.Id, tokenView);
                spawnedObjects.Add(tokenView.gameObject);
            }
        }

        /// <summary>
        /// Solicita al controlador reservar
        /// y revelar un ingrediente.
        /// </summary>
        public bool TryBeginToken(
            string tokenId,
            int pointerId)
        {
            if (controller == null)
                return false;

            bool accepted = controller.TryBeginToken(
                tokenId, pointerId);

            if (accepted)
                Refresh();

            return accepted;
        }

        /// <summary>
        /// Finaliza un gesto.
        ///
        /// Si el dedo se suelta sobre una carta,
        /// intentamos entregar el ingrediente.
        /// En otro caso cancelamos su reserva.
        /// </summary>
        public void FinishTokenGesture(
            string tokenId,
            int pointerId,
            Vector2 screenPosition,
            Camera eventCamera,
            bool wasDragged)
        {
            if (controller == null)
                return;

            bool delivered = false;

            if (wasDragged &&
                TryFindDropCard(
                    screenPosition,
                    eventCamera,
                    out RecipeCardView targetCard))
            {
                delivered = controller.TryDeliverToken(
                    tokenId,
                    pointerId,
                    targetCard.Owner,
                    targetCard.CardId);
            }
            else
            {
                controller.CancelTokenGesture(
                    tokenId, pointerId);
            }

            Refresh();

            if (delivered)
            {
                Debug.Log(
                    $"[Round] Ingrediente {tokenId} entregado.",
                    this);
            }

            if (controller.IsFinished)
            {
                CancelAllTokenDrags();

                Debug.Log(
                    $"[Round] Ganador: {controller.Winner}",
                    this);
            }
        }

        public void CancelTokenGesture(
            string tokenId,
            int pointerId)
        {
            controller?.CancelTokenGesture(
                tokenId, pointerId);
        }

        /// <summary>
        /// Busca la carta cuyo RectTransform
        /// contiene la posición de pantalla.
        /// </summary>
        private bool TryFindDropCard(
            Vector2 screenPosition,
            Camera eventCamera,
            out RecipeCardView targetCard)
        {
            foreach (RecipeCardView card in cardViews)
            {
                if (card != null &&
                    card.ContainsScreenPoint(
                        screenPosition,
                        eventCamera))
                {
                    targetCard = card;
                    return true;
                }
            }

            targetCard = null;
            return false;
        }

        /// <summary>
        /// Actualiza las vistas existentes
        /// con un nuevo snapshot.
        ///
        /// No reinstancia los objetos.
        /// </summary>
        public void Refresh()
        {
            if (controller == null)
                return;

            RoundSnapshot snapshot =
                controller.GetSnapshot();

            RefreshCards(
                snapshot.Player1.Cards,
                PlayerId.Player1);

            RefreshCards(
                snapshot.Player2.Cards,
                PlayerId.Player2);

            foreach (IngredientTokenSnapshot token
                     in snapshot.Tokens)
            {
                if (tokensById.TryGetValue(
                    token.Id,
                    out IngredientTokenView tokenView))
                {
                    tokenView.Refresh(token);
                }
            }

            if (winnerText != null)
            {
                winnerText.text = controller.IsFinished
                    ? (controller.Winner == PlayerId.Player1
                        ? "¡GANA JUGADOR 1!"
                        : "¡GANA JUGADOR 2!")
                    : string.Empty;
            }
        }

        private void RefreshCards(
            IReadOnlyList<RecipeCardSnapshot> cards,
            PlayerId owner)
        {
            foreach (RecipeCardSnapshot snapshot in cards)
            {
                if (cardsById.TryGetValue(
                    snapshot.CardId,
                    out RecipeCardView cardView))
                {
                    cardView.Bind(snapshot, owner);
                }
            }
        }

        public void CancelAllTokenDrags()
        {
            foreach (IngredientTokenView tokenView
                     in tokensById.Values)
            {
                if (tokenView != null)
                {
                    tokenView.ForceCancel();
                }
            }

            controller?.CancelAllGestures();
        }

        public void Clear()
        {
            CancelAllTokenDrags();

            foreach (GameObject instance in spawnedObjects)
            {
                if (instance != null)
                    Destroy(instance);
            }

            spawnedObjects.Clear();
            cardViews.Clear();
            cardsById.Clear();
            tokensById.Clear();

            if (winnerText != null)
                winnerText.text = string.Empty;
        }

        private void ValidateReferences()
        {
            if (player1CardsRoot == null ||
                player2CardsRoot == null ||
                tokensRoot == null ||
                dragLayer == null ||
                recipeCardPrefab == null ||
                ingredientTokenPrefab == null)
            {
                throw new InvalidOperationException(
                    "RoundBoardView tiene referencias " +
                    "sin asignar en el Inspector.");
            }
        }
    }
}
