using System;

namespace ATM.Gameplay
{
    internal sealed class Gold
    {
        public long Balance { get; private set; }

        public void CreditEnemyDefeat(int wave)
        {
            double amount = 10d * Math.Pow(1.2d, wave - 1);
            long reward = amount >= long.MaxValue ? long.MaxValue : (long)Math.Floor(amount);
            Balance = Math.Min(Balance, long.MaxValue - reward) + reward;
        }

        public bool TrySpend(long amount)
        {
            if (amount < 0 || Balance < amount)
                return false;

            Balance -= amount;
            return true;
        }
    }
}
