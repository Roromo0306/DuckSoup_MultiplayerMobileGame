
using System;

namespace DuckSoup.Core.Randomization
{
    /// <summary>
    /// Implementación de IRandomSource
    /// basada en System.Random.
    ///
    /// Permite utilizar una semilla fija
    /// para reproducir secuencias durante las pruebas.
    /// </summary>
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random random;

        public SystemRandomSource()
        {
            random = new Random();
        }

        public SystemRandomSource(int seed)
        {
            random = new Random(seed);
        }

        public int NextInt(
            int minInclusive,
            int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive),
                    "El máximo debe ser mayor que el mínimo.");
            }

            return random.Next(
                minInclusive,
                maxExclusive);
        }
    }
}
