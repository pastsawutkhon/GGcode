using System;
using UnityEngine;

namespace ATM.Gameplay
{
    public abstract class Character : MonoBehaviour
    {
        [SerializeField] private CharacterStats stats;

        private HitFlash flash;
        private double protectedUntil;
        internal void Protect(float seconds) => protectedUntil = Time.timeAsDouble + seconds;

        public CharacterStats Stats => stats;
        internal void RestoreStats(CharacterStats saved) => stats = saved;
        public bool IsAlive => stats.CurrentHP > 0f;
        public event Action<Character> Died;

        protected void Reset() => stats = CreateDefaultStats();

        protected virtual void Awake()
        {
            flash = GetComponent<HitFlash>();
            stats ??= CreateDefaultStats();
            stats.Initialize();
        }

        public float TakeDamage(float damage)
        {
            if (!IsAlive || Time.timeAsDouble < protectedUntil)
                return 0f;

            float barrier = stats.Barrier;
            float applied = stats.ApplyDamage(damage);
            if (applied > 0 || barrier > stats.Barrier) flash?.Pulse();
            if (applied > 0 && this is Player && Camera.main != null && Camera.main.TryGetComponent(out SmoothCameraFollow camera))
                camera.Pulse();
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
