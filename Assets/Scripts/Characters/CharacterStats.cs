using System;
using ATM.Waves;
using UnityEngine;

namespace ATM.Gameplay
{
    [Serializable]
    public sealed class CharacterStats
    {
        [SerializeField, Min(1)] private float maxHP;
        [SerializeField, Min(0)] private float moveSpeed;
        [SerializeField, Min(.1f)] private float attackSpeed;
        [SerializeField, Min(0)] private float attackDamage;
        [SerializeField, Min(0)] private float defense;
        [SerializeField, Min(.1f)] private float projectileRange;
        [SerializeField, Min(1)] private int projectileCount;

        public float MaxHP { get; private set; }
        public float CurrentHP { get; private set; }
        public float MoveSpeed => moveSpeed;
        private float hpPercentBonus, damageBonus, fireRateBonus, rangeBonus;

        public float AttackSpeed => attackSpeed * (1f + fireRateBonus);
        public float AttackDamage { get; private set; }
        public float Defense { get; private set; }
        public float ProjectileRange => projectileRange * (1f + rangeBonus);
        public int ProjectileCount => projectileCount;
        public float Barrier { get; private set; }
        public double Luck { get; private set; } = 100d;

        internal CharacterStats CopyForPreview() => (CharacterStats)MemberwiseClone();

        internal CharacterStats(float maxHP, float moveSpeed, float attackSpeed, float attackDamage,
            float defense, float projectileRange, int projectileCount)
        {
            this.maxHP = maxHP;
            this.moveSpeed = moveSpeed;
            this.attackSpeed = attackSpeed;
            this.attackDamage = attackDamage;
            this.defense = defense;
            this.projectileRange = projectileRange;
            this.projectileCount = projectileCount;
        }

        internal void Initialize()
        {
            MaxHP = CurrentHP = maxHP;
            AttackDamage = attackDamage;
            Defense = defense;
        }

        internal void ScaleForWave(int wave)
        {
            MaxHP = CurrentHP = DifficultyScaler.ScaleHP(maxHP, wave);
            AttackDamage = DifficultyScaler.ScaleAttack(attackDamage, wave);
            Defense = DifficultyScaler.ScaleDefense(defense, wave);
        }

        internal float ApplyDamage(float amount)
        {
            float before = CurrentHP;
            float damage = Mathf.Max(0f, amount) / (1f + Defense / 100f);
            float absorbed = Mathf.Min(Barrier, damage);
            Barrier -= absorbed;
            CurrentHP = Mathf.Max(0f, before - (damage - absorbed));
            return before - CurrentHP;
        }

        internal void IncreaseMaxHP(float percentage)
        {
            float before = MaxHP;
            hpPercentBonus += percentage;
            MaxHP = maxHP * (1f + hpPercentBonus);
            Heal(MaxHP - before);
        }

        internal void IncreaseAttack(float percentage)
        {
            damageBonus += percentage;
            AttackDamage = attackDamage * (1f + damageBonus);
        }

        internal void IncreaseFireRate(float percentage) => fireRateBonus += percentage;
        internal void IncreaseRange(float percentage) => rangeBonus += percentage;
        internal void IncreaseLuck(float amount) => Luck += amount;
        internal void AddBarrier(float amount) => Barrier += amount;

        internal void Heal(float amount)
        {
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + Mathf.Max(0f, amount));
        }
    }
}
