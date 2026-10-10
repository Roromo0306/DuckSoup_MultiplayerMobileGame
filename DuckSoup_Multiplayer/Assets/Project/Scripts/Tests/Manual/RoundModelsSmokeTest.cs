
using System;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Models;
using UnityEngine;

namespace DuckSoup.Tests.Manual
{
    /// <summary>
    /// Pruebas manuales de los modelos
    /// de tokens, cartas y jugadores.
    /// </summary>
    public sealed class RoundModelsSmokeTest : MonoBehaviour
    {
        private void Start()
        {
            try
            {
                TestIngredientTokens();
                TestPlayersAndCards();

                Debug.Log(
                    "[Domain Test] All round model tests passed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private static void TestIngredientTokens()
        {
            var token = new IngredientToken(
                "token_001",
                IngredientType.Carrot);

            Assert(!token.IsRevealed,
                "El token debe comenzar oculto.");

            Assert(!token.IsConsumed,
                "El token no debe comenzar consumido.");

            Assert(!token.TryConsume(),
                "No debe consumirse un token oculto.");

            Assert(token.TryReveal(),
                "El token debe poder revelarse.");

            Assert(!token.TryReveal(),
                "No debe revelarse dos veces.");

            Assert(token.TryConsume(),
                "El token revelado debe poder consumirse.");

            Assert(!token.TryConsume(),
                "No debe consumirse dos veces.");

            var secondToken = new IngredientToken(
                "token_002",
                IngredientType.Carrot);

            Assert(!secondToken.IsConsumed,
                "Consumir un token no debe afectar a otro.");

            ExpectException<ArgumentException>(
                () => new IngredientToken(
                    "token_invalid",
                    IngredientType.None),
                "No debe aceptar un ingrediente inválido.");
        }

        private static void TestPlayersAndCards()
        {
            var recipe1 = new RecipeDefinition(
                "soup_001",
                "Sopa clásica",
                new[]
                {
                    IngredientType.Carrot,
                    IngredientType.Potato,
                    IngredientType.Onion
                });

            var recipe2 = new RecipeDefinition(
                "soup_002",
                "Sopa de setas",
                new[]
                {
                    IngredientType.Mushroom,
                    IngredientType.Onion,
                    IngredientType.Potato
                });

            var recipe3 = new RecipeDefinition(
                "soup_003",
                "Sopa del sol",
                new[]
                {
                    IngredientType.Corn,
                    IngredientType.Pumpkin,
                    IngredientType.Carrot
                });

            var player1Cards = new[]
            {
                new RecipeCardModel("p1_card1", recipe1),
                new RecipeCardModel("p1_card2", recipe2),
                new RecipeCardModel("p1_card3", recipe3)
            };

            var player2Cards = new[]
            {
                new RecipeCardModel("p2_card1", recipe1),
                new RecipeCardModel("p2_card2", recipe2),
                new RecipeCardModel("p2_card3", recipe3)
            };

            var player1 = new RoundPlayerModel(
                PlayerId.Player1,
                player1Cards,
                3);

            var player2 = new RoundPlayerModel(
                PlayerId.Player2,
                player2Cards,
                3);

            Assert(player1.Cards.Count == 3,
                "Jugador 1 debe tener tres cartas.");

            Assert(player2.Cards.Count == 3,
                "Jugador 2 debe tener tres cartas.");

            Assert(player1.CompletedCardCount == 0,
                "El jugador debe comenzar sin recetas.");

            Assert(!player1.HasCompletedAllCards,
                "El jugador no debe comenzar como ganador.");

            Assert(ReferenceEquals(
                player1Cards[0].Definition,
                player2Cards[0].Definition),
                "Ambas cartas deben compartir definición.");

            Assert(!ReferenceEquals(
                player1Cards[0].Progress,
                player2Cards[0].Progress),
                "Las cartas deben tener progresos distintos.");

            Assert(player1.TryGetCard(
                "p1_card1",
                out RecipeCardModel foundCard),
                "Debe encontrar la carta por ID.");

            Assert(ReferenceEquals(
                foundCard, player1Cards[0]),
                "La carta encontrada debe ser la correcta.");

            // Completamos la primera receta del jugador 1.
            CompleteCard(player1Cards[0]);

            Assert(player1.CompletedCardCount == 1,
                "Debe contar una receta completada.");

            Assert(player2.CompletedCardCount == 0,
                "El segundo jugador no debe verse afectado.");

            // Completamos las dos cartas restantes.
            CompleteCard(player1Cards[1]);
            CompleteCard(player1Cards[2]);

            Assert(player1.HasCompletedAllCards,
                "Jugador 1 debe completar sus tres cartas.");

            Assert(!player2.HasCompletedAllCards,
                "Jugador 2 debe conservar sus recetas.");

            ExpectException<ArgumentException>(
                () => new RoundPlayerModel(
                    PlayerId.Player1,
                    new[] { player1Cards[0] },
                    3),
                "Debe rechazar una mano incompleta.");

            ExpectException<ArgumentException>(
                () => new RoundPlayerModel(
                    PlayerId.Player1,
                    new[]
                    {
                        new RecipeCardModel("same", recipe1),
                        new RecipeCardModel("same", recipe2),
                        new RecipeCardModel("third", recipe3)
                    },
                    3),
                "Debe rechazar IDs duplicados.");

            ExpectException<ArgumentOutOfRangeException>(
                () => new RoundPlayerModel(
                    PlayerId.None,
                    player1Cards,
                    3),
                "Debe rechazar jugadores inválidos.");
        }

        private static void CompleteCard(
            RecipeCardModel card)
        {
            foreach (IngredientType ingredient in
                     card.Definition.RequiredIngredients)
            {
                Assert(
                    card.Progress.TryAddIngredient(ingredient),
                    "No se pudo completar la carta.");
            }

            Assert(card.IsComplete,
                "La carta debe estar completada.");
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

        private static void ExpectException<TException>(
            Action action,
            string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
