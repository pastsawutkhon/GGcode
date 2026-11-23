using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ATM.Gameplay
{
    public sealed class BossEnemy : Enemy
    {
        private enum Move { Pursue, Warn, Charge, Volley, Stomp, Recover }
        [SerializeField, Min(1)] private float chargeSpeed = 7f;
        [SerializeField, Min(.1f)] private float chargeRange = 5f;
        [SerializeField, Min(.1f)] private float warningTime = .65f;
        [SerializeField] private Bullet orb;
        [FormerlySerializedAs("shockwave"), SerializeField] private LineRenderer groundCrack;
        [SerializeField, Min(1f)] private float stompRadius = 6.5f;
        [SerializeField, Min(.1f)] private float stompDuration = 1.45f;
        [SerializeField, Min(0f)] private float pullSpeed = 3.2f;
        private readonly List<RaycastHit2D> obstacles = new(8);
        private readonly ContactFilter2D solids = new() { useTriggers = false };
        private PlayerController runner;
        private SmoothCameraFollow followCamera;
        private Vector3 artScale;
        private Vector2 chargeStart;
        private bool backingOut;
        private Move move, pattern;
        private int nextPattern;
        private float timer;
        private Vector2 heading;
        private SpriteRenderer visual;
        private bool stompHit;

        protected override CharacterStats CreateDefaultStats() => new(230,1.6f,.85f,20,3,12,1);
        protected override void Awake()
        {
            base.Awake();
            visual = transform.Find("Character Art").GetComponent<SpriteRenderer>();
            artScale = visual.transform.localScale;
            groundCrack.positionCount = 13;
            groundCrack.loop = false;
            if (Camera.main != null) Camera.main.TryGetComponent(out followCamera);
        }

        protected override void Act()
        {
            Vector2 offset = (Vector2)TargetHitbox.bounds.center - Body.position;
            timer -= Time.fixedDeltaTime;
            switch (move)
            {
                case Move.Pursue:
                    if (timer > 0 || !HasClearApproach())
                    {
                        ChaseAndAttack();
                        return;
                    }
                    pattern = nextPattern switch { 0 => Move.Charge, 1 => Move.Volley, _ => Move.Stomp };
                    heading = offset.normalized;
                    if (pattern == Move.Charge && (offset.magnitude > chargeRange || Blocked(heading, chargeRange + .28f)))
                        pattern = Move.Volley;
                    if (pattern == Move.Stomp && offset.magnitude > stompRadius)
                    {
                        MoveTowardTarget();
                        return;
                    }
                    nextPattern = pattern == Move.Volley ? 2 : (nextPattern + 1) % 3;
                    chargeStart = Body.position;
                    Body.linearVelocity = Vector2.zero;
                    move = Move.Warn;
                    timer = warningTime;
                    visual.color = pattern == Move.Charge ? new Color(1,.64f,.38f) : new Color(1,.36f,.35f);
                    if (pattern == Move.Stomp) runner = Target.GetComponent<PlayerController>();
                    break;
                case Move.Warn:
                    if (pattern == Move.Stomp)
                    {
                        float crouch = 1f - Mathf.Clamp01(timer / warningTime);
                        visual.transform.localScale = Vector3.Scale(artScale, new Vector3(1f + .13f * crouch, 1f - .23f * crouch, 1));
                        DrawCrack(.35f + .18f * crouch, .045f, new Color(1f,.53f,.22f,.75f));
                    }
                    if (timer > 0) return;
                    visual.transform.localScale = artScale;
                    visual.color = Color.white;
                    move = pattern;
                    timer = pattern == Move.Charge ? chargeRange / chargeSpeed : pattern == Move.Volley ? .45f : stompDuration;
                    if (pattern == Move.Volley) FireCircle(0);
                    stompHit = false;
                    break;
                case Move.Charge:
                    if (Hitbox.Distance(TargetHitbox).distance < .15f)
                    {
                        Target.TakeDamage(Stats.AttackDamage);
                        Rest();
                    }
                    else if (timer <= 0) Rest();
                    else if (Blocked(heading, chargeSpeed * Time.fixedDeltaTime + .28f))
                    {
                        Rest();
                        backingOut = true;
                    }
                    else Body.linearVelocity = heading * chargeSpeed;
                    break;
                case Move.Volley:
                    if (timer <= 0) { FireCircle(15); Rest(); }
                    break;
                case Move.Stomp:
                    float elapsed = 1f - Mathf.Clamp01(timer / stompDuration);
                    float impact = 1f - elapsed;
                    DrawCrack(1f + .2f * elapsed, .045f + .08f * impact, new Color(1f,.38f,.12f,impact));
                    float distance = offset.magnitude;
                    if (distance <= stompRadius && HasClearApproach())
                    {
                        float strength = Mathf.Clamp01(1.35f - distance / stompRadius);
                        if (followCamera != null) followCamera.Tremor(strength * impact);
                        if (runner != null) runner.Pull(-offset.normalized * (pullSpeed * strength));
                        if (!stompHit && elapsed > .62f && Hitbox.Distance(TargetHitbox).distance < .45f)
                        {
                            stompHit = true;
                            Target.TakeDamage(Stats.AttackDamage);
                        }
                    }
                    if (timer <= 0) Rest();
                    break;
                case Move.Recover:
                    if (backingOut)
                    {
                        Vector2 retreat = chargeStart - Body.position;
                        float step = Mathf.Min(retreat.magnitude, Stats.MoveSpeed * Time.fixedDeltaTime);
                        backingOut = step > .01f && !Blocked(retreat.normalized, step);
                        Body.linearVelocity = backingOut ? retreat.normalized * (step / Time.fixedDeltaTime) : Vector2.zero;
                    }
                    if (timer <= 0) { move = Move.Pursue; timer = 1.8f; }
                    break;
            }
        }

        private void FireCircle(float offset)
        {
            for (int i = 0; i < 12; i++)
            {
                Quaternion angle = Quaternion.Euler(0,0,offset + i * 30);
                Vector2 direction = angle * Vector3.right;
                var shot = Bullet.Rent(orb, Body.position, angle);
                shot.Launch(this, direction, 2.65f, 13f, Stats.AttackDamage * .6f);
            }
        }

        private bool Blocked(Vector2 direction, float distance)
        {
            Hitbox.Cast(direction, solids, obstacles, distance);
            foreach (var hit in obstacles)
                if (hit.rigidbody == null || hit.rigidbody.bodyType == RigidbodyType2D.Static)
                    return true;
            return false;
        }

        private void DrawCrack(float extent, float width, Color color)
        {
            groundCrack.enabled = true;
            groundCrack.startWidth = groundCrack.endWidth = width;
            groundCrack.startColor = groundCrack.endColor = color;
            Vector2 foot = new Vector2(Hitbox.bounds.center.x, Hitbox.bounds.min.y);
            for (int i = 0; i < groundCrack.positionCount; i++)
            {
                float x = i - 6;
                float y = Mathf.Sin(i * 2.3f) * .27f - .11f * Mathf.Abs(x);
                groundCrack.SetPosition(i, foot + new Vector2(x * .47f, y) * extent);
            }
        }

        protected override void OnDeath()
        {
            groundCrack.enabled = false;
            visual.transform.localScale = artScale;
            base.OnDeath();
        }

        private void Rest()
        {
            Body.linearVelocity = Vector2.zero;
            groundCrack.enabled = false;
            backingOut = false;
            move = Move.Recover;
            timer = .85f;
        }
    }
}
