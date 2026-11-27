using System;
using System.Collections.Generic;

namespace DuckSoup.Features.GameFeel.Menu
{
    public enum MedalRewardKind
    {
        Bronze,
        Silver,
        Gold,
        Master
    }

    public static class SinglePlayerRewardHandoff
    {
        public struct Reward
        {
            public int PreviousTotal;
            public int Earned;
            public string TableId;
            public int PreviousHighScore;
            public int HighScore;
        }

        private static bool pending;
        private static bool pendingMedals;
        private static bool pendingSelectorMedals;

        private static Reward reward;

        public static void Queue(int previousTotal, int earned)
        {
            Queue(previousTotal, earned, null, 0, 0);
        }

        public static void Queue(
            int previousTotal,
            int earned,
            string tableId,
            int previousHighScore,
            int highScore)
        {
            Clear();

            if (previousTotal < 0 ||
                earned < 0 ||
                previousHighScore < 0 ||
                highScore < 0)
            {
                return;
            }

            reward = new Reward
            {
                PreviousTotal = previousTotal,
                Earned = earned,
                TableId = tableId,
                PreviousHighScore = previousHighScore,
                HighScore = highScore
            };

            pending = true;

            pendingMedals =
                !string.IsNullOrWhiteSpace(tableId) &&
                highScore > previousHighScore;

            pendingSelectorMedals = pendingMedals;
        }

        public static bool TryConsume(out Reward result)
        {
            result = reward;

            bool hadPending = pending;
            pending = false;

            return hadPending;
        }

        public static bool TryConsumeMedalReward(out Reward result)
        {
            result = reward;

            bool hadPending = pendingMedals;
            pendingMedals = false;

            return hadPending;
        }

        // Aviso independiente para las medallas del selector.
        public static bool TryConsumeSelectorMedalReward(
            string tableId,
            out Reward result)
        {
            result = reward;

            if (!pendingSelectorMedals ||
                string.IsNullOrWhiteSpace(tableId) ||
                !string.Equals(
                    tableId.Trim(),
                    reward.TableId?.Trim(),
                    StringComparison.Ordinal))
            {
                return false;
            }

            pendingSelectorMedals = false;

            return true;
        }

        public static List<MedalRewardKind> GetNewMedals(
            Reward result,
            int bronze,
            int silver,
            int gold,
            int master)
        {
            var medals = new List<MedalRewardKind>();

            int[] thresholds =
            {
                bronze,
                silver,
                gold,
                master
            };

            for (int i = 0; i < thresholds.Length; i++)
            {
                if (result.PreviousHighScore < thresholds[i] &&
                    result.HighScore >= thresholds[i])
                {
                    medals.Add((MedalRewardKind)i);
                }
            }

            return medals;
        }

        public static void Clear()
        {
            pending = false;
            pendingMedals = false;
            pendingSelectorMedals = false;

            reward = default;
        }
    }
}