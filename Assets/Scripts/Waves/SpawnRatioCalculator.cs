using System;

namespace ATM.Waves
{
    public static class SpawnRatioCalculator
    {
        // The array follows EnemyType order. These are Prototype balance values.
        public static int[] GetRegularWeights(int wave)
        {
            if (wave < 1)
                throw new ArgumentOutOfRangeException(nameof(wave));

            return wave switch
            {
                < 5 => new[] { 100, 0, 0 },
                < 10 => new[] { 70, 30, 0 },
                < 20 => new[] { 60, 25, 15 },
                < 40 => new[] { 50, 30, 20 },
                _ => new[] { 40, 35, 25 }
            };
        }
    }
}
