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
        public float MoveSpeed => moveSpeed * (1f + movementBonus);
        private float hpPercentBonus, damageBonus, fireRateBonus, rangeBonus, movementBonus;

        public float AttackSpeed => attackSpeed * (1f + fireRateBonus);
        public float AttackDamage { get; private set; }
        public float Defense { get; private set; }
        public float ProjectileRange => projectileRange * (1f + rangeBonus);
        public int ProjectileCount => projectileCount;
        public int Pierce { get; private set; }
        public float Splash { get; private set; }
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

        internal void ApplyBaseTraining(int vitality, int power, int armor)
        {
            maxHP = Trained(maxHP, vitality);
            attackDamage = Trained(attackDamage, power);
            defense = Trained(defense + 10f, armor) - 10f;
            Initialize();
        }

        internal static float Trained(float value, int level) =>
            (float)Math.Min(float.MaxValue, value * Math.Pow(1.10d, level));

        public int DashCapacity { get; private set; }
        public int DashReady { get; private set; }
        public float DashRecharge { get; private set; }
        public float DashProtection { get; private set; }
        internal void AddDash() { DashCapacity++; DashReady++; }
        internal void ExtendDash() => DashProtection += .3f;

        internal bool UseDash()
        {
            if (DashReady == 0) return false;
            if (DashReady == DashCapacity) DashRecharge = 10f;
            DashReady--;
            return true;
        }

        internal void TickDash(float dt)
        {
            if (DashReady >= DashCapacity) return;
            DashRecharge -= dt;
            if (DashRecharge > 0f) return;
            DashReady++;
            DashRecharge = DashReady < DashCapacity ? 10f : 0f;
        }

        internal void ScaleForWave(int wave, bool isBoss)
        {
            MaxHP = CurrentHP = isBoss ? DifficultyScaler.GetBossHP(wave) : DifficultyScaler.ScaleHP(maxHP, wave);
            AttackDamage = isBoss ? DifficultyScaler.GetBossAttack(wave) : DifficultyScaler.ScaleAttack(attackDamage, wave);
            Defense = isBoss ? wave : DifficultyScaler.ScaleDefense(defense, wave);
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
            MaxHP = (float)Math.Min(float.MaxValue, maxHP * (1d + hpPercentBonus));
            Heal(MaxHP - before);
        }

        internal void IncreaseAttack(float percentage)
        {
            damageBonus += percentage;
            AttackDamage = (float)Math.Min(float.MaxValue, attackDamage * (1d + damageBonus));
        }

        internal void IncreaseFireRate(float percentage) => fireRateBonus += percentage;
        internal void AddProjectiles(int count) => projectileCount += count;
        internal void AddPierce(int count) => Pierce += count;
        internal void AddSplash(float fraction) => Splash += fraction;
        internal void IncreaseMovement(float percentage) => movementBonus += percentage;
        internal void IncreaseRange(float percentage) => rangeBonus += percentage;
        internal void IncreaseLuck(float amount) => Luck += amount;
        internal void AddBarrier(float amount) => Barrier += amount;

        internal void Heal(float amount)
        {
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + Mathf.Max(0f, amount));
        }
    }
}
