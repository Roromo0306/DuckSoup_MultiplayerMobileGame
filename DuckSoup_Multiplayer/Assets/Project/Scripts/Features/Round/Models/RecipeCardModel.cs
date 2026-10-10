
using System;

namespace DuckSoup.Features.Round.Models
{
    /// <summary>
    /// Representa una carta de receta individual.
    /// Cada carta tiene su propio progreso.
    /// </summary>
    public sealed class RecipeCardModel
    {
        public string CardId { get; }

        public RecipeDefinition Definition =>
            Progress.Definition;

        public RecipeProgress Progress { get; }

        public bool IsComplete => Progress.IsComplete;

        public RecipeCardModel(
            string cardId,
            RecipeDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                throw new ArgumentException(
                    "La carta necesita un ID válido.",
                    nameof(cardId));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(
                    nameof(definition));
            }

            CardId = cardId.Trim();

            // Cada carta tiene su propio progreso.
            Progress = new RecipeProgress(definition);
        }
    }
}
