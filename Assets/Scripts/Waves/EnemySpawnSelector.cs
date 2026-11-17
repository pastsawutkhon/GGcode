using System;

namespace ATM.Waves
{
    public enum EnemyType { Melee, Ranged, Fast }

    public sealed class EnemySpawnSelector
    {
        private readonly int[] cumulativeWeights;

        public int TotalWeight => cumulativeWeights[cumulativeWeights.Length - 1];

        public EnemySpawnSelector(int wave)
        {
            cumulativeWeights = SpawnRatioCalculator.GetRegularWeights(wave);
            for (int i = 1; i < cumulativeWeights.Length; i++)
                cumulativeWeights[i] += cumulativeWeights[i - 1];
        }

        public EnemyType SelectByRoll(int roll)
        {
            if (roll < 0 || roll >= TotalWeight)
                throw new ArgumentOutOfRangeException(nameof(roll));

            // A strict comparison skips zero-weight types and leaves no overlapping buckets.
            for (int i = 0; i < cumulativeWeights.Length; i++)
            {
                if (roll < cumulativeWeights[i])
                    return (EnemyType)i;
            }

            throw new InvalidOperationException("Spawn weights must cover the roll.");
        }
    }
}
