using System;
using System.Collections.Generic;
using DuckSoup.Core.Randomization;
using DuckSoup.Features.Procedural;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;
using DuckSoup.Features.SinglePlayer.Models;

namespace DuckSoup.Features.SinglePlayer.Services
{
    // Modo 1P: 15 fichas, 3 recetas procedurales, rellenado individual.
    public sealed class SinglePlayerSession
    {
        public const int RecipeSlots = 3;
        public const int TokenSlots = 15;

        private readonly RecipeCardModel[] cards = new RecipeCardModel[RecipeSlots];
        private readonly IngredientToken[] tokens = new IngredientToken[TokenSlots];
        private readonly float[] hideTimers = new float[TokenSlots];
        private readonly float[] refillTimers = new float[TokenSlots];
        private readonly IngredientType[] pendingIngredients = new IngredientType[TokenSlots];
        private readonly float[] recipeTimers = new float[RecipeSlots];
        private readonly Dictionary<string, int> tokenOwners = new Dictionary<string, int>();
        private readonly Dictionary<int, string> pointerTokens = new Dictionary<int, string>();
        private readonly AvailableRecipeGenerator generator;
        private readonly float revealSeconds;
        private readonly float refillSeconds;
        private readonly float recipeSeconds;
        private int tokenSerial;
        private int cardSerial;

        public float RemainingSeconds { get; private set; }
        public int CompletedRecipes { get; private set; }
        public bool IsTimeUp { get; private set; }

        // Se conserva el parametro definitions para no romper el Bootstrap existente.
        // Las nuevas recetas ya no dependen de un catalogo fijo.
        public SinglePlayerSession(
            IEnumerable<RecipeDefinition> definitions,
            IRandomSource randomSource,
            float durationSeconds = 60f,
            float revealSeconds = 3f,
            float recipeReplacementSeconds = 0.8f,
            float tokenRefillSeconds = 1.2f)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (randomSource == null) throw new ArgumentNullException(nameof(randomSource));
            CheckPositive(durationSeconds, nameof(durationSeconds));
            CheckPositive(revealSeconds, nameof(revealSeconds));
            CheckPositive(recipeReplacementSeconds, nameof(recipeReplacementSeconds));
            CheckPositive(tokenRefillSeconds, nameof(tokenRefillSeconds));
            generator = new AvailableRecipeGenerator(randomSource);
            this.revealSeconds = revealSeconds;
            recipeSeconds = recipeReplacementSeconds;
            refillSeconds = tokenRefillSeconds;
            RemainingSeconds = durationSeconds;
            for (int i = 0; i < TokenSlots; i++)
            {
                hideTimers[i] = -1f;
                refillTimers[i] = -1f;
            }
            for (int i = 0; i < RecipeSlots; i++) recipeTimers[i] = -1f;
            var initialStock = generator.InitialStock(TokenSlots);
            for (int i = 0; i < TokenSlots; i++) tokens[i] = NewToken(initialStock[i]);
            for (int i = 0; i < RecipeSlots; i++) cards[i] = NewCard();
        }

        private static void CheckPositive(float value, string name)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name);
        }

        private IngredientToken NewToken(IngredientType type)
        {
            tokenSerial++;
            return new IngredientToken($"sp_token_{tokenSerial:000000}", type);
        }

        private RecipeCardModel NewCard()
        {
            cardSerial++;
            return new RecipeCardModel($"sp_card_{cardSerial:000000}",
                generator.Generate(GetStockIncludingPending(), AllCards()));
        }

        private IEnumerable<RecipeCardModel> AllCards()
        {
            for (int i = 0; i < RecipeSlots; i++) if (cards[i] != null) yield return cards[i];
        }

        private IEnumerable<IngredientType> GetStockIncludingPending()
        {
            for (int i = 0; i < TokenSlots; i++)
            {
                if (tokens[i] != null) yield return tokens[i].Ingredient;
                else if (refillTimers[i] >= 0f) yield return pendingIngredients[i];
            }
        }

        private int FindToken(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId)) return -1;
            for (int i = 0; i < TokenSlots; i++)
                if (tokens[i] != null && tokens[i].Id == tokenId) return i;
            return -1;
        }

        private int FindCard(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId)) return -1;
            for (int i = 0; i < RecipeSlots; i++)
                if (cards[i].CardId == cardId) return i;
            return -1;
        }

        private bool Owns(string tokenId, int pointerId)
        {
            return !string.IsNullOrWhiteSpace(tokenId) &&
                tokenOwners.TryGetValue(tokenId, out int owner) && owner == pointerId &&
                pointerTokens.TryGetValue(pointerId, out string token) && token == tokenId;
        }

        public bool TryBeginGesture(string tokenId, int pointerId)
        {
            if (IsTimeUp || tokenOwners.ContainsKey(tokenId ?? "") ||
                pointerTokens.ContainsKey(pointerId)) return false;
            int slot = FindToken(tokenId);
            if (slot < 0) return false;
            IngredientToken token = tokens[slot];
            if (token.IsConsumed || (!token.IsRevealed && !token.TryReveal())) return false;
            tokenOwners.Add(tokenId, pointerId);
            pointerTokens.Add(pointerId, tokenId);
            hideTimers[slot] = -1f;
            return true;
        }

        public bool TryDeliver(
            string tokenId, int pointerId, string cardId, out bool recipeCompleted)
        {
            recipeCompleted = false;
            if (!Owns(tokenId, pointerId)) return false;
            try
            {
                if (IsTimeUp) return false;
                int t = FindToken(tokenId);
                int c = FindCard(cardId);
                if (t < 0 || c < 0 || recipeTimers[c] >= 0f) return false;
                IngredientToken token = tokens[t];
                RecipeCardModel card = cards[c];
                if (!token.IsRevealed || token.IsConsumed ||
                    !card.Progress.NeedsIngredient(token.Ingredient)) return false;
                if (!token.TryConsume()) return false;
                if (!card.Progress.TryAddIngredient(token.Ingredient))
                    throw new InvalidOperationException("No se pudo actualizar la receta.");

                tokens[t] = null;
                hideTimers[t] = -1f;
                pendingIngredients[t] = generator.RandomIngredient();
                refillTimers[t] = refillSeconds;
                if (card.IsComplete)
                {
                    CompletedRecipes++;
                    recipeCompleted = true;
                    recipeTimers[c] = recipeSeconds;
                }
                return true;
            }
            finally { CancelGesture(tokenId, pointerId); }
        }

        public void CancelGesture(string tokenId, int pointerId)
        {
            if (!Owns(tokenId, pointerId)) return;
            tokenOwners.Remove(tokenId);
            pointerTokens.Remove(pointerId);
            int index = FindToken(tokenId);
            if (!IsTimeUp && index >= 0 && tokens[index].IsRevealed &&
                !tokens[index].IsConsumed) hideTimers[index] = revealSeconds;
        }

        public void CancelAllGestures()
        {
            foreach (string tokenId in tokenOwners.Keys)
            {
                int index = FindToken(tokenId);
                if (!IsTimeUp && index >= 0 && tokens[index].IsRevealed &&
                    !tokens[index].IsConsumed) hideTimers[index] = revealSeconds;
            }
            tokenOwners.Clear();
            pointerTokens.Clear();
        }

        public bool Advance(float deltaTime)
        {
            if (deltaTime < 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (IsTimeUp || deltaTime == 0f) return false;
            RemainingSeconds = Math.Max(0f, RemainingSeconds - deltaTime);
            if (RemainingSeconds <= 0f)
            {
                IsTimeUp = true;
                CancelAllGestures();
                return true;
            }
            bool changed = false;
            for (int t = 0; t < TokenSlots; t++)
            {
                if (tokens[t] != null && hideTimers[t] >= 0f &&
                    !tokenOwners.ContainsKey(tokens[t].Id))
                {
                    hideTimers[t] -= deltaTime;
                    if (hideTimers[t] <= 0f)
                    {
                        changed |= tokens[t].TryHide();
                        hideTimers[t] = -1f;
                    }
                }
                if (refillTimers[t] >= 0f)
                {
                    refillTimers[t] -= deltaTime;
                    if (refillTimers[t] <= 0f)
                    {
                        if (tokens[t] != null) throw new InvalidOperationException("Hueco ocupado.");
                        tokens[t] = NewToken(pendingIngredients[t]);
                        pendingIngredients[t] = IngredientType.None;
                        refillTimers[t] = -1f;
                        changed = true;
                    }
                }
            }
            for (int c = 0; c < RecipeSlots; c++)
            {
                if (recipeTimers[c] < 0f) continue;
                recipeTimers[c] -= deltaTime;
                if (recipeTimers[c] > 0f) continue;
                cards[c] = NewCard();
                recipeTimers[c] = -1f;
                changed = true;
            }
            return changed;
        }

        public SinglePlayerSnapshot GetSnapshot()
        {
            var recipes = new List<RecipeCardSnapshot>();
            foreach (RecipeCardModel card in cards)
            {
                var requirements = new List<IngredientRequirementSnapshot>();
                var visited = new HashSet<IngredientType>();
                foreach (IngredientType type in card.Definition.RequiredIngredients)
                {
                    if (!visited.Add(type)) continue;
                    requirements.Add(new IngredientRequirementSnapshot(
                        type, card.Progress.GetRequiredCount(type),
                        card.Progress.GetCompletedCount(type)));
                }
                recipes.Add(new RecipeCardSnapshot(card.CardId, card.Definition.Id,
                    card.Definition.DisplayName, requirements));
            }
            var snapshots = new List<IngredientTokenSnapshot>();
            foreach (IngredientToken token in tokens)
                snapshots.Add(token == null ? null : new IngredientTokenSnapshot(
                    token.Id, token.Ingredient, token.IsRevealed, token.IsConsumed));
            return new SinglePlayerSnapshot(
                recipes, snapshots, CompletedRecipes, RemainingSeconds);
        }
    }
}
