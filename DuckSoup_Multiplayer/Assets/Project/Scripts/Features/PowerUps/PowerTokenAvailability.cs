using System.Collections.Generic;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.PowerUps
{
    // Pure C#: only replace surplus ingredients. Keeps EVERY active recipe solvable.
    public static class PowerTokenAvailability
    {
        public static List<int> GetEligibleSlots(
            IEnumerable<RecipeCardModel> cards,
            IngredientToken[] tokens,
            IngredientType[] pending,
            float[] refillTimers,
            PowerTokenKind[] kinds,
            Dictionary<string, int> pointerOwners)
        {
            var required = new Dictionary<IngredientType, int>();
            var available = new Dictionary<IngredientType, int>();

            foreach (RecipeCardModel card in cards)
            {
                if (card == null || card.IsComplete) continue;
                var visited = new HashSet<IngredientType>();
                foreach (IngredientType ingredient in card.Definition.RequiredIngredients)
                {
                    if (!visited.Add(ingredient)) continue;
                    int remaining = card.Progress.GetRequiredCount(ingredient) -
                        card.Progress.GetCompletedCount(ingredient);
                    Add(required, ingredient, remaining);
                }
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] != null && kinds[i] == PowerTokenKind.None)
                    Add(available, tokens[i].Ingredient, 1);
                else if (tokens[i] == null && refillTimers[i] >= 0f)
                    Add(available, pending[i], 1);
            }

            var eligible = new List<int>();
            for (int i = 0; i < tokens.Length; i++)
            {
                IngredientToken token = tokens[i];
                if (token == null || token.IsConsumed || token.IsRevealed ||
                    kinds[i] != PowerTokenKind.None ||
                    pointerOwners.ContainsKey(token.Id))
                    continue;
                IngredientType type = token.Ingredient;
                if (Count(available, type) > Count(required, type))
                    eligible.Add(i);
            }
            return eligible;
        }

        private static int Count(Dictionary<IngredientType, int> dict, IngredientType t)
        {
            int value;
            return dict.TryGetValue(t, out value) ? value : 0;
        }

        private static void Add(Dictionary<IngredientType, int> dict, IngredientType type, int amount)
        {
            int current;
            dict.TryGetValue(type, out current);
            dict[type] = current + amount;
        }
    }
}
