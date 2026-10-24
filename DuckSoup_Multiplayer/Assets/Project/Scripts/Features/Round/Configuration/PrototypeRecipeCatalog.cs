
using System.Collections.Generic;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.Round.Configuration
{
    /// <summary>
    /// Catálogo temporal de recetas.
    /// Será sustituido por datos configurables
    /// mediante ScriptableObjects.
    /// </summary>
    public static class PrototypeRecipeCatalog
    {
        public static IReadOnlyList<RecipeDefinition> Create()
        {
            var recipes = new List<RecipeDefinition>
            {
                new RecipeDefinition(
                    "classic",
                    "Sopa clasica",
                    new[]
                    {
                        IngredientType.Carrot,
                        IngredientType.Potato,
                        IngredientType.Onion
                    }),

                new RecipeDefinition(
                    "mushroom",
                    "Sopa de champinones",
                    new[]
                    {
                        IngredientType.Mushroom,
                        IngredientType.Potato,
                        IngredientType.Onion
                    }),

                new RecipeDefinition(
                    "tomato",
                    "Sopa de tomate",
                    new[]
                    {
                        IngredientType.Tomato,
                        IngredientType.Onion,
                        IngredientType.Carrot
                    }),

                new RecipeDefinition(
                    "corn",
                    "Sopa de maiz",
                    new[]
                    {
                        IngredientType.Corn,
                        IngredientType.Potato,
                        IngredientType.Leek
                    }),

                new RecipeDefinition(
                    "forest",
                    "Sopa del bosque",
                    new[]
                    {
                        IngredientType.Mushroom,
                        IngredientType.Leek,
                        IngredientType.Pumpkin
                    }),

                new RecipeDefinition(
                    "countryside",
                    "Sopa campestre",
                    new[]
                    {
                        IngredientType.Corn,
                        IngredientType.Carrot,
                        IngredientType.Pumpkin
                    })
            };

            return recipes.AsReadOnly();
        }
    }
}
