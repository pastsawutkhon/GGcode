using System.Collections.Generic;
using UnityEngine;

namespace ATM.Gameplay
{
    public sealed class Bullet : MonoBehaviour
    {
        private static readonly List<RaycastHit2D> Hits = new(8);
        private static readonly ContactFilter2D SolidColliders = new() { useTriggers = false };

        [SerializeField] private LineRenderer tracer;
        [SerializeField, Min(0)] private float tracerLength = .6f;

        private Character owner;
        private bool firedByEnemy;
        private Vector2 direction;
        private float speed;
        private float remainingRange;
        private float damage;
        private Vector3 localBackward;
        private float tracerDistance;

        public void Launch(Character owner, Vector2 direction, float speed, float range, float damage)
        {
            this.owner = owner;
            firedByEnemy = owner is Enemy;
            this.direction = direction;
            this.speed = speed;
            this.damage = damage;
            remainingRange = range;
            if (tracer != null)
            {
                tracerDistance = 0;
                tracer.enabled = false;
                localBackward = transform.InverseTransformVector(-(Vector3)direction);
            }
        }

        private void Update()
        {
            float distance = Mathf.Min(remainingRange, speed * Time.deltaTime);
            if (distance > 0 && TryGetImpact(distance, out Character target, out float contact))
            {
                transform.position += (Vector3)(direction * contact);
                if (target != null)
                    target.TakeDamage(damage);
                Despawn();
                return;
            }

            transform.position += (Vector3)(direction * distance);
            remainingRange -= distance;
            if (remainingRange <= 0)
            {
                Despawn();
                return;
            }

            if (tracer != null && tracerDistance < tracerLength && distance > 0)
            {
                tracerDistance = Mathf.Min(tracerLength, tracerDistance + distance);
                int segments = Mathf.Max(1, tracer.positionCount - 1);
                for (int i = 0; i < tracer.positionCount; i++)
                    tracer.SetPosition(i, localBackward * (tracerDistance * i / segments));
                tracer.enabled = true;
            }
        }

        private bool TryGetImpact(float distance, out Character target, out float contact)
        {
            Physics2D.Raycast(transform.position, direction, SolidColliders, Hits, distance);
            bool found = false;
            target = null;
            contact = distance;
            foreach (RaycastHit2D hit in Hits)
            {
                if (hit.distance > contact)
                    continue;

                Character candidate = hit.collider.GetComponentInParent<Character>();
                if (candidate != null &&
                    (candidate == owner || !candidate.IsAlive || (firedByEnemy && candidate is Enemy)))
                    continue;

                target = candidate;
                contact = hit.distance;
                found = true;
            }
            return found;
        }

        public void Despawn()
        {
            enabled = false;
            Destroy(gameObject);
        }
    }
}
