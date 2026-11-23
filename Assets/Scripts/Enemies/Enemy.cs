using UnityEngine;

namespace ATM.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public abstract class Enemy : Character
    {
        [SerializeField, Min(0f)] private float attackReach = .12f;
        [SerializeField, Min(.1f)] private float spawnClearance = .55f;

        private double nextAttackTime, nextRouteCheck;
        private bool direct;
        private float recoilTime;
        private Vector2 recoil;
        private ArenaRoutes routes;
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

        internal void Spawn(Player player, Collider2D playerHitbox, int wave, ArenaRoutes routes)
        {
            this.routes = routes;
            Target = player;
            TargetHitbox = playerHitbox;
            Stats.ScaleForWave(wave, this is BossEnemy);
        }

        protected void FixedUpdate()
        {
            if (Target == null || !Target.IsAlive || !IsAlive)
            {
                Body.linearVelocity = Vector2.zero;
                return;
            }

            if (recoilTime > 0)
            {
                Body.linearVelocity = recoil * (recoilTime / .12f);
                recoilTime -= Time.fixedDeltaTime;
                return;
            }
            Act();
        }

        protected abstract void Act();

        protected bool HasClearApproach()
        {
            if (Time.fixedTimeAsDouble >= nextRouteCheck)
            {
                direct = routes.Clear(Body.position, TargetHitbox.bounds.center);
                nextRouteCheck = Time.fixedTimeAsDouble + .18;
            }
            return direct;
        }

        protected void MoveTowardTarget()
        {
            Vector2 goal = TargetHitbox.bounds.center;
            if (!HasClearApproach())
                goal = routes.Waypoint(Body.position, goal);
            Vector2 direction = goal - Body.position;
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
            if (!TryStartAttack()) return;
            float barrier = Target.Stats.Barrier;
            float applied = Target.TakeDamage(Stats.AttackDamage);
            if ((this is FastEnemy || this is MeleeEnemy) && (applied > 0 || barrier > Target.Stats.Barrier))
            {
                recoilTime = .12f;
                recoil = (Body.position - (Vector2)TargetHitbox.bounds.center).normalized * (this is FastEnemy ? 6f : 4f);
                Body.linearVelocity = recoil;
            }
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
            Destroy(gameObject, .085f);
        }
    }
}
