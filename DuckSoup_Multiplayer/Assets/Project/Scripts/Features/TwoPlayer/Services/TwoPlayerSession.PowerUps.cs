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
            PlayerId owner,
            string cardId,
            out bool completed)
        {
            completed = false;

            if (!Owns(tokenId, pointerId) ||
                IsTimeUp ||
                IsCardLocked(owner, cardId))
            {
                return false;
            }

            int slot = FindToken(tokenId);

            if (slot < 0)
                return false;

            PowerTokenKind kind = specialKinds[slot];

            if (kind == PowerTokenKind.None ||
                kind == PowerTokenKind.Poison)
            {
                return false;
            }

            bool accepted = TryUsePower(
                kind,
                owner,
                cardId,
                out completed);

            if (accepted)
            {
                ConsumeSpecial(
                    slot,
                    tokenId,
                    pointerId,
                    IngredientType.None);
            }
            else
            {
                CancelGesture(tokenId, pointerId);
            }

            return accepted;
        }

        public bool TryTouchPoison(
            string tokenId,
            int pointerId,
            PlayerId actor,
            out bool removedTick)
        {
            removedTick = false;

            if (!Owns(tokenId, pointerId) || IsTimeUp)
                return false;

            int slot = FindToken(tokenId);

            if (slot < 0 ||
                specialKinds[slot] != PowerTokenKind.Poison)
            {
                return false;
            }

            removedTick = ApplyPoison(actor);

            ConsumeSpecial(
                slot,
                tokenId,
                pointerId,
                removedTick
                    ? lastPoisonIngredient
                    : IngredientType.None);

            return true;
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

        private bool TickSpecialTokens(float dt)
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

            var slots =
                PowerTokenAvailability.GetEligibleSlots(
                    RequiredAtTable,
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

            specialLeft[chosen] =
                powerUpRules.TokenLifetime;

            return true;
        }

        private void RestoreSpecialTokensBeforeNewHand()
        {
            for (int i = 0; i < TokenSlots; i++)
            {
                if (specialKinds[i] == PowerTokenKind.None)
                    continue;

                specialKinds[i] = PowerTokenKind.None;
                specialLeft[i] = 0f;

                if (tokens[i] != null &&
                    !tokenOwners.ContainsKey(tokens[i].Id))
                {
                    tokens[i].TryHide();
                }
            }
        }

        // -----------------------------------------------
        // INDEPENDENT COMBOS
        // -----------------------------------------------

        private PowerUpRules powerUpRules;

        private readonly float[] comboRemaining =
            new float[2];

        private readonly float[] comboTotalSeconds =
            new float[2];

        private readonly int[] comboCounts =
            new int[2];

        private readonly string[] lastCompletedCardIds =
            new string[2];

        public bool PowerUpsEnabled => powerUpRules != null;

        public float Player1ComboRemaining =>
            comboRemaining[0];

        public float Player2ComboRemaining =>
            comboRemaining[1];

        public int GetComboCount(PlayerId player)
        {
            int p = PlayerIndex(player);

            return p >= 0
                ? comboCounts[p]
                : 0;
        }

        public string GetLastCompletedCardId(PlayerId player)
        {
            int p = PlayerIndex(player);

            return p >= 0
                ? lastCompletedCardIds[p]
                : null;
        }

        public float GetComboFill01(PlayerId player)
        {
            int p = PlayerIndex(player);

            if (p < 0 || comboTotalSeconds[p] <= 0f)
                return 0f;

            return (float)Math.Max(
                0.0,
                Math.Min(
                    1.0,
                    comboRemaining[p] /
                    comboTotalSeconds[p]));
        }

        public bool IsCompletedCard(
            PlayerId player,
            string cardId)
        {
            int p = PlayerIndex(player);

            if (p < 0)
                return false;

            int c = FindCard(p, cardId);

            return c >= 0 &&
                   cards[p, c] != null &&
                   cards[p, c].IsComplete;
        }

        // -----------------------------------------------
        // LOCKS BY CARD
        // -----------------------------------------------

        private readonly float[,] cardLockRemaining =
            new float[2, CardsPerPlayer];

        // Legacy properties used by the previous HUD.
        public float Player1LockRemaining => 0f;
        public float Player2LockRemaining => 0f;

        public void EnablePowerUps(PowerUpRules settings)
        {
            powerUpRules = settings ??
                throw new ArgumentNullException(nameof(settings));

            spawnCountdown = powerUpRules.SpawnInterval;
        }

        public bool IsPlayerLocked(PlayerId id)
        {
            return false;
        }

        public float GetCardLockRemaining(
            PlayerId owner,
            string cardId)
        {
            int p = PlayerIndex(owner);

            if (p < 0 || string.IsNullOrEmpty(cardId))
                return 0f;

            int c = FindCard(p, cardId);

            return c < 0
                ? 0f
                : cardLockRemaining[p, c];
        }

        public bool IsCardLocked(
            PlayerId owner,
            string cardId)
        {
            return GetCardLockRemaining(
                owner,
                cardId) > 0f;
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

            int p = PlayerIndex(owner);

            if (p < 0)
                return false;

            int c = FindCard(p, cardId);
            int t = FindToken(tokenId);

            if (c < 0 ||
                t < 0 ||
                specialKinds[t] != PowerTokenKind.None)
            {
                return false;
            }

            RecipeCardModel card = cards[p, c];
            IngredientToken token = tokens[t];

            if (card == null ||
                card.IsComplete ||
                token == null ||
                token.IsConsumed ||
                !token.IsRevealed)
            {
                return false;
            }

            return !card.Progress.NeedsIngredient(
                token.Ingredient);
        }

        public bool ApplyWrongDeliveryPenalty(
            PlayerId owner,
            string cardId)
        {
            if (powerUpRules == null || IsTimeUp)
                return false;

            int p = PlayerIndex(owner);

            if (p < 0)
                return false;

            int c = FindCard(p, cardId);

            if (c < 0 ||
                cards[p, c] == null ||
                cards[p, c].IsComplete ||
                cardLockRemaining[p, c] > 0f)
            {
                return false;
            }

            cardLockRemaining[p, c] =
                powerUpRules.MultiplayerLockSeconds;

            return true;
        }

        [Obsolete(
            "Use ApplyWrongDeliveryPenalty(PlayerId, string).")]
        public void ApplyWrongDeliveryPenalty(PlayerId player)
        {
        }

        // -----------------------------------------------
        // RECIPE COMPLETION AND SCORE
        // -----------------------------------------------

        private void RegisterCompletedCard(int p, int c)
        {
            cardLockRemaining[p, c] = 0f;

            if (powerUpRules == null)
            {
                scores[p]++;
                recipeTimers[p, c] = recipeSeconds;
                return;
            }

            if (comboRemaining[p] <= 0f)
            {
                comboRemaining[p] =
                    powerUpRules.ComboWindowSeconds;

                comboTotalSeconds[p] =
                    comboRemaining[p];

                comboCounts[p] = 0;
            }

            comboCounts[p] = Math.Min(
                CardsPerPlayer,
                comboCounts[p] + 1);

            scores[p] += comboCounts[p];

            lastCompletedCardIds[p] =
                cards[p, c].CardId;

            recipeTimers[p, c] = -1f;
        }

        private bool AdvancePowerUps(float dt)
        {
            if (powerUpRules == null)
                return false;

            bool changed = false;
            bool[] expired = new bool[2];

            for (int p = 0; p < 2; p++)
            {
                // Los candados siguen siendo
                // independientes para cada carta.
                for (int c = 0; c < CardsPerPlayer; c++)
                {
                    if (cardLockRemaining[p, c] > 0f)
                    {
                        cardLockRemaining[p, c] = Math.Max(
                            0f,
                            cardLockRemaining[p, c] - dt);
                    }
                }

                if (comboRemaining[p] <= 0f)
                    continue;

                comboRemaining[p] = Math.Max(
                    0f,
                    comboRemaining[p] - dt);

                expired[p] = comboRemaining[p] <= 0f;
            }

            if (expired[0] || expired[1])
            {
                RestoreSpecialTokensBeforeNewHand();

                var replacements = new List<int>[2]
                {
                    new List<int>(),
                    new List<int>()
                };

                // Primero retirar solo las cartas
                // completadas de los combos terminados.
                for (int p = 0; p < 2; p++)
                {
                    if (!expired[p])
                        continue;

                    for (int c = 0; c < CardsPerPlayer; c++)
                    {
                        if (cards[p, c] == null ||
                            !cards[p, c].IsComplete)
                        {
                            continue;
                        }

                        replacements[p].Add(c);

                        cards[p, c] = null;
                        recipeTimers[p, c] = -1f;
                        cardLockRemaining[p, c] = 0f;
                    }
                }

                // Después generar las sustitutas.
                // Las cartas incompletas siguen
                // reservando sus ingredientes.
                for (int p = 0; p < 2; p++)
                {
                    foreach (int c in replacements[p])
                    {
                        cards[p, c] = NewCard(p);
                        changed = true;
                    }
                }

                for (int p = 0; p < 2; p++)
                {
                    if (!expired[p])
                        continue;

                    comboCounts[p] = 0;
                    comboTotalSeconds[p] = 0f;
                    lastCompletedCardIds[p] = null;
                }
            }

            return TickSpecialTokens(dt) || changed;
        }

        public bool TryUsePower(
            PowerTokenKind kind,
            PlayerId player,
            string cardId,
            out bool completed)
        {
            completed = false;

            int p = PlayerIndex(player);

            if (powerUpRules == null ||
                IsTimeUp ||
                p < 0 ||
                IsCardLocked(player, cardId))
            {
                return false;
            }

            int c = FindCard(p, cardId);

            if (c < 0)
                return false;

            if (kind == PowerTokenKind.ExtraComboTime)
            {
                if (comboRemaining[p] <= 0f)
                    return false;

                comboRemaining[p] +=
                    powerUpRules.ExtraComboSeconds;

                comboTotalSeconds[p] +=
                    powerUpRules.ExtraComboSeconds;

                return true;
            }

            RecipeCardModel card = cards[p, c];

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
                RegisterCompletedCard(p, c);
                completed = true;
            }

            return true;
        }

        // -----------------------------------------------
        // POISON
        // -----------------------------------------------

        public bool ApplyPoison(PlayerId player)
        {
            int p = PlayerIndex(player);

            if (powerUpRules == null ||
                IsTimeUp ||
                p < 0)
            {
                return false;
            }

            int bestSlot = -1;
            int best = 0;

            for (int c = 0; c < CardsPerPlayer; c++)
            {
                if (cards[p, c] == null ||
                    cards[p, c].IsComplete ||
                    cardLockRemaining[p, c] > 0f)
                {
                    continue;
                }

                int count = CountProgress(cards[p, c]);

                if (count > best)
                {
                    bestSlot = c;
                    best = count;
                }
            }

            if (bestSlot < 0)
                return false;

            IngredientType lostType;

            cards[p, bestSlot] = RebuildWithoutOneTick(
                cards[p, bestSlot],
                out lostType);

            lastPoisonIngredient = lostType;
            EnsureIngredientAvailable(lostType);

            return true;
        }

        private static int CountProgress(
            RecipeCardModel card)
        {
            int total = 0;
            var visited = new HashSet<IngredientType>();

            foreach (IngredientType type
                in card.Definition.RequiredIngredients)
            {
                if (visited.Add(type))
                {
                    total +=
                        card.Progress.GetCompletedCount(type);
                }
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
            {
                result.Progress.TryAddIngredient(type);
            }

            return result;
        }

        private void EnsureIngredientAvailable(
            IngredientType needed)
        {
            var stock = new Dictionary<IngredientType, int>();

            foreach (IngredientType type
                in GetStockIncludingPending())
            {
                stock[type] =
                    GetCount(stock, type) + 1;
            }

            if (GetCount(stock, needed) >=
                RequiredAtTable(needed))
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
                    RequiredAtTable(type))
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