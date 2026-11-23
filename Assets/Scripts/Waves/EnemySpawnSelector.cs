using System;

namespace ATM.Waves
{
    public enum EnemyType { Melee, Ranged, Fast }

    // The periodic reference order fixes rounding across waves. Within each short
    // group we draw without replacement, so enemies need not march in a fixed order.
    public sealed class EnemySpawnSelector
    {
        private const int GroupSize = 4;
        private int[] previousWeights;
        private readonly int[] credit = new int[3];
        private readonly int[] remaining = new int[3];
        private EnemyType[] slots = Array.Empty<EnemyType>();
        private int cursor;

        public int Remaining(EnemyType type) => remaining[(int)type];
        public int DrawRange => Math.Min(GroupSize - cursor % GroupSize, slots.Length - cursor);

        public void Begin(int[] weights, int count)
        {
            if (count <= 0 || weights.Length != 3 || weights[0] < 0 || weights[1] < 0 ||
                weights[2] < 0 || (long)weights[0] + weights[1] + weights[2] != 100)
                throw new ArgumentException("Expected three percentages totaling 100 and a positive count.");
            if (previousWeights == null || !previousWeights.AsSpan().SequenceEqual(weights))
                Array.Clear(credit, 0, credit.Length);
            previousWeights = (int[])weights.Clone();
            Array.Clear(remaining, 0, remaining.Length);
            slots = new EnemyType[count];
            cursor = 0;
            for (int i = 0; i < count; i++)
            {
                int next = 0;
                for (int type = 0; type < weights.Length; type++)
                {
                    credit[type] += weights[type];
                    if (credit[type] > credit[next])
                        next = type;
                }
                credit[next] -= 100;
                slots[i] = (EnemyType)next;
                remaining[next]++;
            }
        }

        public EnemyType Peek(int roll)
        {
            if (roll < 0 || roll >= DrawRange)
                throw new ArgumentOutOfRangeException(nameof(roll));
            return slots[cursor + roll];
        }

        public void Commit(int roll)
        {
            EnemyType picked = Peek(roll);
            slots[cursor + roll] = slots[cursor];
            remaining[(int)picked]--;
            cursor++;
        }
    }
}
