
using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.Round.Contracts
{
    public sealed class IngredientRequirementSnapshot
    {
        public IngredientType Ingredient { get; }

        public int RequiredCount { get; }

        public int CompletedCount { get; }

        public bool IsComplete =>
            CompletedCount == RequiredCount;

        public IngredientRequirementSnapshot(
            IngredientType ingredient,
            int requiredCount,
            int completedCount)
        {
            if (requiredCount <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(requiredCount));

            if (completedCount < 0 ||
                completedCount > requiredCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedCount));
            }

            Ingredient = ingredient;
            RequiredCount = requiredCount;
            CompletedCount = completedCount;
        }
    }

    public sealed class RecipeCardSnapshot
    {
        public string CardId { get; }

        public string RecipeId { get; }

        public string DisplayName { get; }

        public IReadOnlyList<IngredientRequirementSnapshot>
            Requirements
        { get; }

        public int TotalIngredientCount { get; }

        public int CompletedIngredientCount { get; }

        public bool IsComplete =>
            CompletedIngredientCount == TotalIngredientCount;

        public RecipeCardSnapshot(
            string cardId,
            string recipeId,
            string displayName,
            IEnumerable<IngredientRequirementSnapshot>
                requirements)
        {
            if (requirements == null)
                throw new ArgumentNullException(
                    nameof(requirements));

            var copy =
                new List<IngredientRequirementSnapshot>(
                    requirements);

            if (copy.Count == 0)
                throw new ArgumentException(
                    "La carta necesita requisitos.",
                    nameof(requirements));

            foreach (IngredientRequirementSnapshot item in copy)
            {
                if (item == null)
                    throw new ArgumentException(
                        "Existe un requisito nulo.",
                        nameof(requirements));

                TotalIngredientCount += item.RequiredCount;
                CompletedIngredientCount += item.CompletedCount;
            }

            CardId = cardId;
            RecipeId = recipeId;
            DisplayName = displayName;

            Requirements = copy.AsReadOnly();
        }
    }
    public sealed class RoundPlayerSnapshot
    {
        public PlayerId Id { get; }

        public IReadOnlyList<RecipeCardSnapshot>
            Cards
        { get; }

        public int CompletedCardCount { get; }

        public bool HasCompletedAllCards =>
            Cards.Count > 0 &&
            CompletedCardCount == Cards.Count;

        public RoundPlayerSnapshot(
            PlayerId id,
            IEnumerable<RecipeCardSnapshot> cards)
        {
            if (cards == null)
                throw new ArgumentNullException(
                    nameof(cards));

            var copy = new List<RecipeCardSnapshot>(cards);

            foreach (RecipeCardSnapshot card in copy)
            {
                if (card == null)
                    throw new ArgumentException(
                        "Existe una carta nula.",
                        nameof(cards));

                if (card.IsComplete)
                    CompletedCardCount++;
            }

            Id = id;
            Cards = copy.AsReadOnly();
        }
    }
    public sealed class IngredientTokenSnapshot
    {
        public string Id { get; }

        public bool IsRevealed { get; }

        public bool IsConsumed { get; }

        public IngredientType VisibleIngredient { get; }

        public IngredientTokenSnapshot(
            string id,
            IngredientType ingredient,
            bool isRevealed,
            bool isConsumed)
        {
            Id = id;
            IsRevealed = isRevealed;
            IsConsumed = isConsumed;

            // Nunca exponemos el ingrediente oculto.
            VisibleIngredient = isRevealed
                ? ingredient
                : IngredientType.None;
        }
    }


    public sealed class RoundSnapshot
    {
        public string Id { get; }

        public RoundPlayerSnapshot Player1 { get; }

        public RoundPlayerSnapshot Player2 { get; }

        public IReadOnlyList<IngredientTokenSnapshot>
            Tokens
        { get; }

        public RoundSnapshot(
            string id,
            RoundPlayerSnapshot player1,
            RoundPlayerSnapshot player2,
            IEnumerable<IngredientTokenSnapshot> tokens)
        {
            if (player1 == null)
                throw new ArgumentNullException(
                    nameof(player1));

            if (player2 == null)
                throw new ArgumentNullException(
                    nameof(player2));

            if (tokens == null)
                throw new ArgumentNullException(
                    nameof(tokens));

            Id = id;
            Player1 = player1;
            Player2 = player2;

            Tokens = new List<IngredientTokenSnapshot>(
                tokens).AsReadOnly();
        }
    }
}
