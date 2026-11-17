using System;

namespace ATM.Waves
{
    public static class DifficultyScaler
    {
        public static int GetRegularEnemyCount(int wave) =>
            Math.Min(8 + 2 * (GetCompletedWaves(wave) / 3), 40);

        public static bool IsBossWave(int wave) => (GetCompletedWaves(wave) + 1) % 10 == 0;

        public static float ScaleHP(float baseHP, int wave) =>
            ScaleExponential(baseHP, 1.07, GetCompletedWaves(wave));

        public static float ScaleAttack(float baseAttack, int wave) =>
            ScaleExponential(baseAttack, 1.045, GetCompletedWaves(wave));

        public static float ScaleDefense(float baseDefense, int wave) =>
            (float)Math.Min(float.MaxValue, baseDefense + .5 * GetCompletedWaves(wave));

        private static float ScaleExponential(float baseValue, double growth, int completedWaves)
        {
            if (baseValue <= 0f)
                return 0f;

            // Keep combat arithmetic finite even in extremely late waves.
            return (float)Math.Min(float.MaxValue, baseValue * Math.Pow(growth, completedWaves));
        }

        private static int GetCompletedWaves(int wave)
        {
            if (wave < 1)
                throw new ArgumentOutOfRangeException(nameof(wave));
            return wave - 1;
        }
    }
}
