using UnityEngine;

namespace ATM.Gameplay
{
    public sealed class RangedEnemy : Enemy
    {
        [SerializeField] private Bullet projectilePrefab;
        [SerializeField, Min(.1f)] private float attackRange = 4f;
        [SerializeField, Min(.1f)] private float projectileSpeed = 7f;

        protected override CharacterStats CreateDefaultStats() =>
            new(maxHP: 37f, moveSpeed: 1.8f, attackSpeed: .7f, attackDamage: 8f,
                defense: 0f, projectileRange: 6f, projectileCount: 1);

        protected override void Act()
        {
            Vector2 origin = Hitbox.bounds.center;
            Vector2 offset = (Vector2)TargetHitbox.bounds.center - origin;
            if (offset.sqrMagnitude > attackRange * attackRange)
            {
                MoveTowardTarget();
                return;
            }

            Body.linearVelocity = Vector2.zero;
            if (projectilePrefab == null || offset.sqrMagnitude < .0001f || !TryStartAttack())
                return;

            Vector2 direction = offset.normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector3 position = new(origin.x, origin.y, transform.position.z);
            Bullet shot = Instantiate(projectilePrefab, position, Quaternion.Euler(0f, 0f, angle));
            shot.Launch(this, direction, projectileSpeed, Stats.ProjectileRange, Stats.AttackDamage);
        }
    }
}
