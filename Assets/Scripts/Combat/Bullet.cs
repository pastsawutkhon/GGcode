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

        private Stack<Bullet> inactive;
        [SerializeField] private ImpactBurst impactBurst;
        [SerializeField, Min(.1f)] private float splashRadius = 1.25f;
        private readonly HashSet<Character> struck = new();
        private static readonly List<Collider2D> nearby = new();
        private SpriteRenderer sprite;
        private int pierce;
        private float splash;
        private static readonly HashSet<Enemy> splashTargets = new();
        private Character owner;

        private void Awake() => sprite = GetComponent<SpriteRenderer>();

        // Each weapon prefab keeps its own stack. Scene unloading also destroys
        // its inactive shots, which are discarded on the next rental.
        public static Bullet Rent(Bullet prefab, Vector3 position, Quaternion rotation)
        {
            prefab.inactive ??= new Stack<Bullet>();
            while (prefab.inactive.Count > 0)
            {
                Bullet shot = prefab.inactive.Pop();
                if (shot == null)
                    continue;
                shot.transform.SetPositionAndRotation(position, rotation);
                shot.gameObject.SetActive(true);
                return shot;
            }
            Bullet fresh = Instantiate(prefab, position, rotation);
            fresh.inactive = prefab.inactive;
            return fresh;
        }
        private bool firedByEnemy;
        private Vector2 direction;
        private float speed;
        private float remainingRange;
        private float damage;
        private Vector3 localBackward;
        private float tracerDistance;

        public void Launch(Character owner, Vector2 direction, float speed, float range, float damage)
        {
            struck.Clear();
            pierce = owner.Stats.Pierce;
            splash = owner.Stats.Splash;
            sprite.enabled = true;
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
            while (distance > 0 && TryGetImpact(distance, out Character target, out float contact))
            {
                transform.position += (Vector3)(direction * contact);
                remainingRange -= contact;
                distance -= contact;
                if (target != null)
                {
                    struck.Add(target);
                    Vector2 center = target.transform.position;
                    target.TakeDamage(damage);
                    if (splash > 0 && target is Enemy)
                    {
                        Explode(center, target);
                        splash = 0;
                    }
                    if (pierce > 0)
                    {
                        pierce--;
                        continue;
                    }
                }
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
                    (candidate == owner || struck.Contains(candidate) || !candidate.IsAlive || (firedByEnemy && candidate is Enemy)))
                    continue;

                target = candidate;
                contact = hit.distance;
                found = true;
            }
            return found;
        }

        private void Explode(Vector2 center, Character primary)
        {
            if (impactBurst != null) impactBurst.Show(center, splashRadius);
            splashTargets.Clear();
            Physics2D.OverlapCircle(center, splashRadius, SolidColliders, nearby);
            foreach (var collider in nearby)
            {
                var enemy = collider.GetComponentInParent<Enemy>();
                if (enemy != null && enemy != primary && enemy.IsAlive && splashTargets.Add(enemy))
                    enemy.TakeDamage(damage * splash);
            }
        }

        public void Despawn()
        {
            if (!gameObject.activeSelf)
                return;
            if (inactive == null || inactive.Count >= 128)
            {
                Destroy(gameObject);
                return;
            }
            owner = null;
            if (tracer != null)
                tracer.enabled = false;
            gameObject.SetActive(false);
            inactive.Push(this);
        }
    }
}
