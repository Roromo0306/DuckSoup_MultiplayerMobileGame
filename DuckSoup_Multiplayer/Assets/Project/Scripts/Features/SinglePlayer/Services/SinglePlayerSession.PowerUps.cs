using System;
using System.Collections.Generic;
using DuckSoup.Features.PowerUps;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.SinglePlayer.Services
{
    /// <summary>
    /// Extension of the existing SinglePlayerSession without duplicating its core logic.
    /// Partial classes share the private fields of SinglePlayerSession.cs.
    /// </summary>
    public sealed partial class SinglePlayerSession
    {

        // Exactly the same token slot now may represent an ingredient or a special.
        private readonly PowerTokenKind[] specialKinds = new PowerTokenKind[TokenSlots];
        private readonly float[] specialLeft = new float[TokenSlots];
        private float spawnCountdown;
        private IngredientType lastPoisonIngredient = IngredientType.None;

        public PowerTokenKind GetPowerKind(string tokenId)
        {
            int i = FindToken(tokenId);
            return i < 0 ? PowerTokenKind.None : specialKinds[i];
        }

        // Used by the SAME SinglePlayerTokenView and pointer reservation.
        public bool TryDeliverSpecial(string tokenId, int pointerId, string cardId,
            out bool completed)
        {
            completed = false;
            if (!Owns(tokenId, pointerId) || IsTimeUp) return false;
            int slot = FindToken(tokenId);
            if (slot < 0) return false;
            PowerTokenKind kind = specialKinds[slot];
            if (kind == PowerTokenKind.None || kind == PowerTokenKind.Poison) return false;

            bool accepted = TryUsePower(kind, cardId, out completed);
            if (accepted)
                ConsumeSpecial(slot, tokenId, pointerId, IngredientType.None);
            else
                CancelGesture(tokenId, pointerId);
            return accepted;
        }

        // Poison triggers ON TOUCH; there is no separate poison prefab.
        public bool TryTouchPoison(string tokenId, int pointerId, out bool removedTick)
        {
            removedTick = false;
            if (!Owns(tokenId, pointerId) || IsTimeUp) return false;
            int slot = FindToken(tokenId);
            if (slot < 0 || specialKinds[slot] != PowerTokenKind.Poison) return false;
            removedTick = ApplyPoison();
            ConsumeSpecial(slot, tokenId, pointerId,
                removedTick ? lastPoisonIngredient : IngredientType.None);
            return true;
        }

        private void ConsumeSpecial(int slot, string tokenId, int pointerId,
            IngredientType forcedRefill)
        {
            // Must release reservation BEFORE removing its model.
            CancelGesture(tokenId, pointerId);
            if (tokens[slot] != null) tokens[slot].TryConsume();
            tokens[slot] = null;
            specialKinds[slot] = PowerTokenKind.None;
            specialLeft[slot] = 0f;
            hideTimers[slot] = -1f;
            pendingIngredients[slot] = forcedRefill != IngredientType.None
                ? forcedRefill : generator.RandomIngredient();
            refillTimers[slot] = refillSeconds;
        }

        private bool TickSpecialTokens(float dt)
        {
            bool changed = false;
            for (int i = 0; i < TokenSlots; i++)
            {
                if (specialKinds[i] == PowerTokenKind.None || tokens[i] == null)
                    continue;
                if (tokenOwners.ContainsKey(tokens[i].Id)) continue;
                specialLeft[i] -= dt;
                if (specialLeft[i] <= 0f)
                {
                    specialKinds[i] = PowerTokenKind.None;
                    tokens[i].TryHide();
                    changed = true;
                }
            }
            spawnCountdown -= dt;
            if (spawnCountdown > 0f) return changed;
            spawnCountdown = powerUpRules.SpawnInterval;

            int count = 0;
            for (int i = 0; i < TokenSlots; i++)
                if (specialKinds[i] != PowerTokenKind.None) count++;
            if (count >= powerUpRules.MaxActiveSpecials) return changed;

            var slots = PowerTokenAvailability.GetEligibleSlots(
                AllCards(), tokens, pendingIngredients, refillTimers,
                specialKinds, tokenOwners);
            if (slots.Count == 0) return changed;
            PowerTokenKind drawn = powerUpRules.Draw(random);
            if (drawn == PowerTokenKind.None) return changed;
            int chosen = slots[random.NextInt(0, slots.Count)];
            specialKinds[chosen] = drawn;
            specialLeft[chosen] = powerUpRules.TokenLifetime;
            return true;
        }

        private void RestoreSpecialTokensBeforeNewHand()
        {
            // Refill old recipe requirements from normal ingredient stock.
            // Especially important in 2P: six cards can reserve all 18 slots.
            for (int i = 0; i < TokenSlots; i++)
            {
                if (specialKinds[i] != PowerTokenKind.None)
                {
                    specialKinds[i] = PowerTokenKind.None;
                    specialLeft[i] = 0f;
                    if (tokens[i] != null && !tokenOwners.ContainsKey(tokens[i].Id))
                        tokens[i].TryHide();
                }
            }
        }

        private PowerUpRules powerUpRules;
        private float comboRemaining;

        public bool PowerUpsEnabled => powerUpRules != null;
        public float ComboRemaining => comboRemaining;
        public int ActualRecipesCompleted { get; private set; }
        // CompletedRecipes continues as the HUD score (1 point / 2 combo points).

        public void EnablePowerUps(PowerUpRules settings)
        {
            powerUpRules = settings ?? throw new ArgumentNullException(nameof(settings));
            spawnCountdown = powerUpRules.SpawnInterval;
        }

        // Called when an ordinary ingredient or a bonus completes a recipe.
        private void RegisterCompletedCard(int cardIndex)
        {
            ActualRecipesCompleted++;
            bool doublePoints = powerUpRules != null && comboRemaining > 0f;
            CompletedRecipes += doublePoints ? 2 : 1;

            if (powerUpRules == null)
            {
                // Legacy behaviour when this feature has not been enabled.
                recipeTimers[cardIndex] = recipeSeconds;
                return;
            }

            // The first card starts a fixed combo window; it counts 1 point.
            if (!doublePoints)
                comboRemaining = powerUpRules.ComboWindowSeconds;

            // Keep completed cards face-down until the hand refreshes together.
            recipeTimers[cardIndex] = -1f;
        }

        // Called from Advance(), after regular token timers have advanced.
        private bool AdvancePowerUps(float dt)
        {
            if (powerUpRules == null) return false;
            bool changed = false;
            if (comboRemaining > 0f)
            {
                comboRemaining = Math.Max(0f, comboRemaining - dt);
                if (comboRemaining <= 0f)
                {
                    RestoreSpecialTokensBeforeNewHand();
                    for (int i = 0; i < RecipeSlots; i++)
                    {
                        cards[i] = null;
                        recipeTimers[i] = -1f;
                    }
                    for (int i = 0; i < RecipeSlots; i++)
                        cards[i] = NewCard();
                    changed = true;
                }
            }
            return TickSpecialTokens(dt) || changed;
        }

        public bool TryUsePower(PowerTokenKind kind, string cardId, out bool completed)
        {
            completed = false;
            if (powerUpRules == null || IsTimeUp)
                return false;

            int index = FindCard(cardId);
            if (index < 0)
                return false;

            if (kind == PowerTokenKind.ExtraComboTime)
            {
                if (comboRemaining <= 0f) return false;
                comboRemaining += powerUpRules.ExtraComboSeconds;
                return true;
            }

            RecipeCardModel card = cards[index];
            if (card == null || card.IsComplete)
                return false;

            if (kind == PowerTokenKind.WildcardDuck)
            {
                foreach (IngredientType ingredient in card.Definition.RequiredIngredients)
                {
                    if (!card.Progress.NeedsIngredient(ingredient)) continue;
                    if (!card.Progress.TryAddIngredient(ingredient)) return false;
                    break;
                }
            }
            else if (kind == PowerTokenKind.CompleteRecipe)
            {
                foreach (IngredientType ingredient in card.Definition.RequiredIngredients)
                {
                    if (card.Progress.NeedsIngredient(ingredient))
                        if (!card.Progress.TryAddIngredient(ingredient)) return false;
                }
            }
            else return false;

            if (card.IsComplete)
            {
                RegisterCompletedCard(index);
                completed = true;
            }
            return true;
        }

        /// <summary>Poison removes one already completed tick from an incomplete recipe.</summary>
        public bool ApplyPoison()
        {
            if (powerUpRules == null || IsTimeUp) return false;
            int bestSlot = -1, bestProgress = 0;
            for (int i = 0; i < RecipeSlots; i++)
            {
                if (cards[i] == null || cards[i].IsComplete) continue;
                int completed = CountProgress(cards[i]);
                if (completed > bestProgress) { bestProgress = completed; bestSlot = i; }
            }
            if (bestSlot < 0) return false;
            IngredientType lostType;
            cards[bestSlot] = RebuildWithoutOneTick(cards[bestSlot], out lostType);
            lastPoisonIngredient = lostType;
            EnsureIngredientAvailable(lostType);
            return true;
        }

        public void ApplyWrongDeliveryPenalty()
        {
            if (powerUpRules == null || IsTimeUp) return;
            RemainingSeconds = Math.Max(0f,
                RemainingSeconds - powerUpRules.WrongSinglePlayerPenaltySeconds);
            if (RemainingSeconds <= 0f)
            {
                IsTimeUp = true;
                CancelAllGestures();
            }
        }

        private static int CountProgress(RecipeCardModel card)
        {
            int count = 0;
            var visited = new HashSet<IngredientType>();
            foreach (IngredientType type in card.Definition.RequiredIngredients)
                if (visited.Add(type)) count += card.Progress.GetCompletedCount(type);
            return count;
        }

        private static RecipeCardModel RebuildWithoutOneTick(
            RecipeCardModel oldCard, out IngredientType removedIngredient)
        {
            removedIngredient = IngredientType.None;
            var ordered = new List<IngredientType>();
            var visited = new HashSet<IngredientType>();
            foreach (IngredientType type in oldCard.Definition.RequiredIngredients)
            {
                if (!visited.Add(type)) continue;
                for (int n = 0; n < oldCard.Progress.GetCompletedCount(type); n++)
                    ordered.Add(type);
            }
            removedIngredient = ordered[ordered.Count - 1];
            ordered.RemoveAt(ordered.Count - 1);
            var rebuilt = new RecipeCardModel(oldCard.CardId, oldCard.Definition);
            foreach (IngredientType type in ordered)
                rebuilt.Progress.TryAddIngredient(type);
            return rebuilt;
        }

        // Repair missing stock caused by poison by replacing an UNRESERVED surplus token.
        private void EnsureIngredientAvailable(IngredientType needed)
        {
            var demand = new Dictionary<IngredientType, int>();
            var stock = new Dictionary<IngredientType, int>();
            foreach (RecipeCardModel card in cards)
            {
                if (card == null || card.IsComplete) continue;
                var visited = new HashSet<IngredientType>();
                foreach (IngredientType type in card.Definition.RequiredIngredients)
                {
                    if (!visited.Add(type)) continue;
                    int rest = card.Progress.GetRequiredCount(type) -
                        card.Progress.GetCompletedCount(type);
                    demand[type] = GetCount(demand, type) + rest;
                }
            }
            foreach (IngredientType type in GetStockIncludingPending())
                stock[type] = GetCount(stock, type) + 1;
            if (GetCount(stock, needed) >= GetCount(demand, needed)) return;
            for (int i = 0; i < TokenSlots; i++)
            {
                if (specialKinds[i] != PowerTokenKind.None) continue;
                IngredientType type = tokens[i] != null
                    ? tokens[i].Ingredient : pendingIngredients[i];
                if (GetCount(stock, type) <= GetCount(demand, type)) continue;
                if (tokens[i] != null && tokenOwners.ContainsKey(tokens[i].Id)) continue;
                if (tokens[i] != null) { tokens[i] = NewToken(needed); hideTimers[i] = -1f; }
                else if (refillTimers[i] >= 0f) pendingIngredients[i] = needed;
                else continue;
                return;
            }
        }

        private static int GetCount(Dictionary<IngredientType, int> map, IngredientType type)
        {
            return map.TryGetValue(type, out int n) ? n : 0;
        }
    }
}
