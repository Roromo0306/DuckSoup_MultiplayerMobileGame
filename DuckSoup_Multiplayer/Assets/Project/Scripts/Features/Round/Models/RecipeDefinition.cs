using System;
using System.Collections.Generic;

namespace DuckSoup.Features.Round.Models
{
    public sealed class RecipeDefinition
    {
        private readonly IReadOnlyList<IngredientType>requiredIngredients;

        public string Id { get; }
        public string DisplayName { get; }
        public IReadOnlyList<IngredientType> RequiredIngredients => requiredIngredients;

        public RecipeDefinition(string id, string displayName, IEnumerable<IngredientType> ingredients)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("La receta necesita un ID válido.",nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("La receta necesita un nombre.",nameof(displayName));
            }

            if (ingredients == null)
            {
                throw new ArgumentNullException(nameof(ingredients));
            }

            var ingredientList = new List<IngredientType>();

            foreach (IngredientType ingredient in ingredients)
            {
                if (ingredient == IngredientType.None || !Enum.IsDefined(typeof(IngredientType), ingredient))
                {
                    throw new ArgumentException("La receta contiene un ingrediente inválido.", nameof(ingredients));
                }

                ingredientList.Add(ingredient);
            }

            if (ingredientList.Count == 0)
            {
                throw new ArgumentException("Una receta necesita al menos un ingrediente.",nameof(ingredients));
            }

            Id = id;
            DisplayName = displayName;

            requiredIngredients = ingredientList.AsReadOnly();
        }
    }
}
