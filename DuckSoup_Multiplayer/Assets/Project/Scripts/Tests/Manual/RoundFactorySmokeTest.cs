
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
    /// Comprueba la generación de rondas,
    /// los repartos, los ingredientes
    /// y la reproducibilidad de la fábrica.
    /// </summary>
    public sealed class RoundFactorySmokeTest : MonoBehaviour
    {
        private const int CardsPerPlayer = 3;
        private const int IngredientsPerRecipe = 3;

        private void Start()
        {
            try
            {
                TestRoundGeneration();
                TestDeterministicGeneration();
                TestIndependentRounds();
                TestInvalidConfigurations();

                ShowExampleRound();

                Debug.Log(
                    "[Domain Test] All round factory tests passed.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        /// <summary>
        /// Genera 40 rondas con semillas diferentes
        /// y comprueba sus condiciones.
        /// </summary>
        private static void TestRoundGeneration()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                IRandomSource random =
                    new SystemRandomSource(seed);

                IRoundFactory factory = new RoundFactory(
                    CreateCatalog(),
                    random,
                    CardsPerPlayer);

                RoundModel round = factory.CreateRound(
                    $"test_round_{seed}");

                Assert(round.Player1.Id == PlayerId.Player1,
                    "Jugador 1 incorrecto.");

                Assert(round.Player2.Id == PlayerId.Player2,
                    "Jugador 2 incorrecto.");

                Assert(round.TotalTokenCount == 18,
                    "La mesa debe contener 18 tokens.");

                Assert(round.ConsumedTokenCount == 0,
                    "Los tokens deben comenzar disponibles.");

                var cardIds = new HashSet<string>();
                var required =
                    new Dictionary<IngredientType, int>();

                ValidatePlayer(
                    round.Player1, cardIds, required);

                ValidatePlayer(
                    round.Player2, cardIds, required);

                Assert(cardIds.Count == 6,
                    "Las seis cartas necesitan IDs distintos.");

                var tokenIds = new HashSet<string>();

                var available =
                    new Dictionary<IngredientType, int>();

                foreach (IngredientToken token in round.Tokens)
                {
                    Assert(tokenIds.Add(token.Id),
                        "Existen tokens con IDs duplicados.");

                    Assert(!token.IsRevealed,
                        "Los tokens deben comenzar ocultos.");

                    Assert(!token.IsConsumed,
                        "Los tokens deben comenzar sin consumir.");

                    available.TryGetValue(
                        token.Ingredient, out int count);

                    available[token.Ingredient] = count + 1;
                }

                Assert(available.Count == required.Count,
                    "Hay tipos de ingrediente innecesarios.");

                foreach (KeyValuePair<IngredientType, int> entry
                         in required)
                {
                    available.TryGetValue(
                        entry.Key, out int availableCount);

                    Assert(availableCount == entry.Value,
                        $"Cantidad incorrecta de {entry.Key}.");
                }

                // Integración con nuestro servicio de lectura.
                IRoundReadModel readModel =
                    new RoundReadModelService(round);

                RoundSnapshot snapshot =
                    readModel.GetSnapshot();

                Assert(snapshot.Tokens.Count == 18,
                    "El snapshot tiene un número incorrecto de tokens.");

                foreach (IngredientTokenSnapshot token
                         in snapshot.Tokens)
                {
                    Assert(
                        token.VisibleIngredient ==
                        IngredientType.None,
                        "Un ingrediente oculto es visible.");
                }
            }
        }

        /// <summary>
        /// Comprueba que cada mano tenga tres recetas
        /// diferentes y sin progreso inicial.
        /// </summary>
        private static void ValidatePlayer(
            RoundPlayerModel player,
            HashSet<string> allCardIds,
            Dictionary<IngredientType, int> required)
        {
            Assert(player.Cards.Count == CardsPerPlayer,
                "El jugador no tiene tres cartas.");

            Assert(player.CompletedCardCount == 0,
                "El jugador comienza con cartas completas.");

            var recipeIds = new HashSet<string>();

            foreach (RecipeCardModel card in player.Cards)
            {
                Assert(allCardIds.Add(card.CardId),
                    "ID de carta duplicado.");

                Assert(recipeIds.Add(card.Definition.Id),
                    "El jugador recibió una receta repetida.");

                Assert(!card.IsComplete,
                    "La carta no debe comenzar completada.");

                Assert(
                    card.Definition.RequiredIngredients.Count ==
                    IngredientsPerRecipe,
                    "Una receta de prueba no tiene 3 ingredientes.");

                foreach (IngredientType ingredient in
                         card.Definition.RequiredIngredients)
                {
                    required.TryGetValue(
                        ingredient, out int count);

                    required[ingredient] = count + 1;
                }
            }
        }

        /// <summary>
        /// Dos fábricas con la misma semilla
        /// deben producir el mismo reparto.
        /// </summary>
        private static void TestDeterministicGeneration()
        {
            IRoundFactory factoryA = new RoundFactory(
                CreateCatalog(),
                new SystemRandomSource(12345),
                CardsPerPlayer);

            IRoundFactory factoryB = new RoundFactory(
                CreateCatalog(),
                new SystemRandomSource(12345),
                CardsPerPlayer);

            RoundModel roundA =
                factoryA.CreateRound("replay_round");

            RoundModel roundB =
                factoryB.CreateRound("replay_round");

            ComparePlayers(
                roundA.Player1, roundB.Player1);

            ComparePlayers(
                roundA.Player2, roundB.Player2);

            Assert(
                roundA.Tokens.Count == roundB.Tokens.Count,
                "Las mesas tienen tamaños diferentes.");

            for (int i = 0; i < roundA.Tokens.Count; i++)
            {
                Assert(
                    roundA.Tokens[i].Id ==
                    roundB.Tokens[i].Id,
                    "El orden de tokens no coincide.");

                Assert(
                    roundA.Tokens[i].Ingredient ==
                    roundB.Tokens[i].Ingredient,
                    "Los ingredientes no coinciden.");
            }
        }

        private static void ComparePlayers(
            RoundPlayerModel first,
            RoundPlayerModel second)
        {
            Assert(first.Cards.Count == second.Cards.Count,
                "Las manos tienen tamaños distintos.");

            for (int i = 0; i < first.Cards.Count; i++)
            {
                Assert(
                    first.Cards[i].CardId ==
                    second.Cards[i].CardId,
                    "Los IDs de cartas son diferentes.");

                Assert(
                    first.Cards[i].Definition.Id ==
                    second.Cards[i].Definition.Id,
                    "El reparto no es reproducible.");
            }
        }

        /// <summary>
        /// Comprueba que rondas diferentes
        /// no compartan estados mutables.
        /// </summary>
        private static void TestIndependentRounds()
        {
            IRoundFactory factory = new RoundFactory(
                CreateCatalog(),
                new SystemRandomSource(42),
                CardsPerPlayer);

            RoundModel roundA =
                factory.CreateRound("round_a");

            RoundModel roundB =
                factory.CreateRound("round_b");

            Assert(!ReferenceEquals(roundA, roundB),
                "Las rondas deben ser objetos distintos.");

            var firstRoundTokenIds = new HashSet<string>();

            foreach (IngredientToken token in roundA.Tokens)
            {
                firstRoundTokenIds.Add(token.Id);
            }

            foreach (IngredientToken token in roundB.Tokens)
            {
                Assert(!firstRoundTokenIds.Contains(token.Id),
                    "Hay IDs de token repetidos entre rondas.");
            }

            IngredientToken firstToken = roundA.Tokens[0];

            Assert(firstToken.TryReveal(),
                "No se pudo revelar el token.");

            Assert(!roundB.Tokens[0].IsRevealed,
                "Revelar un token afectó a otra ronda.");

            RecipeCardModel card =
                roundA.Player1.Cards[0];

            IngredientType ingredient =
                card.Definition.RequiredIngredients[0];

            Assert(card.Progress.TryAddIngredient(ingredient),
                "No se pudo modificar el progreso.");

            Assert(
                roundB.Player1.Cards[0]
                    .Progress.CompletedIngredientCount == 0,
                "Dos rondas comparten el progreso de una carta.");
        }

        /// <summary>
        /// Comprueba que la fábrica rechace
        /// configuraciones incorrectas.
        /// </summary>
        private static void TestInvalidConfigurations()
        {
            List<RecipeDefinition> catalog =
                CreateCatalog();

            ExpectArgumentException(
                () => new RoundFactory(
                    new[] { catalog[0], catalog[1] },
                    new SystemRandomSource(1),
                    3),
                "Aceptó un catálogo insuficiente.");

            ExpectArgumentException(
                () => new RoundFactory(
                    new[]
                    {
                        catalog[0],
                        catalog[0],
                        catalog[1],
                        catalog[2]
                    },
                    new SystemRandomSource(1),
                    3),
                "Aceptó IDs de receta duplicados.");

            ExpectArgumentException(
                () => new RoundFactory(
                    catalog,
                    null,
                    3),
                "Aceptó un generador aleatorio nulo.");

            ExpectArgumentException(
                () => new RoundFactory(
                    catalog,
                    new SystemRandomSource(1),
                    0),
                "Aceptó cero cartas por jugador.");

            IRoundFactory factory = new RoundFactory(
                catalog,
                new SystemRandomSource(1),
                3);

            ExpectArgumentException(
                () => factory.CreateRound("  "),
                "Aceptó un ID de ronda vacío.");

            // El orden del catálogo original
            // no debe modificarse.
            string originalFirstId = catalog[0].Id;

            factory.CreateRound("catalog_test");

            Assert(catalog[0].Id == originalFirstId,
                "La fábrica modificó el catálogo original.");
        }

        /// <summary>
        /// Muestra un ejemplo de ronda en consola.
        /// </summary>
        private static void ShowExampleRound()
        {
            IRoundFactory factory = new RoundFactory(
                CreateCatalog(),
                new SystemRandomSource(2026),
                CardsPerPlayer);

            RoundModel round =
                factory.CreateRound("example_round");

            Debug.Log(
                "[RoundFactory] Player 1: " +
                DescribeCards(round.Player1));

            Debug.Log(
                "[RoundFactory] Player 2: " +
                DescribeCards(round.Player2));

            Debug.Log(
                "[RoundFactory] Shared table: " +
                round.TotalTokenCount + " tokens.");
        }

        private static string DescribeCards(
            RoundPlayerModel player)
        {
            var names = new List<string>();

            foreach (RecipeCardModel card in player.Cards)
            {
                names.Add(card.Definition.DisplayName);
            }

            return string.Join(" | ", names);
        }

        /// <summary>
        /// Catálogo provisional de seis recetas.
        /// Las recetas definitivas vendrán de
        /// ScriptableObjects en la siguiente fase.
        /// </summary>
        private static List<RecipeDefinition> CreateCatalog()
        {
            return new List<RecipeDefinition>
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
                    "mushroom",
                    "Sopa de champiñones",
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
                    "Sopa de maíz",
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

