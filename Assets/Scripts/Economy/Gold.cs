using System;
using System.Numerics;

namespace ATM.Gameplay
{
    internal sealed class Gold
    {
        public BigInteger Earned { get; private set; }
        public BigInteger Spent { get; private set; }
        public long Balance { get; private set; }

        public static decimal Multiplier(int level) => 1m + level / 10m;

        public static long Reward(int wave, int bonusLevel = 0)
        {
            double waveAmount = 10d * Math.Pow(1.2d, wave - 1);
            long baseDrop = waveAmount >= long.MaxValue ? long.MaxValue : (long)Math.Floor(waveAmount);
            decimal finalDrop = baseDrop * Multiplier(bonusLevel);
            return finalDrop >= long.MaxValue ? long.MaxValue : (long)decimal.Floor(finalDrop);
        }

        public void Credit(long reward)
        {
            long deposited = Math.Min(reward, long.MaxValue - Balance);
            Balance += deposited;
            Earned += deposited;
        }

        public bool TrySpend(long amount)
        {
            if (amount < 0 || Balance < amount) return false;
            Balance -= amount;
            Spent += amount;
            return true;
        }
    }
}
