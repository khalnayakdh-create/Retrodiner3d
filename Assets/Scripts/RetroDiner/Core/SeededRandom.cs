using System.Collections.Generic;

namespace RetroDiner.Core
{
    /// <summary>Deterministic RNG wrapper so level generation is reproducible from a seed.</summary>
    public class SeededRandom
    {
        readonly System.Random rng;
        public int Seed { get; private set; }

        public SeededRandom(int seed)
        {
            Seed = seed;
            rng = new System.Random(seed);
        }

        public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : rng.Next(maxExclusive);
        public int Range(int minInclusive, int maxExclusive) => rng.Next(minInclusive, maxExclusive);
        public float Value() => (float)rng.NextDouble();
        public float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);
        public bool Chance(float probability) => rng.NextDouble() < probability;

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        public T Pick<T>(IList<T> list) => list[rng.Next(list.Count)];
    }
}
