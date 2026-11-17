using UnityEngine;

namespace ATM.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class Player : Character
    {
        [SerializeField] private Weapon weapon;

        protected override CharacterStats CreateDefaultStats() =>
            new(maxHP: 100f, moveSpeed: 5f, attackSpeed: 20f, attackDamage: 12f,
                defense: 0f, projectileRange: 3f, projectileCount: 1);

        public bool TryFire(Vector2 direction, Vector2? aimPoint = null)
        {
            return IsAlive && weapon != null && weapon.TryFire(this, direction, aimPoint);
        }

        protected override void OnDeath()
        {
            if (TryGetComponent(out Rigidbody2D body))
                body.linearVelocity = Vector2.zero;
        }
    }
}
