
using System;
using System.Collections.Generic;

namespace DuckSoup.Features.Round.Models
{
    public sealed class RecipeProgress
    {
        private readonly Dictionary<IngredientType, int>requiredCounts;

        private readonly Dictionary<IngredientType, int>completedCounts;

        public RecipeDefinition Definition { get; }

        public int CompletedIngredientCount { get; private set; }

        public int TotalIngredientCount => Definition.RequiredIngredients.Count;

        public bool IsComplete => CompletedIngredientCount == TotalIngredientCount;

        public RecipeProgress(RecipeDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));

            requiredCounts = new Dictionary<IngredientType, int>();

            completedCounts = new Dictionary<IngredientType, int>();

            foreach (IngredientType ingredient in Definition.RequiredIngredients)
            {
                if (!requiredCounts.ContainsKey(ingredient))
                {
                    requiredCounts[ingredient] = 0;
                    completedCounts[ingredient] = 0;
                }

                requiredCounts[ingredient]++;
            }
        }

        public bool NeedsIngredient(IngredientType ingredient)
        {
            if (IsComplete)
                return false;

            if (!requiredCounts.TryGetValue(ingredient, out int required))
            {
                return false;
            }

            return completedCounts[ingredient] < required;
        }

        public bool TryAddIngredient(IngredientType ingredient)
        {
            if (!NeedsIngredient(ingredient))
                return false;

            completedCounts[ingredient]++;
            CompletedIngredientCount++;

            return true;
        }


        public int GetRequiredCount(IngredientType ingredient)
        {
            return requiredCounts.TryGetValue(ingredient, out int count) ? count : 0;
        }
        public int GetCompletedCount(IngredientType ingredient)
        {
            return completedCounts.TryGetValue(ingredient, out int count) ? count : 0;
        }
    }
}
