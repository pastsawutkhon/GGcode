using System.Collections.Generic;
using UnityEngine;

namespace ATM.Gameplay
{
    public sealed class RangedEnemy : Enemy
    {
        [SerializeField] private LineRenderer beam, glow, core;
        [SerializeField] private AudioSource chargeSound;
        [SerializeField, Min(.1f)] private float attackRange = 4f;
        [SerializeField, Min(.05f)] private float windup = .65f;
        private static readonly List<RaycastHit2D> hits = new(16);
        private static readonly ContactFilter2D solids = new() { useTriggers = false };
        private Vector2 direction;
        private float remaining;
        private bool aiming;
        protected override CharacterStats CreateDefaultStats() => new(37,1.8f,.7f,8,0,6,1);

        protected override void Act()
        {
            Vector2 origin = Hitbox.bounds.center;
            Vector2 offset = (Vector2)TargetHitbox.bounds.center - origin;
            if (!aiming && remaining <= 0)
            {
                Show(false);
                if (offset.sqrMagnitude > attackRange * attackRange || !HasClearApproach())
                {
                    MoveTowardTarget();
                    return;
                }
                Body.linearVelocity = Vector2.zero;
                if (!TryStartAttack()) return;
                aiming = true;
                remaining = windup;
                direction = offset.normalized;
                Show(true);
                chargeSound.Play();
            }
            Body.linearVelocity = Vector2.zero;
            if (aiming && remaining > .4f && offset.sqrMagnitude > .0001f)
                direction = offset.normalized;
            float distance = Stats.ProjectileRange;
            Player victim = null;
            Physics2D.Raycast(origin, direction, solids, hits, distance);
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<Enemy>() != null || hit.distance > distance) continue;
                distance = hit.distance;
                victim = hit.collider.GetComponentInParent<Player>();
            }
            remaining -= Time.fixedDeltaTime;
            if (aiming && remaining <= 0)
            {
                aiming = false;
                remaining = .16f;
                if (victim != null) victim.TakeDamage(Stats.AttackDamage);
            }
            Vector2 end = origin + direction * distance;
            float w = aiming ? .042f : .22f * Mathf.Lerp(.25f, 1f, Mathf.Clamp01(remaining / .12f));
            Draw(glow, origin, end, w * 2.1f, new Color(1,0,.02f,.26f));
            Draw(beam, origin, end, w, new Color(1,.025f,.025f,1));
            Draw(core, origin, end, aiming ? .008f : w * .52f, aiming ? new Color(1,.3f,.24f) : Color.white);
        }

        private static void Draw(LineRenderer line, Vector2 start, Vector2 end, float width, Color color)
        {
            line.SetPosition(0,start);
            line.SetPosition(1,end);
            line.startWidth = line.endWidth = width;
            line.startColor = line.endColor = color;
        }
        private void Show(bool visible) => beam.enabled = glow.enabled = core.enabled = visible;
        protected override void OnDeath()
        {
            Show(false);
            chargeSound.Stop();
            base.OnDeath();
        }

        private void LateUpdate()
        {
            if (beam.enabled && (Target == null || !Target.IsAlive))
            {
                Show(false);
                chargeSound.Stop();
            }
        }
    }
}
