using System;
using DuckSoup.Core.Randomization;

namespace DuckSoup.Features.PowerUps
{
    public enum PowerTokenKind
    {
        None = 0,
        WildcardDuck = 1,
        Poison = 2,
        CompleteRecipe = 3,
        ExtraComboTime = 4
    }

    // Pure C# rules. Sessions generate specials inside their existing token slots.
    public sealed class PowerUpRules
    {
        public float ComboWindowSeconds { get; private set; }
        public float ExtraComboSeconds { get; private set; }
        public float WrongSinglePlayerPenaltySeconds { get; private set; }
        public float MultiplayerLockSeconds { get; private set; }
        public float SpawnInterval { get; private set; }
        public float TokenLifetime { get; private set; }
        public int MaxActiveSpecials { get; private set; }

        private readonly float noneWeight;
        private readonly float duckWeight;
        private readonly float poisonWeight;
        private readonly float instantWeight;
        private readonly float clockWeight;

        // Compatible with the earlier smoke tests.
        public PowerUpRules(float combo, float extra, float penalty, float multiplayerLock)
            : this(combo, extra, penalty, multiplayerLock,
                2.5f, 10f, 2, 75f, 10f, 7f, 3f, 5f)
        {
        }

        public PowerUpRules(float combo, float extra, float penalty, float multiplayerLock,
            float interval, float lifetime, int maximum,
            float none, float duck, float poison, float instant, float clock)
        {
            RequirePositive(combo, nameof(combo));
            RequirePositive(extra, nameof(extra));
            RequirePositive(penalty, nameof(penalty));
            RequirePositive(multiplayerLock, nameof(multiplayerLock));
            RequirePositive(interval, nameof(interval));
            RequirePositive(lifetime, nameof(lifetime));
            if (maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
            RequireNonNegative(none, nameof(none));
            RequireNonNegative(duck, nameof(duck));
            RequireNonNegative(poison, nameof(poison));
            RequireNonNegative(instant, nameof(instant));
            RequireNonNegative(clock, nameof(clock));

            ComboWindowSeconds = combo;
            ExtraComboSeconds = extra;
            WrongSinglePlayerPenaltySeconds = penalty;
            MultiplayerLockSeconds = multiplayerLock;
            SpawnInterval = interval;
            TokenLifetime = lifetime;
            MaxActiveSpecials = maximum;
            noneWeight = none;
            duckWeight = duck;
            poisonWeight = poison;
            instantWeight = instant;
            clockWeight = clock;
        }

        public PowerTokenKind Draw(IRandomSource random)
        {
            float total = noneWeight + duckWeight + poisonWeight +
                instantWeight + clockWeight;
            if (total <= 0f) return PowerTokenKind.None;
            float roll = (random.NextInt(0, 1000000) / 1000000f) * total;
            if ((roll -= noneWeight) < 0f) return PowerTokenKind.None;
            if ((roll -= duckWeight) < 0f) return PowerTokenKind.WildcardDuck;
            if ((roll -= poisonWeight) < 0f) return PowerTokenKind.Poison;
            if ((roll -= instantWeight) < 0f) return PowerTokenKind.CompleteRecipe;
            return PowerTokenKind.ExtraComboTime;
        }

        private static void RequirePositive(float value, string name)
        {
            if (value <= 0f || float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name);
        }

        private static void RequireNonNegative(float value, string name)
        {
            if (value < 0f || float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
