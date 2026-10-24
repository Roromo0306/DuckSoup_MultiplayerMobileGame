
using System;
using System.Collections.Generic;
using DuckSoup.Core.Randomization;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.Round.Services
{
    /// <summary>
    /// Construye rondas válidas y reproducibles.
    ///
    /// Selecciona recetas, crea jugadores,
    /// genera los ingredientes necesarios
    /// y mezcla los tokens de la mesa.
    /// </summary>
    public sealed class RoundFactory : IRoundFactory
    {
        private readonly IReadOnlyList<RecipeDefinition> catalog;
        private readonly IRandomSource randomSource;
        private readonly int cardsPerPlayer;

        public RoundFactory(
            IEnumerable<RecipeDefinition> recipes,
            IRandomSource randomSource,
            int cardsPerPlayer = 3)
        {
            if (recipes == null)
            {
                throw new ArgumentNullException(
                    nameof(recipes));
            }

            this.randomSource = randomSource ??
                throw new ArgumentNullException(
                    nameof(randomSource));

            if (cardsPerPlayer <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cardsPerPlayer));
            }

            var recipeList =
                new List<RecipeDefinition>();

            var recipeIds =
                new HashSet<string>(StringComparer.Ordinal);

            foreach (RecipeDefinition recipe in recipes)
            {
                if (recipe == null)
                {
                    throw new ArgumentException(
                        "El catálogo contiene una receta nula.",
                        nameof(recipes));
                }

                if (!recipeIds.Add(recipe.Id))
                {
                    throw new ArgumentException(
                        $"ID de receta duplicado: {recipe.Id}",
                        nameof(recipes));
                }

                recipeList.Add(recipe);
            }

            if (recipeList.Count < cardsPerPlayer)
            {
                throw new ArgumentException(
                    "No existen suficientes recetas distintas " +
                    "para repartir las cartas.",
                    nameof(recipes));
            }

            catalog = recipeList.AsReadOnly();
            this.cardsPerPlayer = cardsPerPlayer;
        }

        /// <summary>
        /// Crea una nueva ronda con dos jugadores
        /// y una mesa compartida.
        ///
        /// El ID debe ser único por ronda
        /// dentro de la partida.
        /// </summary>
        public RoundModel CreateRound(string roundId)
        {
            if (string.IsNullOrWhiteSpace(roundId))
            {
                throw new ArgumentException(
                    "La ronda necesita un ID válido.",
                    nameof(roundId));
            }

            string id = roundId.Trim();

            // Cada jugador recibe una selección
            // independiente de recetas.
            RoundPlayerModel player1 = CreatePlayer(
                PlayerId.Player1,
                id,
                "p1");

            RoundPlayerModel player2 = CreatePlayer(
                PlayerId.Player2,
                id,
                "p2");

            // Generamos exactamente los ingredientes
            // requeridos por las seis cartas.
            var tokens = new List<IngredientToken>();

            AddRequiredTokens(id, player1, tokens);
            AddRequiredTokens(id, player2, tokens);

            // Aleatorizamos el orden de los tokens.
            ShuffleTokens(tokens);

            // RoundModel realiza la validación final.
            return new RoundModel(
                id,
                player1,
                player2,
                tokens);
        }

        /// <summary>
        /// Selecciona recetas sin repetir
        /// dentro de la mano de un jugador.
        /// </summary>
        private RoundPlayerModel CreatePlayer(
            PlayerId playerId,
            string roundId,
            string playerPrefix)
        {
            // Copiamos el catálogo para no modificarlo.
            var candidates =
                new List<RecipeDefinition>(catalog);

            var cards =
                new List<RecipeCardModel>(cardsPerPlayer);

            for (int i = 0; i < cardsPerPlayer; i++)
            {
                // Selección parcial de Fisher-Yates.
                int selectedIndex = randomSource.NextInt(
                    i,
                    candidates.Count);

                RecipeDefinition temp = candidates[i];

                candidates[i] = candidates[selectedIndex];
                candidates[selectedIndex] = temp;

                string cardId =
                    $"{roundId}_{playerPrefix}_card_{i + 1:00}";

                cards.Add(new RecipeCardModel(
                    cardId,
                    candidates[i]));
            }

            return new RoundPlayerModel(
                playerId,
                cards,
                cardsPerPlayer);
        }

        /// <summary>
        /// Crea un token por cada ingrediente
        /// requerido por las cartas del jugador.
        /// </summary>
        private static void AddRequiredTokens(
            string roundId,
            RoundPlayerModel player,
            List<IngredientToken> tokens)
        {
            foreach (RecipeCardModel card in player.Cards)
            {
                foreach (IngredientType ingredient in
                         card.Definition.RequiredIngredients)
                {
                    string tokenId =
                        $"{roundId}_token_{tokens.Count + 1:000}";

                    tokens.Add(new IngredientToken(
                        tokenId,
                        ingredient));
                }
            }
        }

        /// <summary>
        /// Mezcla los tokens utilizando
        /// el algoritmo Fisher-Yates.
        /// </summary>
        private void ShuffleTokens(
            List<IngredientToken> tokens)
        {
            for (int i = tokens.Count - 1; i > 0; i--)
            {
                int selectedIndex =
                    randomSource.NextInt(0, i + 1);

                IngredientToken temp = tokens[i];

                tokens[i] = tokens[selectedIndex];
                tokens[selectedIndex] = temp;
            }
        }
    }
}
