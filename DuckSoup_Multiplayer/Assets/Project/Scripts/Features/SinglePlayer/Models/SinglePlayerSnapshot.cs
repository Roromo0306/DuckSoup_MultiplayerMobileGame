
using System;
using System.Collections.Generic;
using DuckSoup.Features.Round.Contracts;

namespace DuckSoup.Features.SinglePlayer.Models
{
    public sealed class SinglePlayerSnapshot
    {
        public IReadOnlyList<RecipeCardSnapshot> Cards { get; }

        // Una posición puede estar vacía tras
        // consumir un ingrediente.
        public IReadOnlyList<IngredientTokenSnapshot> Tokens
        {
            get;
        }

        public int CompletedRecipes { get; }

        public float RemainingSeconds { get; }

        public SinglePlayerSnapshot(
            IEnumerable<RecipeCardSnapshot> cards,
            IEnumerable<IngredientTokenSnapshot> tokens,
            int completedRecipes,
            float remainingSeconds)
        {
            if (cards == null)
                throw new ArgumentNullException(nameof(cards));

            if (tokens == null)
                throw new ArgumentNullException(nameof(tokens));

            Cards = new List<RecipeCardSnapshot>(cards)
                .AsReadOnly();

            Tokens = new List<IngredientTokenSnapshot>(tokens)
                .AsReadOnly();

            CompletedRecipes = completedRecipes;
            RemainingSeconds = remainingSeconds;
        }
    }
}
