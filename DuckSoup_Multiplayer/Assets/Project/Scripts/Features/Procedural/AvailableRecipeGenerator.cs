using System;
using System.Collections.Generic;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.Procedural
{
    // Genera recetas SOLO con unidades disponibles, descontando lo que
    // aun necesitan todas las demas cartas activas. C# puro.
    public sealed class AvailableRecipeGenerator
    {
        private static readonly IngredientType[] AllTypes =
        {
            IngredientType.Carrot, IngredientType.Potato,
            IngredientType.Onion, IngredientType.Mushroom,
            IngredientType.Tomato, IngredientType.Pumpkin,
            IngredientType.Corn, IngredientType.Leek
        };

        private readonly IRandomSource random;
        private int serial;

        public AvailableRecipeGenerator(IRandomSource randomSource)
        {
            random = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        }

        // Garantiza al menos una unidad de cada uno de los ocho tipos.
        public List<IngredientType> InitialStock(int numberOfTokens)
        {
            if (numberOfTokens < AllTypes.Length)
                throw new ArgumentOutOfRangeException(nameof(numberOfTokens));
            var result = new List<IngredientType>(numberOfTokens);
            result.AddRange(AllTypes);
            while (result.Count < numberOfTokens)
                result.Add(AllTypes[random.NextInt(0, AllTypes.Length)]);
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(0, i + 1);
                IngredientType tmp = result[i]; result[i] = result[j]; result[j] = tmp;
            }
            return result;
        }

        public IngredientType RandomIngredient()
        {
            return AllTypes[random.NextInt(0, AllTypes.Length)];
        }

        // Stock total = tokens presentes + refills pendientes.
        // Reserva primero las unidades que requieren las cartas en juego.
        public RecipeDefinition Generate(
            IEnumerable<IngredientType> totalStock,
            IEnumerable<RecipeCardModel> currentCards)
        {
            if (totalStock == null || currentCards == null)
                throw new ArgumentNullException("Stock o cartas nulas.");

            var free = new List<IngredientType>(totalStock);
            foreach (RecipeCardModel card in currentCards)
            {
                if (card == null || card.IsComplete) continue;
                var visited = new HashSet<IngredientType>();
                foreach (IngredientType type in card.Definition.RequiredIngredients)
                {
                    if (!visited.Add(type)) continue;
                    int remaining = card.Progress.GetRequiredCount(type) -
                                    card.Progress.GetCompletedCount(type);
                    for (int n = 0; n < remaining; n++)
                    {
                        if (!free.Remove(type))
                            throw new InvalidOperationException(
                                "Mesa inconsistente: falta un ingrediente reservado para otra receta.");
                    }
                }
            }

            if (free.Count < 3)
                throw new InvalidOperationException(
                    "No quedan tres ingredientes libres para una nueva receta.");

            var chosen = new List<IngredientType>(3);
            for (int i = 0; i < 3; i++)
            {
                // Favorece tres imagenes distintas si existen esos tipos.
                var distinct = new List<IngredientType>();
                foreach (IngredientType type in free)
                    if (!chosen.Contains(type) && !distinct.Contains(type)) distinct.Add(type);
                IngredientType selection;
                if (distinct.Count > 0)
                    selection = distinct[random.NextInt(0, distinct.Count)];
                else
                    selection = free[random.NextInt(0, free.Count)];
                chosen.Add(selection);
                free.Remove(selection);
            }

            serial++;
            return new RecipeDefinition($"dynamic_recipe_{serial:000000}", "SOUP", chosen);
        }
    }
}
