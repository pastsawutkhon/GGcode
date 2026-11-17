using UnityEngine;

namespace ATM.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public abstract class Enemy : Character
    {
        [SerializeField, Min(0f)] private float attackReach = .12f;
        [SerializeField, Min(.1f)] private float spawnClearance = .55f;

        private double nextAttackTime;
        protected Rigidbody2D Body { get; private set; }
        protected Collider2D Hitbox { get; private set; }
        protected Player Target { get; private set; }
        protected Collider2D TargetHitbox { get; private set; }
        internal float SpawnClearance => spawnClearance;

        protected override void Awake()
        {
            base.Awake();
            Body = GetComponent<Rigidbody2D>();
            Hitbox = GetComponent<Collider2D>();
        }

        internal void Spawn(Player player, Collider2D playerHitbox, int wave)
        {
            Target = player;
            TargetHitbox = playerHitbox;
            Stats.ScaleForWave(wave);
        }

        protected void FixedUpdate()
        {
            if (Target == null || !Target.IsAlive || !IsAlive)
            {
                Body.linearVelocity = Vector2.zero;
                return;
            }

            Act();
        }

        protected abstract void Act();

        protected void MoveTowardTarget()
        {
            Vector2 direction = (Vector2)TargetHitbox.bounds.center - Body.position;
            Body.linearVelocity = direction.normalized * Stats.MoveSpeed;
        }

        protected void ChaseAndAttack()
        {
            if (Hitbox.Distance(TargetHitbox).distance > attackReach)
            {
                MoveTowardTarget();
                return;
            }

            Body.linearVelocity = Vector2.zero;
            if (TryStartAttack())
                Target.TakeDamage(Stats.AttackDamage);
        }

        protected bool TryStartAttack()
        {
            double now = Time.fixedTimeAsDouble;
            if (Stats.AttackSpeed <= 0f || now < nextAttackTime)
                return false;

            nextAttackTime = now + 1d / Stats.AttackSpeed;
            return true;
        }

        protected override void OnDeath()
        {
            enabled = false;
            Body.simulated = false;
            Destroy(gameObject);
        }
    }
}
