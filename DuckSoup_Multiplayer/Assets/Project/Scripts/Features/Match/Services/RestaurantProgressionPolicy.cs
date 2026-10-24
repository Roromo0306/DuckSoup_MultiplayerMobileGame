
using System;
using System.Collections.Generic;

namespace DuckSoup.Features.Match.Services
{
    /// <summary>
    /// Calcula el nivel del restaurante
    /// según las monedas acumuladas.
    /// </summary>
    public sealed class RestaurantProgressionPolicy
    {
        private readonly IReadOnlyList<int> thresholds;

        public int MaxLevel => thresholds.Count;

        public RestaurantProgressionPolicy(
            IEnumerable<int> upgradeThresholds)
        {
            if (upgradeThresholds == null)
                throw new ArgumentNullException(
                    nameof(upgradeThresholds));

            var values = new List<int>();
            int previous = 0;

            foreach (int threshold in upgradeThresholds)
            {
                if (threshold <= previous)
                {
                    throw new ArgumentException(
                        "Los umbrales deben ser positivos " +
                        "y estrictamente crecientes.");
                }

                values.Add(threshold);
                previous = threshold;
            }

            if (values.Count == 0)
                throw new ArgumentException(
                    "Se necesita al menos una mejora.");

            thresholds = values.AsReadOnly();
        }

        public int GetLevel(int totalCoins)
        {
            if (totalCoins < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(totalCoins));

            int level = 0;

            foreach (int threshold in thresholds)
            {
                if (totalCoins < threshold)
                    break;

                level++;
            }

            return level;
        }

        /// <summary>
        /// Devuelve el umbral de la próxima mejora.
        /// Si está al máximo, devuelve 0.
        /// </summary>
        public int GetNextThreshold(int currentLevel)
        {
            if (currentLevel < 0 ||
                currentLevel > MaxLevel)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentLevel));
            }

            return currentLevel == MaxLevel
                ? 0
                : thresholds[currentLevel];
        }
    }
}
