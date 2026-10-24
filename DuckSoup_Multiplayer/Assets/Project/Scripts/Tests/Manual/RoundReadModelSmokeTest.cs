
using System;
using System.Collections.Generic;
using DuckSoup.Core.Randomization;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;
using DuckSoup.Features.Round.Services;
using UnityEngine;

namespace DuckSoup.Tests.Manual
{
    /// <summary>
    /// Pruebas de los contratos de lectura,
    /// snapshots y generación aleatoria.
    /// </summary>
    public sealed class RoundReadModelSmokeTest : MonoBehaviour
    {
        private void Start()
        {
            try
            {
                TestReadModel();
                TestRandomSource();

                Debug.Log(
                    "[Domain Test] All round read model tests passed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private static void TestReadModel()
        {
            RoundModel round = CreateSampleRound();

            IRoundReadModel readModel =
                new RoundReadModelService(round);

            RoundSnapshot initial =
                readModel.GetSnapshot();

            Assert(initial.Id == round.Id,
                "El ID de ronda es incorrecto.");

            Assert(initial.Player1.Cards.Count == 3,
                "Jugador 1 debe tener tres cartas.");

            Assert(initial.Player2.Cards.Count == 3,
                "Jugador 2 debe tener tres cartas.");

            Assert(initial.Tokens.Count == 18,
                "La mesa debe contener 18 tokens.");

            // Los ingredientes ocultos no deben
            // mostrar su identidad en el snapshot.
            Assert(
                initial.Tokens[0].VisibleIngredient ==
                IngredientType.None,
                "El snapshot revela un ingrediente oculto.");

            Assert(!initial.Tokens[0].IsRevealed,
                "El token debe comenzar oculto.");

            // La primera receta necesita dos zanahorias.
            // Debe aparecer como un requisito de 2 unidades,
            // no como dos requisitos duplicados.
            RecipeCardSnapshot cardSnapshot =
                initial.Player1.Cards[0];

            Assert(cardSnapshot.Requirements.Count == 2,
                "Los ingredientes repetidos deben agruparse.");

            Assert(
                cardSnapshot.Requirements[0].RequiredCount == 2,
                "La receta debe necesitar dos zanahorias.");

            // Revelamos un token en el modelo original.
            IngredientToken token = round.Tokens[0];

            Assert(token.TryReveal(),
                "No se pudo revelar el token.");

            RoundSnapshot revealed =
                readModel.GetSnapshot();

            Assert(revealed.Tokens[0].IsRevealed,
                "El nuevo snapshot debe mostrar el revelado.");

            Assert(
                revealed.Tokens[0].VisibleIngredient ==
                token.Ingredient,
                "El nuevo snapshot muestra otro ingrediente.");

            // El snapshot anterior no debe cambiar.
            Assert(
                initial.Tokens[0].VisibleIngredient ==
                IngredientType.None,
                "El snapshot anterior fue modificado.");

            // Modificamos el progreso del jugador 1.
            RecipeCardModel card =
                round.Player1.Cards[0];

            Assert(
                card.Progress.TryAddIngredient(
                    IngredientType.Carrot),
                "No se pudo añadir la zanahoria.");

            RoundSnapshot updated =
                readModel.GetSnapshot();

            Assert(
                updated.Player1.Cards[0]
                    .CompletedIngredientCount == 1,
                "El nuevo snapshot no refleja el progreso.");

            Assert(
                updated.Player1.Cards[0]
                    .Requirements[0].CompletedCount == 1,
                "El requisito no muestra el progreso.");

            Assert(
                initial.Player1.Cards[0]
                    .CompletedIngredientCount == 0,
                "El snapshot antiguo cambió.");

            Assert(
                updated.Player2.Cards[0]
                    .CompletedIngredientCount == 0,
                "El progreso del jugador 2 fue modificado.");
        }

        private static void TestRandomSource()
        {
            IRandomSource first =
                new SystemRandomSource(12345);

            IRandomSource second =
                new SystemRandomSource(12345);

            for (int i = 0; i < 20; i++)
            {
                int a = first.NextInt(0, 100);
                int b = second.NextInt(0, 100);

                Assert(a == b,
                    "La misma semilla debe reproducir " +
                    "la misma secuencia.");

                Assert(a >= 0 && a < 100,
                    "El valor está fuera del rango.");
            }
        }

        private static RoundModel CreateSampleRound()
        {
            var recipes = new[]
            {
                new RecipeDefinition(
                    "double_carrot",
                    "Sopa de zanahoria",
                    new[]
                    {
                        IngredientType.Carrot,
                        IngredientType.Carrot,
                        IngredientType.Onion
                    }),

                new RecipeDefinition(
                    "forest",
                    "Sopa del bosque",
                    new[]
                    {
                        IngredientType.Potato,
                        IngredientType.Mushroom,
                        IngredientType.Onion
                    }),

                new RecipeDefinition(
                    "garden",
                    "Sopa de la huerta",
                    new[]
                    {
                        IngredientType.Corn,
                        IngredientType.Pumpkin,
                        IngredientType.Leek
                    })
            };

            RoundPlayerModel player1 =
                CreatePlayer(PlayerId.Player1, "p1", recipes);

            RoundPlayerModel player2 =
                CreatePlayer(PlayerId.Player2, "p2", recipes);

            var tokens = new List<IngredientToken>();

            AddPlayerTokens(player1, tokens);
            AddPlayerTokens(player2, tokens);

            return new RoundModel(
                "round_read_test",
                player1,
                player2,
                tokens);
        }

        private static RoundPlayerModel CreatePlayer(
            PlayerId id,
            string prefix,
            RecipeDefinition[] recipes)
        {
            var cards = new List<RecipeCardModel>();

            for (int i = 0; i < recipes.Length; i++)
            {
                cards.Add(new RecipeCardModel(
                    $"{prefix}_card_{i + 1}",
                    recipes[i]));
            }

            return new RoundPlayerModel(
                id,
                cards,
                recipes.Length);
        }

        private static void AddPlayerTokens(
            RoundPlayerModel player,
            List<IngredientToken> tokens)
        {
            foreach (RecipeCardModel card in player.Cards)
            {
                foreach (IngredientType ingredient in
                         card.Definition.RequiredIngredients)
                {
                    tokens.Add(new IngredientToken(
                        $"token_{tokens.Count + 1:000}",
                        ingredient));
                }
            }
        }

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
