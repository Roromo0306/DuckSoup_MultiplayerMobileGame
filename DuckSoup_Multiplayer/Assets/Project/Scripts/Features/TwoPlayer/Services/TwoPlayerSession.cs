using System;
using System.Collections.Generic;
using DuckSoup.Core.Randomization;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Contracts;
using DuckSoup.Features.Round.Models;
using DuckSoup.Features.TwoPlayer.Models;

namespace DuckSoup.Features.TwoPlayer.Services
{
    /// <summary>
    /// Sesion 2P por tiempo. Una sola mesa, dos manos y reposicion individual.
    /// C# puro: no depende de MonoBehaviour ni de UI.
    /// </summary>
    public sealed class TwoPlayerSession
    {
        public const int CardsPerPlayer = 3;
        public const int TokenSlots = 18;

        private readonly List<RecipeDefinition> catalog = new List<RecipeDefinition>();
        private readonly IRandomSource random;
        private readonly RecipeCardModel[,] cards = new RecipeCardModel[2, CardsPerPlayer];
        private readonly RecipeDefinition[,] upcoming = new RecipeDefinition[2, CardsPerPlayer];
        private readonly float[,] recipeTimers = new float[2, CardsPerPlayer];
        private readonly IngredientToken[] tokens = new IngredientToken[TokenSlots];
        private readonly float[] hideTimers = new float[TokenSlots];
        private readonly float[] refillTimers = new float[TokenSlots];
        private readonly IngredientType[] pendingIngredients = new IngredientType[TokenSlots];
        private readonly Dictionary<string, int> tokenOwners = new Dictionary<string, int>();
        private readonly Dictionary<int, string> pointerTokens = new Dictionary<int, string>();
        private readonly int[] scores = new int[2];
        private readonly float revealSeconds;
        private readonly float refillSeconds;
        private readonly float recipeSeconds;
        private int cardSerial;
        private int tokenSerial;

        public float RemainingSeconds { get; private set; }
        public bool IsTimeUp { get; private set; }
        public int Player1Score => scores[0];
        public int Player2Score => scores[1];

        public TwoPlayerSession(
            IEnumerable<RecipeDefinition> definitions,
            IRandomSource randomSource,
            float durationSeconds = 60f,
            float revealDurationSeconds = 3f,
            float tokenRefillSeconds = 1.2f,
            float recipeReplacementSeconds = 0.8f)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            random = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
            CheckPositiveFinite(durationSeconds, nameof(durationSeconds));
            CheckPositiveFinite(revealDurationSeconds, nameof(revealDurationSeconds));
            CheckPositiveFinite(tokenRefillSeconds, nameof(tokenRefillSeconds));
            CheckPositiveFinite(recipeReplacementSeconds, nameof(recipeReplacementSeconds));

            RemainingSeconds = durationSeconds;
            revealSeconds = revealDurationSeconds;
            refillSeconds = tokenRefillSeconds;
            recipeSeconds = recipeReplacementSeconds;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (RecipeDefinition recipe in definitions)
            {
                if (recipe == null || !ids.Add(recipe.Id))
                    throw new ArgumentException("El catalogo tiene recetas nulas o duplicadas.");
                if (recipe.RequiredIngredients.Count != 3)
                    throw new ArgumentException("El prototipo 2P necesita recetas de tres ingredientes.");
                catalog.Add(recipe);
            }

            // Tres recetas visibles y tres futuras distintas por jugador.
            if (catalog.Count < CardsPerPlayer * 2)
                throw new ArgumentException("Se necesitan al menos seis recetas distintas.");

            for (int i = 0; i < TokenSlots; i++)
            {
                hideTimers[i] = -1f;
                refillTimers[i] = -1f;
                pendingIngredients[i] = IngredientType.None;
            }
            for (int p = 0; p < 2; p++)
                for (int c = 0; c < CardsPerPlayer; c++)
                    recipeTimers[p, c] = -1f;

            CreateInitialBoard();
        }

        private static void CheckPositiveFinite(float value, string name)
        {
            if (value <= 0f || float.IsInfinity(value) || float.IsNaN(value))
                throw new ArgumentOutOfRangeException(name);
        }

        private static int PlayerIndex(PlayerId id)
        {
            if (id == PlayerId.Player1) return 0;
            if (id == PlayerId.Player2) return 1;
            return -1;
        }

        private void CreateInitialBoard()
        {
            var needed = new List<IngredientType>(TokenSlots);
            for (int p = 0; p < 2; p++)
            {
                var choices = new List<RecipeDefinition>(catalog);
                for (int c = 0; c < CardsPerPlayer; c++)
                {
                    int j = random.NextInt(c, choices.Count);
                    RecipeDefinition temp = choices[c];
                    choices[c] = choices[j];
                    choices[j] = temp;

                    cards[p, c] = NewCard(p, choices[c]);
                    needed.AddRange(cards[p, c].Definition.RequiredIngredients);
                }
            }

            Shuffle(needed);
            for (int i = 0; i < TokenSlots; i++)
                tokens[i] = NewToken(needed[i]);

            for (int p = 0; p < 2; p++)
                for (int c = 0; c < CardsPerPlayer; c++)
                    upcoming[p, c] = ChooseUpcoming(p);
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private RecipeCardModel NewCard(int playerIndex, RecipeDefinition definition)
        {
            cardSerial++;
            return new RecipeCardModel($"p{playerIndex + 1}_card_{cardSerial:0000}", definition);
        }

        private IngredientToken NewToken(IngredientType ingredient)
        {
            tokenSerial++;
            return new IngredientToken($"p2_token_{tokenSerial:0000}", ingredient);
        }

        private RecipeDefinition ChooseUpcoming(int playerIndex)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            for (int c = 0; c < CardsPerPlayer; c++)
            {
                used.Add(cards[playerIndex, c].Definition.Id);
                if (upcoming[playerIndex, c] != null)
                    used.Add(upcoming[playerIndex, c].Id);
            }

            var choices = new List<RecipeDefinition>();
            foreach (RecipeDefinition definition in catalog)
                if (!used.Contains(definition.Id)) choices.Add(definition);

            if (choices.Count == 0)
                throw new InvalidOperationException("No quedan recetas para reponer.");

            return choices[random.NextInt(0, choices.Count)];
        }

        public bool TryBeginGesture(string tokenId, int pointerId)
        {
            if (IsTimeUp || string.IsNullOrWhiteSpace(tokenId) ||
                tokenOwners.ContainsKey(tokenId) || pointerTokens.ContainsKey(pointerId))
                return false;

            int tokenIndex = FindToken(tokenId);
            if (tokenIndex < 0) return false;
            IngredientToken token = tokens[tokenIndex];
            if (token.IsConsumed) return false;
            if (!token.IsRevealed && !token.TryReveal()) return false;

            tokenOwners.Add(tokenId, pointerId);
            pointerTokens.Add(pointerId, tokenId);
            hideTimers[tokenIndex] = -1f;
            return true;
        }

        public bool TryDeliver(
            string tokenId, int pointerId, PlayerId playerId,
            string cardId, out bool recipeCompleted)
        {
            recipeCompleted = false;
            if (!Owns(tokenId, pointerId)) return false;

            try
            {
                int p = PlayerIndex(playerId);
                if (IsTimeUp || p < 0) return false;
                int t = FindToken(tokenId);
                int c = FindCard(p, cardId);
                if (t < 0 || c < 0 || recipeTimers[p, c] >= 0f) return false;

                IngredientToken token = tokens[t];
                RecipeCardModel card = cards[p, c];
                if (token.IsConsumed || !token.IsRevealed ||
                    !card.Progress.NeedsIngredient(token.Ingredient)) return false;

                // La proxima receta ya esta determinada: cada entrega financia
                // uno de sus tres ingredientes futuros.
                int nextIngredientIndex = card.Progress.CompletedIngredientCount;
                RecipeDefinition nextRecipe = upcoming[p, c];
                if (nextRecipe == null || nextIngredientIndex >= 3)
                    throw new InvalidOperationException("Falta una receta futura valida.");
                IngredientType replacement = nextRecipe.RequiredIngredients[nextIngredientIndex];

                if (!token.TryConsume()) return false;
                if (!card.Progress.TryAddIngredient(token.Ingredient))
                    throw new InvalidOperationException("Progreso inconsistente tras consumir token.");

                tokens[t] = null;
                hideTimers[t] = -1f;
                pendingIngredients[t] = replacement;
                refillTimers[t] = refillSeconds;

                if (card.IsComplete)
                {
                    scores[p]++;
                    recipeCompleted = true;
                    recipeTimers[p, c] = recipeSeconds;
                }
                return true;
            }
            finally
            {
                CancelGesture(tokenId, pointerId);
            }
        }

        public void CancelGesture(string tokenId, int pointerId)
        {
            if (!Owns(tokenId, pointerId)) return;
            tokenOwners.Remove(tokenId);
            pointerTokens.Remove(pointerId);
            int index = FindToken(tokenId);
            if (!IsTimeUp && index >= 0 && !tokens[index].IsConsumed && tokens[index].IsRevealed)
                hideTimers[index] = revealSeconds;
        }

        public void CancelAllGestures()
        {
            foreach (string tokenId in tokenOwners.Keys)
            {
                int index = FindToken(tokenId);
                if (!IsTimeUp && index >= 0 && tokens[index].IsRevealed && !tokens[index].IsConsumed)
                    hideTimers[index] = revealSeconds;
            }
            tokenOwners.Clear();
            pointerTokens.Clear();
        }

        private bool Owns(string tokenId, int pointerId)
        {
            return !string.IsNullOrWhiteSpace(tokenId) &&
                tokenOwners.TryGetValue(tokenId, out int owner) && owner == pointerId &&
                pointerTokens.TryGetValue(pointerId, out string ownedToken) && ownedToken == tokenId;
        }

        private int FindToken(string tokenId)
        {
            if (string.IsNullOrWhiteSpace(tokenId)) return -1;
            for (int i = 0; i < TokenSlots; i++)
                if (tokens[i] != null && tokens[i].Id == tokenId) return i;
            return -1;
        }

        private int FindCard(int playerIndex, string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId)) return -1;
            for (int c = 0; c < CardsPerPlayer; c++)
                if (cards[playerIndex, c].CardId == cardId) return c;
            return -1;
        }

        /// <summary>Devuelve true cuando hay que refrescar las cartas o fichas.</summary>
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
                        if (tokens[t] != null || pendingIngredients[t] == IngredientType.None)
                            throw new InvalidOperationException("El hueco de reposicion no es valido.");
                        tokens[t] = NewToken(pendingIngredients[t]);
                        pendingIngredients[t] = IngredientType.None;
                        refillTimers[t] = -1f;
                        hideTimers[t] = -1f;
                        changed = true;
                    }
                }
            }

            for (int p = 0; p < 2; p++)
            {
                for (int c = 0; c < CardsPerPlayer; c++)
                {
                    if (recipeTimers[p, c] < 0f) continue;
                    recipeTimers[p, c] -= deltaTime;
                    if (recipeTimers[p, c] > 0f) continue;

                    RecipeDefinition next = upcoming[p, c];
                    if (next == null) throw new InvalidOperationException("No hay receta preparada.");
                    cards[p, c] = NewCard(p, next);
                    upcoming[p, c] = null;
                    upcoming[p, c] = ChooseUpcoming(p);
                    recipeTimers[p, c] = -1f;
                    changed = true;
                }
            }
            return changed;
        }

        public TwoPlayerSnapshot GetSnapshot()
        {
            var player1Cards = new List<RecipeCardSnapshot>();
            var player2Cards = new List<RecipeCardSnapshot>();
            for (int c = 0; c < CardsPerPlayer; c++)
            {
                player1Cards.Add(CardSnapshot(cards[0, c]));
                player2Cards.Add(CardSnapshot(cards[1, c]));
            }

            var tokenSnapshots = new List<IngredientTokenSnapshot>();
            foreach (IngredientToken token in tokens)
            {
                tokenSnapshots.Add(token == null ? null :
                    new IngredientTokenSnapshot(
                        token.Id, token.Ingredient, token.IsRevealed, token.IsConsumed));
            }

            return new TwoPlayerSnapshot(
                player1Cards, player2Cards, tokenSnapshots,
                Player1Score, Player2Score, RemainingSeconds, IsTimeUp);
        }

        private static RecipeCardSnapshot CardSnapshot(RecipeCardModel card)
        {
            var requirements = new List<IngredientRequirementSnapshot>();
            var visited = new HashSet<IngredientType>();
            foreach (IngredientType ingredient in card.Definition.RequiredIngredients)
            {
                if (!visited.Add(ingredient)) continue;
                requirements.Add(new IngredientRequirementSnapshot(
                    ingredient,
                    card.Progress.GetRequiredCount(ingredient),
                    card.Progress.GetCompletedCount(ingredient)));
            }
            return new RecipeCardSnapshot(
                card.CardId, card.Definition.Id, card.Definition.DisplayName, requirements);
        }
    }
}
