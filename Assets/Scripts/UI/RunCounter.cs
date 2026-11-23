using System;
using UnityEngine;

namespace ATM.Gameplay
{
    internal sealed class RunCounter
    {
        private long start, target;
        private float elapsed = 1;
        public long Value { get; private set; }

        public void Set(long amount)
        {
            if (target == amount) return;
            start = Value;
            target = amount;
            elapsed = 0;
        }
        public bool Tick(float dt)
        {
            if (elapsed >= 1) return false;
            elapsed = Mathf.Min(1, elapsed + dt / .42f);
            long previous = Value;
            decimal t = (decimal)Mathf.SmoothStep(0, 1, elapsed);
            Value = elapsed == 1 ? target : (long)(start + ((decimal)target - start) * t);
            return previous != Value;
        }
    }
}
