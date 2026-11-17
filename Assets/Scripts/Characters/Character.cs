using System;
using UnityEngine;

namespace ATM.Gameplay
{
    public abstract class Character : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;

        public CharacterStats Stats => stats;
        public bool IsAlive => stats.CurrentHP > 0f;
        public event Action<Character> Died;

        protected void Reset() => stats = CreateDefaultStats();

        protected virtual void Awake()
        {
            stats ??= CreateDefaultStats();
            stats.Initialize();
        }

        public float TakeDamage(float damage)
        {
            if (!IsAlive)
                return 0f;

            float applied = stats.ApplyDamage(damage);
            if (!IsAlive)
            {
                OnDeath();
                Died?.Invoke(this);
            }
            return applied;
        }

        public void Heal(float amount)
        {
            if (IsAlive)
                stats.Heal(amount);
        }

        protected abstract CharacterStats CreateDefaultStats();
        protected abstract void OnDeath();
    }
}
