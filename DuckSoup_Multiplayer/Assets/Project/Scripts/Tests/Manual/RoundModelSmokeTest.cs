
using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Models;
using UnityEngine;

namespace DuckSoup.Tests.Manual
{
    /// <summary>
    /// Pruebas manuales para comprobar
    /// el modelo de ronda compartida.
    /// </summary>
    public sealed class RoundModelSmokeTest : MonoBehaviour
    {
        private void Start()
        {
            try
            {
                TestValidRound();
                TestDuplicateTokenIds();
                TestInsufficientIngredients();
                TestDuplicateCardIds();
                TestInvalidPlayerAssignment();

                Debug.Log(
                    "[Domain Test] All round tests passed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private static void TestValidRound()
        {
            CreateValidSetup(
                out RoundPlayerModel player1,
                out RoundPlayerModel player2,
                out List<IngredientToken> tokens);

            var round = new RoundModel(
                "round_001",
                player1,
                player2,
                tokens);

            Assert(round.Id == "round_001",
                "El ID de la ronda es incorrecto.");

            Assert(round.Player1.Id == PlayerId.Player1,
                "Jugador 1 mal asignado.");

            Assert(round.Player2.Id == PlayerId.Player2,
                "Jugador 2 mal asignado.");

            Assert(round.TotalTokenCount == 18,
                "La mesa debe tener 18 ingredientes.");

            Assert(round.ConsumedTokenCount == 0,
                "Ningún token debe comenzar consumido.");

            // Buscar jugadores.
            Assert(round.TryGetPlayer(
                PlayerId.Player1,
                out RoundPlayerModel foundPlayer),
                "No se encontró al jugador 1.");

            Assert(ReferenceEquals(foundPlayer, player1),
                "Se encontró un jugador incorrecto.");

            Assert(!round.TryGetPlayer(
                PlayerId.None, out _),
                "No debe encontrar jugadores inválidos.");

            // Buscar cartas.
            Assert(round.TryGetCard(
                PlayerId.Player1,
                "p1_card_1",
                out RecipeCardModel foundCard),
                "No se encontró la carta.");

            Assert(ReferenceEquals(
                foundCard, player1.Cards[0]),
                "Se encontró una carta incorrecta.");

            Assert(!round.TryGetCard(
                PlayerId.Player2,
                "p1_card_1",
                out _),
                "Un jugador no debe acceder " +
                "a las cartas del otro.");

            // Buscar tokens.
            IngredientToken firstToken = tokens[0];

            Assert(round.TryGetToken(
                firstToken.Id,
                out IngredientToken foundToken),
                "No se encontró el token.");

            Assert(ReferenceEquals(
                firstToken, foundToken),
                "La ronda debe compartir el mismo token.");

            Assert(!round.TryGetToken(
                "missing_token", out _),
                "No debe encontrar tokens inexistentes.");

            // Comprobar actualización del estado
            // de un token compartido.
            Assert(firstToken.TryReveal(),
                "No se pudo revelar el token.");

            Assert(firstToken.TryConsume(),
                "No se pudo consumir el token.");

            Assert(round.ConsumedTokenCount == 1,
                "La ronda debe contar el token consumido.");

            Assert(!round.Tokens[1].IsConsumed,
                "Los demás tokens no deben modificarse.");
        }

        private static void TestDuplicateTokenIds()
        {
            CreateValidSetup(
                out RoundPlayerModel player1,
                out RoundPlayerModel player2,
                out List<IngredientToken> tokens);

            tokens.Add(new IngredientToken(
                tokens[0].Id,
                IngredientType.Carrot));

            ExpectArgumentException(
                () =>
                {
                    new RoundModel(
                        "round_duplicate",
                        player1,
                        player2,
                        tokens);
                },
                "Se aceptó un token duplicado.");
        }

        private static void TestInsufficientIngredients()
        {
            CreateValidSetup(
                out RoundPlayerModel player1,
                out RoundPlayerModel player2,
                out List<IngredientToken> tokens);

            // Quitamos una unidad necesaria.
            tokens.RemoveAt(0);

            ExpectArgumentException(
                () =>
                {
                    new RoundModel(
                        "round_shortage",
                        player1,
                        player2,
                        tokens);
                },
                "Se aceptó una ronda sin ingredientes suficientes.");
        }

        private static void TestDuplicateCardIds()
        {
            CreateValidSetup(
                out RoundPlayerModel player1,
                out RoundPlayerModel player2,
                out List<IngredientToken> tokens);

            var cards = new[]
            {
                // Repetimos el ID de una carta del jugador 1.
                new RecipeCardModel(
                    player1.Cards[0].CardId,
                    player2.Cards[0].Definition),

                player2.Cards[1],
                player2.Cards[2]
            };

            var invalidPlayer2 = new RoundPlayerModel(
                PlayerId.Player2,
                cards,
                3);

            ExpectArgumentException(
                () =>
                {
                    new RoundModel(
                        "round_duplicate_card",
                        player1,
                        invalidPlayer2,
                        tokens);
                },
                "Se aceptaron IDs duplicados entre jugadores.");
        }

        private static void TestInvalidPlayerAssignment()
        {
            CreateValidSetup(
                out RoundPlayerModel player1,
                out RoundPlayerModel player2,
                out List<IngredientToken> tokens);

            ExpectArgumentException(
                () =>
                {
                    new RoundModel(
                        "round_invalid_players",
                        player2,
                        player1,
                        tokens);
                },
                "Se aceptaron jugadores intercambiados.");
        }

        /// <summary>
        /// Crea una ronda determinista de prueba.
        /// No es el generador aleatorio definitivo.
        /// </summary>
        private static void CreateValidSetup(
            out RoundPlayerModel player1,
            out RoundPlayerModel player2,
            out List<IngredientToken> tokens)
        {
            RecipeDefinition[] definitions =
            {
                new RecipeDefinition(
                    "classic",
                    "Sopa clásica",
                    new[]
                    {
                        IngredientType.Carrot,
                        IngredientType.Potato,
                        IngredientType.Onion
                    }),

                new RecipeDefinition(
                    "forest",
                    "Sopa del bosque",
                    new[]
                    {
                        IngredientType.Mushroom,
                        IngredientType.Onion,
                        IngredientType.Potato
                    }),

                new RecipeDefinition(
                    "sun",
                    "Sopa del sol",
                    new[]
                    {
                        IngredientType.Corn,
                        IngredientType.Pumpkin,
                        IngredientType.Carrot
                    })
            };

            player1 = CreatePlayer(
                PlayerId.Player1, "p1", definitions);

            player2 = CreatePlayer(
                PlayerId.Player2, "p2", definitions);

            tokens = new List<IngredientToken>();

            AddTokensForPlayer(player1, tokens);
            AddTokensForPlayer(player2, tokens);
        }

        private static RoundPlayerModel CreatePlayer(
            PlayerId playerId,
            string prefix,
            RecipeDefinition[] definitions)
        {
            var cards = new List<RecipeCardModel>();

            for (int i = 0; i < definitions.Length; i++)
            {
                cards.Add(new RecipeCardModel(
                    $"{prefix}_card_{i + 1}",
                    definitions[i]));
            }

            return new RoundPlayerModel(
                playerId,
                cards,
                definitions.Length);
        }

        private static void AddTokensForPlayer(
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

        private static void ExpectArgumentException(
            Action action,
            string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
