
using System;
using DuckSoup.Features.Round.Models;
using UnityEngine;

namespace DuckSoup.Tests.Manual
{
    public sealed class RecipeModelSmokeTest : MonoBehaviour
    {
        private void Start()
        {
            try
            {
                RunTests();

                Debug.Log(
                    "[Domain Test] All recipe tests passed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void RunTests()
        {
            var definition = new RecipeDefinition(
                "test_soup",
                "Sopa de prueba",
                new[]
                {
                    IngredientType.Carrot,
                    IngredientType.Potato,
                    IngredientType.Onion
                });

            // Dos cartas independientes
            // que comparten la misma definición.
            var first = new RecipeProgress(definition);
            var second = new RecipeProgress(definition);

            Assert(first.TotalIngredientCount == 3,
                "La receta debe necesitar 3 ingredientes.");

            Assert(!first.IsComplete,
                "La receta no debe comenzar completada.");

            Assert(!first.TryAddIngredient(IngredientType.Corn),
                "No debe aceptar maíz.");

            Assert(first.TryAddIngredient(IngredientType.Carrot),
                "Debe aceptar zanahoria.");

            Assert(!first.TryAddIngredient(IngredientType.Carrot),
                "No debe aceptar una segunda zanahoria.");

            Assert(second.CompletedIngredientCount == 0,
                "El progreso de otra carta debe ser independiente.");

            Assert(first.TryAddIngredient(IngredientType.Potato),
                "Debe aceptar patata.");

            Assert(first.TryAddIngredient(IngredientType.Onion),
                "Debe aceptar cebolla.");

            Assert(first.IsComplete,
                "La receta debe estar completada.");

            Assert(!first.TryAddIngredient(IngredientType.Onion),
                "Una receta completada no acepta más ingredientes.");

            // Comprobar ingredientes repetidos.
            var repeatedDefinition = new RecipeDefinition(
                "double_carrot",
                "Receta de prueba repetida",
                new[]
                {
                    IngredientType.Carrot,
                    IngredientType.Carrot,
                    IngredientType.Onion
                });

            var repeated = new RecipeProgress(repeatedDefinition);

            Assert(repeated.TryAddIngredient(IngredientType.Carrot),
                "Debe aceptar la primera zanahoria.");

            Assert(repeated.TryAddIngredient(IngredientType.Carrot),
                "Debe aceptar la segunda zanahoria.");

            Assert(!repeated.TryAddIngredient(IngredientType.Carrot),
                "No debe aceptar una tercera zanahoria.");

            Assert(repeated.TryAddIngredient(IngredientType.Onion),
                "Debe aceptar la cebolla.");

            Assert(repeated.IsComplete,
                "La receta repetida debe completarse.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
