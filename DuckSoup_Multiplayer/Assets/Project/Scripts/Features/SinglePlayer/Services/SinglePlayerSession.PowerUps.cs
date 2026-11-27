using System;
using System.Collections.Generic;
using DuckSoup.Features.PowerUps;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.SinglePlayer.Services
{
    public sealed partial class SinglePlayerSession
    {
        private readonly PowerTokenKind[] specialKinds =
            new PowerTokenKind[TokenSlots];

        private readonly float[] specialLeft =
            new float[TokenSlots];

        private float spawnCountdown;

        private IngredientType lastPoisonIngredient =
            IngredientType.None;

        public PowerTokenKind GetPowerKind(string tokenId)
        {
            int i = FindToken(tokenId);

            return i < 0
                ? PowerTokenKind.None
                : specialKinds[i];
        }

        public bool TryDeliverSpecial(
            string tokenId,
            int pointerId,
            string cardId,
            out bool completed)
        {
            completed = false;

            if (!Owns(tokenId, pointerId))
                return false;

            try
            {
                if (IsTimeUp)
                    return false;

                int slot = FindToken(tokenId);

                if (slot < 0 ||
                    specialKinds[slot] == PowerTokenKind.None)
                {
                    return false;
                }

                PowerTokenKind kind = specialKinds[slot];

                bool accepted = kind == PowerTokenKind.Poison
                    ? ApplyPoop(cardId)
                    : TryUsePower(kind, cardId, out completed);

                if (!accepted)
                    return false;

                ConsumeSpecial(
                    slot,
                    tokenId,
                    pointerId,
                    kind == PowerTokenKind.Poison
                        ? lastPoisonIngredient
                        : IngredientType.None);

                return true;
            }
            finally
            {
                CancelGesture(tokenId, pointerId);
            }
        }

        public bool TryTouchPoison(
            string tokenId,
            int pointerId,
            out bool removedTick)
        {
            // La caca se aplica al arrastrarla a una carta.
            removedTick = false;
            return false;
        }

        private void ConsumeSpecial(
            int slot,
            string tokenId,
            int pointerId,
            IngredientType forcedRefill)
        {
            CancelGesture(tokenId, pointerId);

            if (tokens[slot] != null)
                tokens[slot].TryConsume();

            tokens[slot] = null;

            specialKinds[slot] = PowerTokenKind.None;
            specialLeft[slot] = 0f;
            hideTimers[slot] = -1f;

            pendingIngredients[slot] =
                forcedRefill != IngredientType.None
                    ? forcedRefill
                    : generator.RandomIngredient();

            refillTimers[slot] = refillSeconds;
        }

        private bool TickSpecialTokensLegacy(float dt)
        {
            bool changed = false;

            for (int i = 0; i < TokenSlots; i++)
            {
                if (specialKinds[i] == PowerTokenKind.None ||
                    tokens[i] == null)
                {
                    continue;
                }

                if (tokenOwners.ContainsKey(tokens[i].Id))
                    continue;

                specialLeft[i] -= dt;

                if (specialLeft[i] <= 0f)
                {
                    specialKinds[i] = PowerTokenKind.None;

                    tokens[i].TryHide();

                    changed = true;
                }
            }

            spawnCountdown -= dt;

            if (spawnCountdown > 0f)
                return changed;

            spawnCountdown = powerUpRules.SpawnInterval;

            int count = 0;

            for (int i = 0; i < TokenSlots; i++)
            {
                if (specialKinds[i] != PowerTokenKind.None)
                    count++;
            }

            if (count >= powerUpRules.MaxActiveSpecials)
                return changed;

            var slots = PowerTokenAvailability.GetEligibleSlots(
                AllCards(),
                tokens,
                pendingIngredients,
                refillTimers,
                specialKinds,
                tokenOwners);

            if (slots.Count == 0)
                return changed;

            PowerTokenKind drawn =
                powerUpRules.Draw(random);

            if (drawn == PowerTokenKind.None)
                return changed;

            int chosen = slots[
                random.NextInt(0, slots.Count)];

            specialKinds[chosen] = drawn;
            specialLeft[chosen] = powerUpRules.TokenLifetime;

            return true;
        }

        private void RestoreSpecialTokensBeforeNewHand()
        {
            for (int i = 0; i < TokenSlots; i++)
            {
                if (specialKinds[i] == PowerTokenKind.None ||
                    (tokens[i] != null &&
                     tokenOwners.ContainsKey(tokens[i].Id)))
                {
                    continue;
                }

                specialKinds[i] = PowerTokenKind.None;
                specialLeft[i] = 0f;

                if (tokens[i] != null &&
                    !tokenOwners.ContainsKey(tokens[i].Id))
                {
                    tokens[i].TryHide();
                }
            }
        }

        // COMBOS

        private PowerUpRules powerUpRules;

        public event Action<int, int> ComboScored;

        private float comboRemaining;
        private float comboTotalSeconds;

        private int comboCount;
        private string lastCompletedCardId;

        public bool PowerUpsEnabled => powerUpRules != null;

        public float ComboRemaining => comboRemaining;

        public int CurrentComboCount => comboCount;

        public string LastCompletedCardId =>
            lastCompletedCardId;

        public int ActualRecipesCompleted
        {
            get;
            private set;
        }

        public float ComboFill01 =>
            comboTotalSeconds > 0f
                ? (float)Math.Max(
                    0.0,
                    Math.Min(
                        1.0,
                        comboRemaining / comboTotalSeconds))
                : 0f;

        public bool IsCompletedCard(string cardId)
        {
            int index = FindCard(cardId);

            return index >= 0 &&
                   cards[index] != null &&
                   cards[index].IsComplete;
        }

        public void EnablePowerUps(PowerUpRules settings)
        {
            powerUpRules = settings ??
                throw new ArgumentNullException(nameof(settings));

            spawnCountdown = powerUpRules.SpawnInterval;
        }

        private void RegisterCompletedCard(int cardIndex)
        {
            ActualRecipesCompleted++;

            if (powerUpRules == null)
            {
                CompletedRecipes++;
                recipeTimers[cardIndex] = recipeSeconds;

                return;
            }

            if (comboRemaining <= 0f)
            {
                comboRemaining =
                    powerUpRules.ComboWindowSeconds;

                comboTotalSeconds = comboRemaining;
                comboCount = 0;
            }

            // Primera carta +1, segunda +2, tercera +3.
            comboCount = Math.Min(
                RecipeSlots,
                comboCount + 1);

            CompletedRecipes += comboCount;

            if (comboCount >= 2)
                ComboScored?.Invoke(cardIndex, comboCount);

            lastCompletedCardId =
                cards[cardIndex].CardId;

            recipeTimers[cardIndex] = -1f;
        }

        private bool AdvancePowerUps(float dt)
        {
            if (powerUpRules == null)
                return false;

            bool changed = false;

            if (comboRemaining > 0f)
            {
                comboRemaining = Math.Max(
                    0f,
                    comboRemaining - dt);

                if (comboRemaining <= 0f ||
                    comboCount >= RecipeSlots)
                {
                    RestoreSpecialTokensBeforeNewHand();

                    var replacedSlots = new List<int>();

                    for (int i = 0; i < RecipeSlots; i++)
                    {
                        if (cards[i] == null ||
                            !cards[i].IsComplete)
                        {
                            continue;
                        }

                        replacedSlots.Add(i);

                        cards[i] = null;
                        recipeTimers[i] = -1f;
                    }

                    foreach (int slot in replacedSlots)
                    {
                        cards[slot] = NewCard();
                    }

                    comboCount = 0;
                    comboRemaining = 0f;
                    comboTotalSeconds = 0f;
                    lastCompletedCardId = null;

                    changed = replacedSlots.Count > 0;
                }
            }

            // Este metodo esta en SinglePlayerSession.AdaptiveSpawning.cs.
            return TickSpecialTokens(dt) || changed;
        }

        public bool TryUsePower(
            PowerTokenKind kind,
            string cardId,
            out bool completed)
        {
            completed = false;

            if (powerUpRules == null || IsTimeUp)
                return false;

            int index = FindCard(cardId);

            if (index < 0)
                return false;

            if (kind == PowerTokenKind.ExtraComboTime)
            {
                if (comboRemaining <= 0f)
                    return false;

                comboRemaining +=
                    powerUpRules.ExtraComboSeconds;

                comboTotalSeconds +=
                    powerUpRules.ExtraComboSeconds;

                return true;
            }

            RecipeCardModel card = cards[index];

            if (card == null || card.IsComplete)
                return false;

            if (kind == PowerTokenKind.WildcardDuck)
            {
                foreach (IngredientType ingredient
                    in card.Definition.RequiredIngredients)
                {
                    if (!card.Progress.NeedsIngredient(ingredient))
                        continue;

                    if (!card.Progress.TryAddIngredient(ingredient))
                        return false;

                    break;
                }
            }
            else if (kind == PowerTokenKind.CompleteRecipe)
            {
                foreach (IngredientType ingredient
                    in card.Definition.RequiredIngredients)
                {
                    if (!card.Progress.NeedsIngredient(ingredient))
                        continue;

                    if (!card.Progress.TryAddIngredient(ingredient))
                        return false;
                }
            }
            else
            {
                return false;
            }

            if (card.IsComplete)
            {
                RegisterCompletedCard(index);
                completed = true;
            }

            return true;
        }

        // CACA Y PENALIZACIONES

        public bool ApplyPoison()
        {
            // La caca necesita una carta destino.
            return false;
        }

        public bool ApplyPoop(string cardId)
        {
            if (powerUpRules == null || IsTimeUp)
                return false;

            int index = FindCard(cardId);

            if (index < 0 ||
                cards[index] == null ||
                cards[index].IsComplete ||
                CountProgress(cards[index]) == 0)
            {
                return false;
            }

            cards[index] = RebuildWithoutOneTick(
                cards[index],
                out lastPoisonIngredient);

            EnsureIngredientAvailable(lastPoisonIngredient);

            return true;
        }

        public bool IsWrongIngredientForCard(
            string tokenId,
            string cardId)
        {
            int token = FindToken(tokenId);
            int card = FindCard(cardId);

            if (powerUpRules == null ||
                IsTimeUp ||
                token < 0 ||
                card < 0 ||
                specialKinds[token] != PowerTokenKind.None ||
                cards[card].IsComplete)
            {
                return false;
            }

            return !cards[card].Progress.NeedsIngredient(
                tokens[token].Ingredient);
        }

        public void ApplyWrongDeliveryPenalty()
        {
            if (powerUpRules == null || IsTimeUp)
                return;

            RemainingSeconds = Math.Max(
                0f,
                RemainingSeconds -
                powerUpRules.WrongSinglePlayerPenaltySeconds);

            if (RemainingSeconds <= 0f)
            {
                IsTimeUp = true;
                CancelAllGestures();
            }
        }

        private static int CountProgress(
            RecipeCardModel card)
        {
            int count = 0;

            var visited = new HashSet<IngredientType>();

            foreach (IngredientType type
                in card.Definition.RequiredIngredients)
            {
                if (visited.Add(type))
                {
                    count +=
                        card.Progress.GetCompletedCount(type);
                }
            }

            return count;
        }

        private static RecipeCardModel RebuildWithoutOneTick(
            RecipeCardModel oldCard,
            out IngredientType removedIngredient)
        {
            removedIngredient = IngredientType.None;

            var ordered = new List<IngredientType>();
            var visited = new HashSet<IngredientType>();

            foreach (IngredientType type
                in oldCard.Definition.RequiredIngredients)
            {
                if (!visited.Add(type))
                    continue;

                for (int n = 0;
                    n < oldCard.Progress.GetCompletedCount(type);
                    n++)
                {
                    ordered.Add(type);
                }
            }

            removedIngredient =
                ordered[ordered.Count - 1];

            ordered.RemoveAt(ordered.Count - 1);

            var rebuilt = new RecipeCardModel(
                oldCard.CardId,
                oldCard.Definition);

            foreach (IngredientType type in ordered)
            {
                rebuilt.Progress.TryAddIngredient(type);
            }

            return rebuilt;
        }

        private void EnsureIngredientAvailable(
            IngredientType needed)
        {
            var demand = new Dictionary<IngredientType, int>();
            var stock = new Dictionary<IngredientType, int>();

            foreach (RecipeCardModel card in cards)
            {
                if (card == null || card.IsComplete)
                    continue;

                var visited = new HashSet<IngredientType>();

                foreach (IngredientType type
                    in card.Definition.RequiredIngredients)
                {
                    if (!visited.Add(type))
                        continue;

                    int rest =
                        card.Progress.GetRequiredCount(type) -
                        card.Progress.GetCompletedCount(type);

                    demand[type] =
                        GetCount(demand, type) + rest;
                }
            }

            foreach (IngredientType type
                in GetStockIncludingPending())
            {
                stock[type] =
                    GetCount(stock, type) + 1;
            }

            if (GetCount(stock, needed) >=
                GetCount(demand, needed))
            {
                return;
            }

            for (int i = 0; i < TokenSlots; i++)
            {
                if (specialKinds[i] != PowerTokenKind.None)
                    continue;

                IngredientType type =
                    tokens[i] != null
                        ? tokens[i].Ingredient
                        : pendingIngredients[i];

                if (GetCount(stock, type) <=
                    GetCount(demand, type))
                {
                    continue;
                }

                if (tokens[i] != null &&
                    tokenOwners.ContainsKey(tokens[i].Id))
                {
                    continue;
                }

                if (tokens[i] != null)
                {
                    tokens[i] = NewToken(needed);
                    hideTimers[i] = -1f;
                }
                else if (refillTimers[i] >= 0f)
                {
                    pendingIngredients[i] = needed;
                }
                else
                {
                    continue;
                }

                return;
            }
        }

        private static int GetCount(
            Dictionary<IngredientType, int> map,
            IngredientType type)
        {
            return map.TryGetValue(type, out int n)
                ? n
                : 0;
        }
    }
}