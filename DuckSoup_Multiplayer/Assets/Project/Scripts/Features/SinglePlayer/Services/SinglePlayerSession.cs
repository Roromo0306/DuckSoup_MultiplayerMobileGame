
using System;
using System.Collections.Generic;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;
using DuckSoup.Features.SinglePlayer.Models;

namespace DuckSoup.Features.SinglePlayer.Services
{
    /// <summary>
    /// Gestiona el modo individual:
    /// recetas, ingredientes, puntuacion,
    /// tiempo y reposicion automatica.
    /// </summary>
    public sealed class SinglePlayerSession
    {
        public const int RecipeSlots = 3;
        public const int TokenSlots = 9;

        private readonly List<RecipeDefinition> catalog;
        private readonly IRandomSource random;

        private readonly RecipeCardModel[] cards =
            new RecipeCardModel[RecipeSlots];

        private readonly IngredientToken[] tokens =
            new IngredientToken[TokenSlots];

        // Proxima receta preparada para cada carta.
        private readonly RecipeDefinition[] upcomingRecipes =
            new RecipeDefinition[RecipeSlots];

        // Temporizadores para ocultar tokens.
        private readonly float[] hideTimers =
            new float[TokenSlots];

        // Temporizadores para crear nuevos tokens.
        private readonly float[] tokenRefillTimers =
            new float[TokenSlots];

        // Ingrediente que aparecera en cada hueco.
        private readonly IngredientType[] pendingIngredients =
            new IngredientType[TokenSlots];

        // Temporizadores para reponer recetas.
        private readonly float[] recipeReplacementTimers =
            new float[RecipeSlots];

        // Reservas multitactiles.
        private readonly Dictionary<string, int> reservations =
            new Dictionary<string, int>();

        private readonly Dictionary<int, string> pointerTokens =
            new Dictionary<int, string>();

        private readonly float revealDuration;
        private readonly float recipeReplacementDelay;
        private readonly float tokenRefillDelay;

        private int nextCardId;
        private int nextTokenId;

        public float RemainingSeconds { get; private set; }

        public int CompletedRecipes { get; private set; }

        public bool IsTimeUp { get; private set; }

        public SinglePlayerSession(
            IEnumerable<RecipeDefinition> definitions,
            IRandomSource randomSource,
            float durationSeconds = 60f,
            float revealSeconds = 3f,
            float recipeReplacementSeconds = 0.8f,
            float tokenRefillSeconds = 1.2f)
        {
            if (definitions == null)
                throw new ArgumentNullException(
                    nameof(definitions));

            random = randomSource ??
                throw new ArgumentNullException(
                    nameof(randomSource));

            ValidateDuration(
                durationSeconds, nameof(durationSeconds));

            ValidateDuration(
                revealSeconds, nameof(revealSeconds));

            ValidateDuration(
                recipeReplacementSeconds,
                nameof(recipeReplacementSeconds));

            ValidateDuration(
                tokenRefillSeconds,
                nameof(tokenRefillSeconds));

            RemainingSeconds = durationSeconds;

            revealDuration = revealSeconds;
            recipeReplacementDelay = recipeReplacementSeconds;
            tokenRefillDelay = tokenRefillSeconds;

            catalog = new List<RecipeDefinition>();

            var ids = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (RecipeDefinition recipe in definitions)
            {
                if (recipe == null)
                    throw new ArgumentException(
                        "El catalogo contiene una receta nula.");

                if (!ids.Add(recipe.Id))
                    throw new ArgumentException(
                        "Hay IDs de receta duplicados.");

                if (recipe.RequiredIngredients.Count != 3)
                    throw new ArgumentException(
                        "Las recetas deben tener " +
                        "tres ingredientes.");

                catalog.Add(recipe);
            }

            // Tres recetas visibles y otras
            // tres preparadas para el futuro.
            if (catalog.Count < RecipeSlots * 2)
                throw new ArgumentException(
                    "Se necesitan al menos seis recetas.");

            for (int i = 0; i < TokenSlots; i++)
            {
                hideTimers[i] = -1f;
                tokenRefillTimers[i] = -1f;
                pendingIngredients[i] = IngredientType.None;
            }

            for (int i = 0; i < RecipeSlots; i++)
            {
                recipeReplacementTimers[i] = -1f;
            }

            CreateInitialBoard();
        }

        private static void ValidateDuration(
            float seconds,
            string parameterName)
        {
            if (seconds <= 0f ||
                float.IsNaN(seconds) ||
                float.IsInfinity(seconds))
            {
                throw new ArgumentOutOfRangeException(
                    parameterName);
            }
        }

        // ===================================
        // INITIAL BOARD
        // ===================================

        private void CreateInitialBoard()
        {
            var candidates =
                new List<RecipeDefinition>(catalog);

            // Repartimos tres recetas distintas.
            for (int i = 0; i < RecipeSlots; i++)
            {
                int selected = random.NextInt(
                    i, candidates.Count);

                RecipeDefinition temp = candidates[i];
                candidates[i] = candidates[selected];
                candidates[selected] = temp;

                cards[i] = CreateCard(candidates[i]);
            }

            // Generamos los nueve ingredientes
            // necesarios para las cartas iniciales.
            var ingredients = new List<IngredientType>();

            foreach (RecipeCardModel card in cards)
            {
                ingredients.AddRange(
                    card.Definition.RequiredIngredients);
            }

            Shuffle(ingredients);

            for (int i = 0; i < TokenSlots; i++)
            {
                tokens[i] = CreateToken(ingredients[i]);
            }

            // Preparamos una receta futura por carta.
            for (int i = 0; i < RecipeSlots; i++)
            {
                upcomingRecipes[i] = ChooseUpcomingRecipe();
            }
        }

        private RecipeCardModel CreateCard(
            RecipeDefinition definition)
        {
            nextCardId++;

            return new RecipeCardModel(
                $"sp_card_{nextCardId:0000}",
                definition);
        }

        private IngredientToken CreateToken(
            IngredientType ingredient)
        {
            nextTokenId++;

            return new IngredientToken(
                $"sp_token_{nextTokenId:0000}",
                ingredient);
        }

        /// <summary>
        /// Selecciona una receta que no este
        /// visible ni preparada en otro espacio.
        /// </summary>
        private RecipeDefinition ChooseUpcomingRecipe()
        {
            var usedIds = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (RecipeCardModel card in cards)
            {
                usedIds.Add(card.Definition.Id);
            }

            foreach (RecipeDefinition recipe in upcomingRecipes)
            {
                if (recipe != null)
                    usedIds.Add(recipe.Id);
            }

            var available = new List<RecipeDefinition>();

            foreach (RecipeDefinition recipe in catalog)
            {
                if (!usedIds.Contains(recipe.Id))
                    available.Add(recipe);
            }

            if (available.Count == 0)
                throw new InvalidOperationException(
                    "No hay recetas disponibles " +
                    "para preparar la siguiente.");

            return available[
                random.NextInt(0, available.Count)];
        }

        private void Shuffle<T>(List<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(0, i + 1);

                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }

        // ===================================
        // INGREDIENT INTERACTION
        // ===================================

        public bool TryBeginGesture(
            string tokenId,
            int pointerId)
        {
            if (IsTimeUp ||
                string.IsNullOrWhiteSpace(tokenId) ||
                reservations.ContainsKey(tokenId) ||
                pointerTokens.ContainsKey(pointerId))
            {
                return false;
            }

            int index = FindTokenIndex(tokenId);

            if (index < 0)
                return false;

            IngredientToken token = tokens[index];

            if (token.IsConsumed)
                return false;

            if (!token.IsRevealed && !token.TryReveal())
                return false;

            reservations.Add(tokenId, pointerId);
            pointerTokens.Add(pointerId, tokenId);

            // No ocultar mientras se arrastra.
            hideTimers[index] = -1f;

            return true;
        }

        /// <summary>
        /// Entrega un ingrediente a una receta.
        ///
        /// Si se acepta:
        /// - Consume el ingrediente actual.
        /// - Deja su hueco temporalmente vacio.
        /// - Prepara un ingrediente nuevo.
        /// - Actualiza el progreso.
        /// </summary>
        public bool TryDeliver(
            string tokenId,
            int pointerId,
            string cardId,
            out bool recipeCompleted)
        {
            recipeCompleted = false;

            if (!HasReservation(tokenId, pointerId))
                return false;

            try
            {
                if (IsTimeUp)
                    return false;

                int tokenIndex = FindTokenIndex(tokenId);
                int cardIndex = FindCardIndex(cardId);

                if (tokenIndex < 0 || cardIndex < 0)
                    return false;

                if (recipeReplacementTimers[cardIndex] >= 0f)
                    return false;

                IngredientToken token = tokens[tokenIndex];
                RecipeCardModel card = cards[cardIndex];

                if (token.IsConsumed ||
                    !token.IsRevealed ||
                    !card.Progress.NeedsIngredient(
                        token.Ingredient))
                {
                    return false;
                }

                if (!token.TryConsume())
                    return false;

                if (!card.Progress.TryAddIngredient(
                    token.Ingredient))
                {
                    throw new InvalidOperationException(
                        "Error al actualizar la receta.");
                }

                // Cada ingrediente utilizado prepara
                // uno de la proxima receta.
                int deliveredIndex =
                    card.Progress.CompletedIngredientCount - 1;

                IngredientType nextIngredient =
                    upcomingRecipes[cardIndex]
                        .RequiredIngredients[deliveredIndex];

                // Liberamos el espacio de la mesa.
                tokens[tokenIndex] = null;

                hideTimers[tokenIndex] = -1f;

                // Programamos la nueva ficha
                // para este mismo espacio.
                pendingIngredients[tokenIndex] =
                    nextIngredient;

                tokenRefillTimers[tokenIndex] =
                    tokenRefillDelay;

                // Comprobar si termino la receta.
                if (card.IsComplete)
                {
                    CompletedRecipes++;
                    recipeCompleted = true;

                    recipeReplacementTimers[cardIndex] =
                        recipeReplacementDelay;
                }

                return true;
            }
            finally
            {
                CancelGesture(tokenId, pointerId);
            }
        }

        public void CancelGesture(
            string tokenId,
            int pointerId)
        {
            if (!HasReservation(tokenId, pointerId))
                return;

            reservations.Remove(tokenId);
            pointerTokens.Remove(pointerId);

            int index = FindTokenIndex(tokenId);

            if (index >= 0 &&
                tokens[index].IsRevealed &&
                !tokens[index].IsConsumed &&
                !IsTimeUp)
            {
                hideTimers[index] = revealDuration;
            }
        }

        public void CancelAllGestures()
        {
            foreach (string tokenId in reservations.Keys)
            {
                int index = FindTokenIndex(tokenId);

                if (index >= 0 &&
                    tokens[index].IsRevealed &&
                    !tokens[index].IsConsumed &&
                    !IsTimeUp)
                {
                    hideTimers[index] = revealDuration;
                }
            }

            reservations.Clear();
            pointerTokens.Clear();
        }

        // ===================================
        // GAME TIMERS
        // ===================================

        public bool Advance(float deltaTime)
        {
            if (deltaTime < 0f ||
                float.IsNaN(deltaTime) ||
                float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime));
            }

            if (IsTimeUp || deltaTime == 0f)
                return false;

            RemainingSeconds = Math.Max(
                0f, RemainingSeconds - deltaTime);

            if (RemainingSeconds <= 0f)
            {
                IsTimeUp = true;
                CancelAllGestures();
                return true;
            }

            bool changed = false;

            // 1. Ocultar fichas reveladas.
            for (int i = 0; i < TokenSlots; i++)
            {
                if (tokens[i] == null ||
                    hideTimers[i] < 0f)
                {
                    continue;
                }

                if (reservations.ContainsKey(tokens[i].Id))
                    continue;

                hideTimers[i] -= deltaTime;

                if (hideTimers[i] <= 0f)
                {
                    changed |= tokens[i].TryHide();
                    hideTimers[i] = -1f;
                }
            }

            // 2. Reponer CADA token utilizado
            // cuando termine su propio temporizador.
            for (int i = 0; i < TokenSlots; i++)
            {
                if (tokenRefillTimers[i] < 0f)
                    continue;

                tokenRefillTimers[i] -= deltaTime;

                if (tokenRefillTimers[i] <= 0f)
                {
                    if (tokens[i] != null)
                    {
                        throw new InvalidOperationException(
                            "El hueco de reposicion " +
                            "ya contiene un ingrediente.");
                    }

                    tokens[i] = CreateToken(
                        pendingIngredients[i]);

                    pendingIngredients[i] =
                        IngredientType.None;

                    tokenRefillTimers[i] = -1f;
                    hideTimers[i] = -1f;

                    changed = true;
                }
            }

            // 3. Reponer recetas completadas.
            for (int i = 0; i < RecipeSlots; i++)
            {
                if (recipeReplacementTimers[i] < 0f)
                    continue;

                recipeReplacementTimers[i] -= deltaTime;

                if (recipeReplacementTimers[i] <= 0f)
                {
                    ReplaceCompletedCard(i);

                    recipeReplacementTimers[i] = -1f;

                    changed = true;
                }
            }

            return changed;
        }

        // ===================================
        // RECIPE REPLACEMENT
        // ===================================

        /// <summary>
        /// La proxima receta ya estaba preparada.
        /// Sus ingredientes se van incorporando
        /// a la mesa a traves de los token refills.
        /// </summary>
        private void ReplaceCompletedCard(int cardIndex)
        {
            RecipeDefinition nextRecipe =
                upcomingRecipes[cardIndex];

            if (nextRecipe == null)
            {
                throw new InvalidOperationException(
                    "No existe una receta preparada.");
            }

            // Reemplazamos solamente la carta.
            cards[cardIndex] = CreateCard(nextRecipe);

            // Preparamos la receta que vendra despues.
            upcomingRecipes[cardIndex] = null;

            upcomingRecipes[cardIndex] =
                ChooseUpcomingRecipe();
        }

        // ===================================
        // LOOKUP
        // ===================================

        private int FindTokenIndex(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return -1;

            for (int i = 0; i < TokenSlots; i++)
            {
                if (tokens[i] != null &&
                    tokens[i].Id == tokenId)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindCardIndex(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                return -1;

            for (int i = 0; i < RecipeSlots; i++)
            {
                if (cards[i].CardId == cardId)
                    return i;
            }

            return -1;
        }

        private bool HasReservation(
            string tokenId,
            int pointerId)
        {
            if (string.IsNullOrWhiteSpace(tokenId))
                return false;

            return reservations.TryGetValue(
                tokenId, out int owner)
                && owner == pointerId
                && pointerTokens.TryGetValue(
                    pointerId, out string reservedId)
                && reservedId == tokenId;
        }

        // ===================================
        // READ-ONLY SNAPSHOT
        // ===================================

        public SinglePlayerSnapshot GetSnapshot()
        {
            var cardSnapshots =
                new List<RecipeCardSnapshot>();

            foreach (RecipeCardModel card in cards)
            {
                var requirements =
                    new List<IngredientRequirementSnapshot>();

                var visited =
                    new HashSet<IngredientType>();

                foreach (IngredientType ingredient in
                         card.Definition.RequiredIngredients)
                {
                    if (!visited.Add(ingredient))
                        continue;

                    requirements.Add(
                        new IngredientRequirementSnapshot(
                            ingredient,
                            card.Progress.GetRequiredCount(
                                ingredient),
                            card.Progress.GetCompletedCount(
                                ingredient)));
                }

                cardSnapshots.Add(new RecipeCardSnapshot(
                    card.CardId,
                    card.Definition.Id,
                    card.Definition.DisplayName,
                    requirements));
            }

            var tokenSnapshots =
                new List<IngredientTokenSnapshot>();

            foreach (IngredientToken token in tokens)
            {
                if (token == null)
                {
                    // Hueco que espera reposicion.
                    tokenSnapshots.Add(null);
                    continue;
                }

                tokenSnapshots.Add(
                    new IngredientTokenSnapshot(
                        token.Id,
                        token.Ingredient,
                        token.IsRevealed,
                        token.IsConsumed));
            }

            return new SinglePlayerSnapshot(
                cardSnapshots,
                tokenSnapshots,
                CompletedRecipes,
                RemainingSeconds);
        }
    }
}

