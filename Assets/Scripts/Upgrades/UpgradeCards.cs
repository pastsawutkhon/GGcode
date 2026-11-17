using UnityEngine;

namespace ATM.Gameplay
{
    internal enum UpgradeKind { Heal, MaxHPPercent, Attack, FireRate, Range, Luck }

    internal readonly struct UpgradeCard
    {
        private static readonly string[] Names = { "HEAL", "MAX HP %", "ATK", "FIRE RATE", "RANGE", "LUCK" };
        private static readonly string[] Numerals = { "I", "II", "III", "IV", "V" };
        private static readonly int[,] Values =
        {
            { 20, 40, 70, 90, 100 },
            {  5, 10, 15, 20, 25 },
            { 10, 15, 20, 25, 30 },
            { 10, 20, 30, 40, 50 },
            {  5, 10, 15, 20, 25 },
            {  5, 10, 15, 20, 25 }
        };

        public UpgradeKind Kind { get; }
        public UpgradeRarity Rarity { get; }
        private int Tier => (int)UpgradeRarity.Common - (int)Rarity;
        private int Amount => Values[(int)Kind, Tier];
        private bool GrantsBarrier => Kind == UpgradeKind.Heal && Rarity == UpgradeRarity.Legendary;
        public string Title => Names[(int)Kind];
        public string Numeral => Numerals[Tier];
        public string EffectText => Kind switch
        {
            UpgradeKind.Heal => GrantsBarrier ? "Healing 100%\n+ Barrier 25% of Max HP" : $"Healing {Amount}% of Max HP",
            UpgradeKind.Luck => $"+{Amount}",
            _ => $"+{Amount}%"
        };

        public UpgradeCard(UpgradeKind kind, UpgradeRarity rarity)
        {
            Kind = kind;
            Rarity = rarity;
        }

        public void Apply(CharacterStats stats)
        {
            float percentage = Amount / 100f;
            switch (Kind)
            {
                case UpgradeKind.Heal:
                    stats.Heal(stats.MaxHP * percentage);
                    if (GrantsBarrier)
                        stats.AddBarrier(stats.MaxHP * .25f);
                    break;
                case UpgradeKind.MaxHPPercent: stats.IncreaseMaxHP(percentage); break;
                case UpgradeKind.Attack: stats.IncreaseAttack(percentage); break;
                case UpgradeKind.FireRate: stats.IncreaseFireRate(percentage); break;
                case UpgradeKind.Range: stats.IncreaseRange(percentage); break;
                case UpgradeKind.Luck: stats.IncreaseLuck(Amount); break;
            }
        }

        public string BuildPreview(CharacterStats before)
        {
            CharacterStats after = before.CopyForPreview();
            Apply(after);
            return Kind switch
            {
                UpgradeKind.Heal => $"Current HP {before.CurrentHP / before.MaxHP:P0} → {after.CurrentHP / after.MaxHP:P0}" +
                    (GrantsBarrier ? $"\nBarrier {before.Barrier:0.##} → {after.Barrier:0.##}" : ""),
                UpgradeKind.MaxHPPercent => $"Max HP {before.MaxHP:0.##} → {after.MaxHP:0.##}",
                UpgradeKind.Attack => $"ATK {before.AttackDamage:0.##} → {after.AttackDamage:0.##}",
                UpgradeKind.FireRate => $"Fire Rate {before.AttackSpeed:0.##} → {after.AttackSpeed:0.##}",
                UpgradeKind.Range => $"Range {before.ProjectileRange:0.##} → {after.ProjectileRange:0.##}",
                _ => $"Luck {before.Luck:0.##} → {after.Luck:0.##}"
            };
        }
    }

    internal sealed class UpgradeCards
    {
        private static readonly UpgradeKind[] Kinds = (UpgradeKind[])System.Enum.GetValues(typeof(UpgradeKind));
        private int successfulRerolls;

        public long RerollCost
        {
            get
            {
                if (successfulRerolls == 0)
                    return 0;

                double cost = 10d * System.Math.Pow(1.2d, successfulRerolls - 1);
                return cost >= long.MaxValue ? long.MaxValue : (long)System.Math.Floor(cost);
            }
        }

        public UpgradeCard[] Draw(int wave, double luck)
        {
            double[] percentages = UpgradeRarityCalculator.GetPercentages(wave, luck);
            var choices = new UpgradeCard[3];
            var available = (UpgradeKind[])Kinds.Clone();
            for (int i = 0; i < choices.Length; i++)
            {
                int index = Random.Range(i, available.Length);
                (available[i], available[index]) = (available[index], available[i]);
                choices[i] = new UpgradeCard(available[i], UpgradeRarityCalculator.Roll(percentages));
            }
            return choices;
        }

        public bool TryReroll(int wave, double luck, Gold gold, out UpgradeCard[] offer)
        {
            offer = null;
            long cost = RerollCost;
            if (gold.Balance < cost)
                return false;

            UpgradeCard[] replacement = Draw(wave, luck);
            if (!gold.TrySpend(cost))
                return false;

            if (successfulRerolls < int.MaxValue)
                successfulRerolls++;
            offer = replacement;
            return true;
        }
    }
}
