
using System;
using System.Collections.Generic;
using DuckSoup.Domain;

namespace DuckSoup.Features.Round.Models
{
    /// <summary>
    /// Representa a un jugador durante una ronda.
    ///
    /// Gestiona su conjunto de cartas,
    /// pero no almacena dinero ni mejoras.
    /// </summary>
    public sealed class RoundPlayerModel
    {
        private readonly IReadOnlyList<RecipeCardModel>
            cards;

        public PlayerId Id { get; }

        public IReadOnlyList<RecipeCardModel>
            Cards => cards;

        public int CompletedCardCount
        {
            get
            {
                int completed = 0;

                foreach (RecipeCardModel card in cards)
                {
                    if (card.IsComplete)
                    {
                        completed++;
                    }
                }

                return completed;
            }
        }

        public bool HasCompletedAllCards =>
            CompletedCardCount == cards.Count;

        public RoundPlayerModel(
            PlayerId id,
            IEnumerable<RecipeCardModel> recipeCards,
            int expectedCardCount)
        {
            if (id != PlayerId.Player1 &&
                id != PlayerId.Player2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    "El jugador no es válido.");
            }

            if (recipeCards == null)
            {
                throw new ArgumentNullException(
                    nameof(recipeCards));
            }

            if (expectedCardCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedCardCount));
            }

            var cardList = new List<RecipeCardModel>();

            var cardIds = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (RecipeCardModel card in recipeCards)
            {
                if (card == null)
                {
                    throw new ArgumentException(
                        "No puede haber cartas nulas.",
                        nameof(recipeCards));
                }

                if (!cardIds.Add(card.CardId))
                {
                    throw new ArgumentException(
                        "Existen IDs de carta duplicados.",
                        nameof(recipeCards));
                }

                cardList.Add(card);
            }

            if (cardList.Count != expectedCardCount)
            {
                throw new ArgumentException(
                    $"Se esperaban {expectedCardCount} cartas, " +
                    $"pero se recibieron {cardList.Count}.",
                    nameof(recipeCards));
            }

            Id = id;

            cards = cardList.AsReadOnly();
        }

        /// <summary>
        /// Busca una carta del jugador por ID.
        /// </summary>
        public bool TryGetCard(
            string cardId,
            out RecipeCardModel card)
        {
            card = null;

            if (string.IsNullOrWhiteSpace(cardId))
                return false;

            foreach (RecipeCardModel current in cards)
            {
                if (current.CardId == cardId)
                {
                    card = current;
                    return true;
                }
            }

            return false;
        }
    }
}
