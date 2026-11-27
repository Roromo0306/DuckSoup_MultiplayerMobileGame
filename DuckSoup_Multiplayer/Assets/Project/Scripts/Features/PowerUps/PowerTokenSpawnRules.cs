using System;
using System.Collections.Generic;
using DuckSoup.Core.Randomization;
using DuckSoup.Domain;
using DuckSoup.Features.Round.Models;

namespace DuckSoup.Features.PowerUps
{
    public sealed class PowerTokenSpawnRules
    {
        public float RampStart { get; }
        public float RampEnd { get; }
        public float LeadSeconds { get; }
        public bool CatchUpEnabled { get; }

        public Func<string, PlayerId> SideResolver { get; }

        private readonly float[] deadlines = new float[5];

        private readonly double bonus6To10;
        private readonly double bonus11To15;
        private readonly double bonus16Plus;

        public PowerTokenSpawnRules(
            float rampStart = 90f,
            float rampEnd = 30f,
            float extraTimeDeadline = 70f,
            float duckDeadline = 60f,
            float poopDeadline = 45f,
            float completeRecipeDeadline = 30f,
            float leadSeconds = 3f,
            bool catchUpEnabled = true,
            float bonus6To10Percent = 20f,
            float bonus11To15Percent = 50f,
            float bonus16PlusPercent = 100f,
            Func<string, PlayerId> sideResolver = null)
        {
            Check(rampStart);
            Check(rampEnd);
            Check(extraTimeDeadline);
            Check(duckDeadline);
            Check(poopDeadline);
            Check(completeRecipeDeadline);
            Check(leadSeconds);
            Check(bonus6To10Percent);
            Check(bonus11To15Percent);
            Check(bonus16PlusPercent);

            if (rampStart <= rampEnd || leadSeconds <= 0f)
            {
                throw new ArgumentException(
                    "Ramp Start debe superar Ramp End; " +
                    "Lead debe ser positivo.");
            }

            RampStart = rampStart;
            RampEnd = rampEnd;
            LeadSeconds = leadSeconds;
            CatchUpEnabled = catchUpEnabled;
            SideResolver = sideResolver;

            deadlines[(int)PowerTokenKind.ExtraComboTime] =
                extraTimeDeadline;

            deadlines[(int)PowerTokenKind.WildcardDuck] =
                duckDeadline;

            deadlines[(int)PowerTokenKind.Poison] =
                poopDeadline;

            deadlines[(int)PowerTokenKind.CompleteRecipe] =
                completeRecipeDeadline;

            bonus6To10 = bonus6To10Percent / 100.0;
            bonus11To15 = bonus11To15Percent / 100.0;
            bonus16Plus = bonus16PlusPercent / 100.0;
        }

        public double GetTimeMultiplier(float remainingSeconds)
        {
            double progress =
                (RampStart - (double)remainingSeconds) /
                (RampStart - RampEnd);

            return 1.0 +
                Math.Max(0.0, Math.Min(1.0, progress));
        }

        public double GetTrailingSideWeight(
            int player1Score,
            int player2Score)
        {
            if (!CatchUpEnabled)
                return 1.0;

            long difference =
                Math.Abs((long)player1Score - player2Score);

            if (difference <= 5)
                return 1.0;

            if (difference <= 10)
                return 1.0 + bonus6To10;

            if (difference <= 15)
                return 1.0 + bonus11To15;

            return 1.0 + bonus16Plus;
        }

        public PowerTokenKind GetRequiredKind(
            float remainingSeconds,
            int[] appearances)
        {
            PowerTokenKind selected = PowerTokenKind.None;
            float earliestDeadline = -1f;

            for (int i = 1; i <= 4; i++)
            {
                if (appearances[i] > 0 ||
                    remainingSeconds > deadlines[i] + LeadSeconds)
                {
                    continue;
                }

                if (deadlines[i] <= earliestDeadline)
                    continue;

                earliestDeadline = deadlines[i];
                selected = (PowerTokenKind)i;
            }

            return selected;
        }

        private static void Check(float value)
        {
            if (value < 0f ||
                float.IsNaN(value) ||
                float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }

    public sealed class AdaptivePowerTokenSpawner
    {
        public PowerUpRules Rules { get; }

        private readonly IRandomSource random;
        private readonly IngredientToken[] tokens;
        private readonly PowerTokenKind[] kinds;
        private readonly float[] lifetimes;

        private readonly Dictionary<string, int> heldTokens;
        private readonly Func<List<int>> eligibleSlots;
        private readonly Func<int, PlayerId> sideOf;

        private readonly int[] appearances = new int[5];

        private float checkCountdown;

        public AdaptivePowerTokenSpawner(
            PowerUpRules rules,
            IRandomSource random,
            IngredientToken[] tokens,
            PowerTokenKind[] kinds,
            float[] lifetimes,
            Dictionary<string, int> heldTokens,
            Func<List<int>> eligibleSlots,
            Func<int, PlayerId> sideOf = null)
        {
            Rules = rules ??
                throw new ArgumentNullException(nameof(rules));

            if (rules.AdaptiveSpawning == null)
            {
                throw new ArgumentException(
                    "Faltan las reglas de aparicion adaptativa.");
            }

            this.random = random ??
                throw new ArgumentNullException(nameof(random));

            this.tokens = tokens ??
                throw new ArgumentNullException(nameof(tokens));

            this.kinds = kinds ??
                throw new ArgumentNullException(nameof(kinds));

            this.lifetimes = lifetimes ??
                throw new ArgumentNullException(nameof(lifetimes));

            this.heldTokens = heldTokens ??
                throw new ArgumentNullException(nameof(heldTokens));

            this.eligibleSlots = eligibleSlots ??
                throw new ArgumentNullException(nameof(eligibleSlots));

            if (tokens.Length != kinds.Length ||
                tokens.Length != lifetimes.Length)
            {
                throw new ArgumentException(
                    "Los arrays de fichas deben tener la misma longitud.");
            }

            this.sideOf = sideOf;

            checkCountdown = rules.SpawnInterval;
        }

        public int GetAppearanceCount(PowerTokenKind kind)
        {
            int index = (int)kind;

            return index > 0 && index < appearances.Length
                ? appearances[index]
                : 0;
        }

        public bool Tick(
            float dt,
            float remainingSeconds,
            int player1Score = 0,
            int player2Score = 0)
        {
            if (dt <= 0f || remainingSeconds <= 0f)
                return false;

            bool changed = false;
            int active = 0;

            for (int i = 0; i < tokens.Length; i++)
            {
                if (kinds[i] == PowerTokenKind.None ||
                    tokens[i] == null)
                {
                    continue;
                }

                if (!heldTokens.ContainsKey(tokens[i].Id))
                {
                    lifetimes[i] -= dt;

                    if (lifetimes[i] <= 0f)
                    {
                        kinds[i] = PowerTokenKind.None;
                        lifetimes[i] = 0f;

                        tokens[i].TryHide();

                        changed = true;
                        continue;
                    }
                }

                active++;
            }

            checkCountdown -= dt;

            PowerTokenKind required =
                Rules.AdaptiveSpawning.GetRequiredKind(
                    remainingSeconds,
                    appearances);

            bool forced = required != PowerTokenKind.None;

            if (!forced && checkCountdown > 0f)
                return changed;

            if (!forced)
                checkCountdown = Rules.SpawnInterval;

            if (!forced && active >= Rules.MaxActiveSpecials)
                return changed;

            List<int> candidates =
                active < Rules.MaxActiveSpecials
                    ? eligibleSlots()
                    : new List<int>();

            // Los minimos tienen prioridad sobre el sorteo.
            // Si hace falta, se sustituye otro especial oculto
            // que nadie este sosteniendo.
            if (forced && candidates.Count == 0)
            {
                for (int i = 0; i < tokens.Length; i++)
                {
                    IngredientToken token = tokens[i];

                    if (token != null &&
                        kinds[i] != PowerTokenKind.None &&
                        !token.IsConsumed &&
                        !token.IsRevealed &&
                        !heldTokens.ContainsKey(token.Id))
                    {
                        candidates.Add(i);
                    }
                }
            }

            // Si no existe una ficha segura, el minimo queda
            // pendiente y se reintenta en la siguiente actualizacion.
            if (candidates.Count == 0)
                return changed;

            PowerTokenKind drawn = forced
                ? required
                : Draw(remainingSeconds);

            if (drawn == PowerTokenKind.None)
                return changed;

            int chosen = ChooseSlot(
                candidates,
                player1Score,
                player2Score);

            kinds[chosen] = drawn;
            lifetimes[chosen] = Rules.TokenLifetime;

            appearances[(int)drawn]++;

            checkCountdown = Rules.SpawnInterval;

            return true;
        }

        public PowerTokenKind Draw(float remainingSeconds)
        {
            double multiplier =
                Rules.AdaptiveSpawning.GetTimeMultiplier(
                    remainingSeconds);

            double totalSpecial = 0.0;

            for (int i = 1; i <= 4; i++)
            {
                totalSpecial +=
                    Rules.GetBaseProbability((PowerTokenKind)i) *
                    multiplier;
            }

            // Con los valores actuales el maximo es 66%.
            // Si los valores editados superan 100%, se normalizan.
            double normalizer = Math.Max(1.0, totalSpecial);

            double roll =
                random.NextInt(0, 1000000) / 1000000.0;

            for (int i = 1; i <= 4; i++)
            {
                roll -=
                    Rules.GetBaseProbability((PowerTokenKind)i) *
                    multiplier / normalizer;

                if (roll < 0.0)
                    return (PowerTokenKind)i;
            }

            return PowerTokenKind.None;
        }

        private int ChooseSlot(
            List<int> candidates,
            int player1Score,
            int player2Score)
        {
            if (sideOf == null)
            {
                return candidates[
                    random.NextInt(0, candidates.Count)];
            }

            var player1 = new List<int>();
            var player2 = new List<int>();

            foreach (int slot in candidates)
            {
                PlayerId side = sideOf(slot);

                if (side == PlayerId.Player1)
                    player1.Add(slot);
                else if (side == PlayerId.Player2)
                    player2.Add(slot);
            }

            if (player1.Count == 0 && player2.Count == 0)
            {
                return candidates[
                    random.NextInt(0, candidates.Count)];
            }

            List<int> selected;

            if (player1.Count == 0)
            {
                selected = player2;
            }
            else if (player2.Count == 0)
            {
                selected = player1;
            }
            else
            {
                double trailingWeight =
                    Rules.AdaptiveSpawning.GetTrailingSideWeight(
                        player1Score,
                        player2Score);

                double weight1 = player1Score < player2Score
                    ? trailingWeight
                    : 1.0;

                double weight2 = player2Score < player1Score
                    ? trailingWeight
                    : 1.0;

                double roll =
                    random.NextInt(0, 1000000) / 1000000.0;

                selected = roll < weight1 / (weight1 + weight2)
                    ? player1
                    : player2;
            }

            return selected[
                random.NextInt(0, selected.Count)];
        }
    }
}