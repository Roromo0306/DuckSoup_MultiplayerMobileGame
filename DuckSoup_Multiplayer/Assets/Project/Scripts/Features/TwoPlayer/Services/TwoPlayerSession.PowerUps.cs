using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.PowerUps;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.TwoPlayer.Services
{
    public sealed partial class TwoPlayerSession
    {

        private readonly PowerTokenKind[] specialKinds = new PowerTokenKind[TokenSlots];
        private readonly float[] specialLeft = new float[TokenSlots];
        private float spawnCountdown;
        private IngredientType lastPoisonIngredient = IngredientType.None;

        public PowerTokenKind GetPowerKind(string tokenId)
        {
            int i = FindToken(tokenId);
            return i < 0 ? PowerTokenKind.None : specialKinds[i];
        }

        public bool TryDeliverSpecial(string tokenId, int pointerId, PlayerId actor,
            string cardId, out bool completed)
        {
            completed = false;
            if (!Owns(tokenId, pointerId) || IsTimeUp || IsPlayerLocked(actor))
                return false;
            int slot = FindToken(tokenId);
            if (slot < 0) return false;
            PowerTokenKind kind = specialKinds[slot];
            if (kind == PowerTokenKind.None || kind == PowerTokenKind.Poison) return false;

            bool accepted = TryUsePower(kind, actor, cardId, out completed);
            if (accepted)
                ConsumeSpecial(slot, tokenId, pointerId, IngredientType.None);
            else
                CancelGesture(tokenId, pointerId);
            return accepted;
        }

        public bool TryTouchPoison(string tokenId, int pointerId, PlayerId actor,
            out bool removedTick)
        {
            removedTick = false;
            if (!Owns(tokenId, pointerId) || IsTimeUp || IsPlayerLocked(actor))
                return false;
            int slot = FindToken(tokenId);
            if (slot < 0 || specialKinds[slot] != PowerTokenKind.Poison) return false;
            removedTick = ApplyPoison(actor);
            ConsumeSpecial(slot, tokenId, pointerId,
                removedTick ? lastPoisonIngredient : IngredientType.None);
            return true;
        }

        private void ConsumeSpecial(int slot, string tokenId, int pointerId,
            IngredientType forcedRefill)
        {
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
        private readonly float[] comboRemaining = new float[2];
        private readonly float[] lockRemaining = new float[2];

        public bool PowerUpsEnabled => powerUpRules != null;
        public float Player1ComboRemaining => comboRemaining[0];
        public float Player2ComboRemaining => comboRemaining[1];
        public float Player1LockRemaining => lockRemaining[0];
        public float Player2LockRemaining => lockRemaining[1];

        public void EnablePowerUps(PowerUpRules settings)
        {
            powerUpRules = settings ?? throw new ArgumentNullException(nameof(settings));
            spawnCountdown = powerUpRules.SpawnInterval;
        }

        public bool IsPlayerLocked(PlayerId id)
        {
            int p = PlayerIndex(id);
            return p >= 0 && lockRemaining[p] > 0f;
        }

        private void RegisterCompletedCard(int p, int c)
        {
            bool doublePoints = powerUpRules != null && comboRemaining[p] > 0f;
            scores[p] += doublePoints ? 2 : 1;
            if (powerUpRules == null)
            {
                recipeTimers[p, c] = recipeSeconds;
                return;
            }
            if (!doublePoints) comboRemaining[p] = powerUpRules.ComboWindowSeconds;
            recipeTimers[p, c] = -1f;
        }

        private bool AdvancePowerUps(float dt)
        {
            if (powerUpRules == null) return false;
            bool changed = false;
            bool[] refresh = new bool[2];
            for (int p = 0; p < 2; p++)
            {
                lockRemaining[p] = Math.Max(0f, lockRemaining[p] - dt);
                if (comboRemaining[p] > 0f)
                {
                    comboRemaining[p] = Math.Max(0f, comboRemaining[p] - dt);
                    refresh[p] = comboRemaining[p] <= 0f;
                }
            }
            if (refresh[0] || refresh[1])
            {
                RestoreSpecialTokensBeforeNewHand();
                // Remove both expired hands BEFORE generating their replacements.
                for (int p = 0; p < 2; p++)
                    if (refresh[p])
                        for (int c = 0; c < CardsPerPlayer; c++)
                        {
                            cards[p, c] = null;
                            recipeTimers[p, c] = -1f;
                        }
                for (int p = 0; p < 2; p++)
                    if (refresh[p])
                        for (int c = 0; c < CardsPerPlayer; c++)
                            cards[p, c] = NewCard(p);
                changed = true;
            }
            return TickSpecialTokens(dt) || changed;
        }

        public bool TryUsePower(PowerTokenKind kind, PlayerId player,
            string cardId, out bool completed)
        {
            completed = false;
            int p = PlayerIndex(player);
            if (powerUpRules == null || IsTimeUp || p < 0 || IsPlayerLocked(player))
                return false;
            int c = FindCard(p, cardId);
            if (c < 0) return false;
            if (kind == PowerTokenKind.ExtraComboTime)
            {
                if (comboRemaining[p] <= 0f) return false;
                comboRemaining[p] += powerUpRules.ExtraComboSeconds;
                return true;
            }
            RecipeCardModel card = cards[p, c];
            if (card == null || card.IsComplete) return false;
            if (kind == PowerTokenKind.WildcardDuck)
            {
                foreach (IngredientType type in card.Definition.RequiredIngredients)
                {
                    if (!card.Progress.NeedsIngredient(type)) continue;
                    if (!card.Progress.TryAddIngredient(type)) return false;
                    break;
                }
            }
            else if (kind == PowerTokenKind.CompleteRecipe)
            {
                foreach (IngredientType type in card.Definition.RequiredIngredients)
                {
                    if (card.Progress.NeedsIngredient(type))
                        if (!card.Progress.TryAddIngredient(type)) return false;
                }
            }
            else return false;

            if (card.IsComplete)
            {
                RegisterCompletedCard(p, c);
                completed = true;
            }
            return true;
        }

        public bool ApplyPoison(PlayerId player)
        {
            int p = PlayerIndex(player);
            if (powerUpRules == null || IsTimeUp || p < 0 || IsPlayerLocked(player))
                return false;
            int bestSlot = -1, best = 0;
            for (int c = 0; c < CardsPerPlayer; c++)
            {
                if (cards[p, c] == null || cards[p, c].IsComplete) continue;
                int count = CountProgress(cards[p, c]);
                if (count > best) { bestSlot = c; best = count; }
            }
            if (bestSlot < 0) return false;
            IngredientType lostType;
            cards[p, bestSlot] = RebuildWithoutOneTick(cards[p, bestSlot], out lostType);
            lastPoisonIngredient = lostType;
            EnsureIngredientAvailable(lostType);
            return true;
        }

        public void ApplyWrongDeliveryPenalty(PlayerId player)
        {
            int p = PlayerIndex(player);
            if (powerUpRules == null || IsTimeUp || p < 0) return;
            lockRemaining[p] = Math.Max(lockRemaining[p], powerUpRules.MultiplayerLockSeconds);
        }

        private static int CountProgress(RecipeCardModel card)
        {
            int total = 0;
            var visited = new HashSet<IngredientType>();
            foreach (IngredientType type in card.Definition.RequiredIngredients)
                if (visited.Add(type)) total += card.Progress.GetCompletedCount(type);
            return total;
        }

        private static RecipeCardModel RebuildWithoutOneTick(
            RecipeCardModel card, out IngredientType removed)
        {
            var types = new List<IngredientType>();
            var visited = new HashSet<IngredientType>();
            foreach (IngredientType type in card.Definition.RequiredIngredients)
            {
                if (!visited.Add(type)) continue;
                for (int n = 0; n < card.Progress.GetCompletedCount(type); n++) types.Add(type);
            }
            removed = types[types.Count - 1];
            types.RemoveAt(types.Count - 1);
            var result = new RecipeCardModel(card.CardId, card.Definition);
            foreach (IngredientType type in types) result.Progress.TryAddIngredient(type);
            return result;
        }

        // A poison may restore a required tick. Keep that ingredient reachable.
        private void EnsureIngredientAvailable(IngredientType needed)
        {
            var demand = new Dictionary<IngredientType, int>();
            var stock = new Dictionary<IngredientType, int>();
            foreach (RecipeCardModel card in AllCards())
            {
                if (card.IsComplete) continue;
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
