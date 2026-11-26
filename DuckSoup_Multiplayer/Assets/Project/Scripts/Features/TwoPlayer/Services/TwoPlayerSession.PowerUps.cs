using System;
using System.Collections.Generic;
using DuckSoup.Domain;
using DuckSoup.Features.PowerUps;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.TwoPlayer.Services
{
    public sealed partial class TwoPlayerSession
    {
        private readonly PowerTokenKind[] specialKinds =
            new PowerTokenKind[TokenSlots];

        private readonly float[] specialLeft =
            new float[TokenSlots];

        private float spawnCountdown;

        private IngredientType lastPoisonIngredient =
            IngredientType.None;

        private PowerUpRules powerUpRules;

        private readonly float[] comboRemaining = new float[2];
        private readonly float[] comboTotalSeconds = new float[2];
        private readonly int[] comboCounts = new int[2];
        private readonly string[] lastCompletedCardIds = new string[2];

        private readonly float[,] cardLockRemaining =
            new float[2, CardsPerPlayer];

        public event Action<PlayerId, int, int> ComboScored;

        public bool PowerUpsEnabled => powerUpRules != null;
        public float Player1ComboRemaining => comboRemaining[0];
        public float Player2ComboRemaining => comboRemaining[1];

        public float Player1LockRemaining => 0f;
        public float Player2LockRemaining => 0f;

        public PowerTokenKind GetPowerKind(string tokenId)
        {
            int index = FindToken(tokenId);

            return index < 0
                ? PowerTokenKind.None
                : specialKinds[index];
        }

        public bool TryDeliverSpecial(
            string tokenId,
            int pointerId,
            PlayerId owner,
            string cardId,
            out bool completed)
        {
            completed = false;

            if (!Owns(tokenId, pointerId))
                return false;

            try
            {
                if (IsTimeUp || IsCardLocked(owner, cardId))
                    return false;

                int slot = FindToken(tokenId);

                if (slot < 0 ||
                    specialKinds[slot] == PowerTokenKind.None)
                {
                    return false;
                }

                PowerTokenKind kind = specialKinds[slot];

                bool accepted = kind == PowerTokenKind.Poison
                    ? ApplyPoop(owner, cardId)
                    : TryUsePower(kind, owner, cardId, out completed);

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
            PlayerId actor,
            out bool removedTick)
        {
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

        private bool TickSpecialTokens(float deltaTime)
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

                specialLeft[i] -= deltaTime;

                if (specialLeft[i] <= 0f)
                {
                    specialKinds[i] = PowerTokenKind.None;
                    tokens[i].TryHide();
                    changed = true;
                }
            }

            spawnCountdown -= deltaTime;

            if (spawnCountdown > 0f)
                return changed;

            spawnCountdown = powerUpRules.SpawnInterval;

            int activeSpecials = 0;

            for (int i = 0; i < TokenSlots; i++)
            {
                if (specialKinds[i] != PowerTokenKind.None)
                    activeSpecials++;
            }

            if (activeSpecials >= powerUpRules.MaxActiveSpecials)
                return changed;

            List<int> slots = PowerTokenAvailability.GetEligibleSlots(
                RequiredAtTable,
                tokens,
                pendingIngredients,
                refillTimers,
                specialKinds,
                tokenOwners);

            if (slots.Count == 0)
                return changed;

            PowerTokenKind drawn = powerUpRules.Draw(random);

            if (drawn == PowerTokenKind.None)
                return changed;

            int chosen = slots[random.NextInt(0, slots.Count)];

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

        public int GetComboCount(PlayerId player)
        {
            int index = PlayerIndex(player);

            return index >= 0
                ? comboCounts[index]
                : 0;
        }

        public string GetLastCompletedCardId(PlayerId player)
        {
            int index = PlayerIndex(player);

            return index >= 0
                ? lastCompletedCardIds[index]
                : null;
        }

        public float GetComboFill01(PlayerId player)
        {
            int index = PlayerIndex(player);

            if (index < 0 || comboTotalSeconds[index] <= 0f)
                return 0f;

            return (float)Math.Max(
                0.0,
                Math.Min(
                    1.0,
                    comboRemaining[index] / comboTotalSeconds[index]));
        }

        public bool IsCompletedCard(
            PlayerId player,
            string cardId)
        {
            int playerIndex = PlayerIndex(player);

            if (playerIndex < 0)
                return false;

            int cardIndex = FindCard(playerIndex, cardId);

            return cardIndex >= 0 &&
                   cards[playerIndex, cardIndex] != null &&
                   cards[playerIndex, cardIndex].IsComplete;
        }

        public void EnablePowerUps(PowerUpRules settings)
        {
            powerUpRules = settings ??
                throw new ArgumentNullException(nameof(settings));

            spawnCountdown = powerUpRules.SpawnInterval;
        }

        public bool IsPlayerLocked(PlayerId player)
        {
            return false;
        }

        public float GetCardLockRemaining(
            PlayerId owner,
            string cardId)
        {
            int playerIndex = PlayerIndex(owner);

            if (playerIndex < 0 || string.IsNullOrEmpty(cardId))
                return 0f;

            int cardIndex = FindCard(playerIndex, cardId);

            return cardIndex < 0
                ? 0f
                : cardLockRemaining[playerIndex, cardIndex];
        }

        public bool IsCardLocked(
            PlayerId owner,
            string cardId)
        {
            return GetCardLockRemaining(owner, cardId) > 0f;
        }

        public bool IsWrongIngredientForCard(
            string tokenId,
            PlayerId owner,
            string cardId)
        {
            if (powerUpRules == null ||
                IsTimeUp ||
                IsCardLocked(owner, cardId))
            {
                return false;
            }

            int playerIndex = PlayerIndex(owner);

            if (playerIndex < 0)
                return false;

            int cardIndex = FindCard(playerIndex, cardId);
            int tokenIndex = FindToken(tokenId);

            if (cardIndex < 0 ||
                tokenIndex < 0 ||
                specialKinds[tokenIndex] != PowerTokenKind.None)
            {
                return false;
            }

            RecipeCardModel card = cards[playerIndex, cardIndex];
            IngredientToken token = tokens[tokenIndex];

            if (card == null ||
                card.IsComplete ||
                token == null ||
                token.IsConsumed ||
                !token.IsRevealed)
            {
                return false;
            }

            return !card.Progress.NeedsIngredient(token.Ingredient);
        }

        public bool ApplyWrongDeliveryPenalty(
            PlayerId owner,
            string cardId)
        {
            if (powerUpRules == null || IsTimeUp)
                return false;

            int playerIndex = PlayerIndex(owner);

            if (playerIndex < 0)
                return false;

            int cardIndex = FindCard(playerIndex, cardId);

            if (cardIndex < 0 ||
                cards[playerIndex, cardIndex] == null ||
                cards[playerIndex, cardIndex].IsComplete ||
                cardLockRemaining[playerIndex, cardIndex] > 0f)
            {
                return false;
            }

            cardLockRemaining[playerIndex, cardIndex] =
                powerUpRules.MultiplayerLockSeconds;

            return true;
        }

        [Obsolete("Use ApplyWrongDeliveryPenalty(PlayerId, string).")]
        public void ApplyWrongDeliveryPenalty(PlayerId player)
        {
        }

        private void RegisterCompletedCard(int playerIndex, int cardIndex)
        {
            cardLockRemaining[playerIndex, cardIndex] = 0f;

            if (powerUpRules == null)
            {
                scores[playerIndex]++;
                recipeTimers[playerIndex, cardIndex] = recipeSeconds;
                return;
            }

            if (comboRemaining[playerIndex] <= 0f)
            {
                comboRemaining[playerIndex] =
                    powerUpRules.ComboWindowSeconds;

                comboTotalSeconds[playerIndex] =
                    comboRemaining[playerIndex];

                comboCounts[playerIndex] = 0;
            }

            comboCounts[playerIndex] = Math.Min(
                CardsPerPlayer,
                comboCounts[playerIndex] + 1);

            scores[playerIndex] += comboCounts[playerIndex];

            if (comboCounts[playerIndex] >= 2)
            {
                ComboScored?.Invoke(
                    playerIndex == 0
                        ? PlayerId.Player1
                        : PlayerId.Player2,
                    cardIndex,
                    comboCounts[playerIndex]);
            }

            lastCompletedCardIds[playerIndex] =
                cards[playerIndex, cardIndex].CardId;

            recipeTimers[playerIndex, cardIndex] = -1f;
        }

        private bool AdvancePowerUps(float deltaTime)
        {
            if (powerUpRules == null)
                return false;

            bool changed = false;
            bool[] expired = new bool[2];

            for (int playerIndex = 0; playerIndex < 2; playerIndex++)
            {
                for (int cardIndex = 0;
                    cardIndex < CardsPerPlayer;
                    cardIndex++)
                {
                    if (cardLockRemaining[playerIndex, cardIndex] > 0f)
                    {
                        cardLockRemaining[playerIndex, cardIndex] =
                            Math.Max(
                                0f,
                                cardLockRemaining[playerIndex, cardIndex] -
                                deltaTime);
                    }
                }

                if (comboRemaining[playerIndex] <= 0f)
                    continue;

                comboRemaining[playerIndex] = Math.Max(
                    0f,
                    comboRemaining[playerIndex] - deltaTime);

                expired[playerIndex] =
                    comboRemaining[playerIndex] <= 0f ||
                    comboCounts[playerIndex] >= CardsPerPlayer;
            }

            if (expired[0] || expired[1])
            {
                RestoreSpecialTokensBeforeNewHand();

                var replacements = new List<int>[2]
                {
                    new List<int>(),
                    new List<int>()
                };

                for (int playerIndex = 0; playerIndex < 2; playerIndex++)
                {
                    if (!expired[playerIndex])
                        continue;

                    for (int cardIndex = 0;
                        cardIndex < CardsPerPlayer;
                        cardIndex++)
                    {
                        if (cards[playerIndex, cardIndex] == null ||
                            !cards[playerIndex, cardIndex].IsComplete)
                        {
                            continue;
                        }

                        replacements[playerIndex].Add(cardIndex);
                        cards[playerIndex, cardIndex] = null;
                        recipeTimers[playerIndex, cardIndex] = -1f;
                        cardLockRemaining[playerIndex, cardIndex] = 0f;
                    }
                }

                for (int playerIndex = 0; playerIndex < 2; playerIndex++)
                {
                    foreach (int cardIndex in replacements[playerIndex])
                    {
                        cards[playerIndex, cardIndex] = NewCard(playerIndex);
                        changed = true;
                    }
                }

                for (int playerIndex = 0; playerIndex < 2; playerIndex++)
                {
                    if (!expired[playerIndex])
                        continue;

                    comboCounts[playerIndex] = 0;
                    comboRemaining[playerIndex] = 0f;
                    comboTotalSeconds[playerIndex] = 0f;
                    lastCompletedCardIds[playerIndex] = null;
                }
            }

            return TickSpecialTokens(deltaTime) || changed;
        }

        public bool TryUsePower(
            PowerTokenKind kind,
            PlayerId player,
            string cardId,
            out bool completed)
        {
            completed = false;

            int playerIndex = PlayerIndex(player);

            if (powerUpRules == null ||
                IsTimeUp ||
                playerIndex < 0 ||
                IsCardLocked(player, cardId))
            {
                return false;
            }

            int cardIndex = FindCard(playerIndex, cardId);

            if (cardIndex < 0)
                return false;

            if (kind == PowerTokenKind.ExtraComboTime)
            {
                if (comboRemaining[playerIndex] <= 0f)
                    return false;

                comboRemaining[playerIndex] +=
                    powerUpRules.ExtraComboSeconds;

                comboTotalSeconds[playerIndex] +=
                    powerUpRules.ExtraComboSeconds;

                return true;
            }

            RecipeCardModel card = cards[playerIndex, cardIndex];

            if (card == null || card.IsComplete)
                return false;

            if (kind == PowerTokenKind.WildcardDuck)
            {
                foreach (IngredientType type
                    in card.Definition.RequiredIngredients)
                {
                    if (!card.Progress.NeedsIngredient(type))
                        continue;

                    if (!card.Progress.TryAddIngredient(type))
                        return false;

                    break;
                }
            }
            else if (kind == PowerTokenKind.CompleteRecipe)
            {
                foreach (IngredientType type
                    in card.Definition.RequiredIngredients)
                {
                    if (!card.Progress.NeedsIngredient(type))
                        continue;

                    if (!card.Progress.TryAddIngredient(type))
                        return false;
                }
            }
            else
            {
                return false;
            }

            if (card.IsComplete)
            {
                RegisterCompletedCard(playerIndex, cardIndex);
                completed = true;
            }

            return true;
        }

        public bool ApplyPoison(PlayerId player)
        {
            return false;
        }

        public bool ApplyPoop(PlayerId player, string cardId)
        {
            int playerIndex = PlayerIndex(player);

            if (powerUpRules == null ||
                IsTimeUp ||
                playerIndex < 0 ||
                IsCardLocked(player, cardId))
            {
                return false;
            }

            int cardIndex = FindCard(playerIndex, cardId);

            if (cardIndex < 0 ||
                cards[playerIndex, cardIndex] == null ||
                cards[playerIndex, cardIndex].IsComplete ||
                CountProgress(cards[playerIndex, cardIndex]) == 0)
            {
                return false;
            }

            cards[playerIndex, cardIndex] = RebuildWithoutOneTick(
                cards[playerIndex, cardIndex],
                out lastPoisonIngredient);

            EnsureIngredientAvailable(lastPoisonIngredient);
            return true;
        }

        private static int CountProgress(RecipeCardModel card)
        {
            int total = 0;
            var visited = new HashSet<IngredientType>();

            foreach (IngredientType type
                in card.Definition.RequiredIngredients)
            {
                if (visited.Add(type))
                    total += card.Progress.GetCompletedCount(type);
            }

            return total;
        }

        private static RecipeCardModel RebuildWithoutOneTick(
            RecipeCardModel card,
            out IngredientType removed)
        {
            var types = new List<IngredientType>();
            var visited = new HashSet<IngredientType>();

            foreach (IngredientType type
                in card.Definition.RequiredIngredients)
            {
                if (!visited.Add(type))
                    continue;

                for (int n = 0;
                    n < card.Progress.GetCompletedCount(type);
                    n++)
                {
                    types.Add(type);
                }
            }

            removed = types[types.Count - 1];
            types.RemoveAt(types.Count - 1);

            var result = new RecipeCardModel(
                card.CardId,
                card.Definition);

            foreach (IngredientType type in types)
                result.Progress.TryAddIngredient(type);

            return result;
        }

        private void EnsureIngredientAvailable(IngredientType needed)
        {
            var stock = new Dictionary<IngredientType, int>();

            foreach (IngredientType type in GetStockIncludingPending())
                stock[type] = GetCount(stock, type) + 1;

            if (GetCount(stock, needed) >= RequiredAtTable(needed))
                return;

            for (int i = 0; i < TokenSlots; i++)
            {
                if (specialKinds[i] != PowerTokenKind.None)
                    continue;

                IngredientType type = tokens[i] != null
                    ? tokens[i].Ingredient
                    : pendingIngredients[i];

                if (GetCount(stock, type) <= RequiredAtTable(type))
                    continue;

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
            return map.TryGetValue(type, out int count)
                ? count
                : 0;
        }
    }
}