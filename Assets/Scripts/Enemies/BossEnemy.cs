using UnityEngine;

namespace ATM.Gameplay
{
    public sealed class BossEnemy : Enemy
    {
        private enum Move { Pursue, Warn, Charge, Volley, Wave, Recover }
        [SerializeField, Min(1)] private float chargeSpeed = 7f;
        [SerializeField, Min(.1f)] private float chargeRange = 5f;
        [SerializeField, Min(.1f)] private float warningTime = .65f;
        [SerializeField] private Bullet orb;
        [SerializeField] private LineRenderer shockwave;
        private Move move, pattern;
        private int nextPattern;
        private float timer, gapAngle;
        private Vector2 heading;
        private SpriteRenderer visual;
        private bool waveHit;

        protected override CharacterStats CreateDefaultStats() => new(230,1.6f,.85f,20,3,12,1);
        protected override void Awake()
        {
            base.Awake();
            visual = transform.Find("Character Art").GetComponent<SpriteRenderer>();
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
                    pattern = nextPattern switch { 0 => Move.Charge, 1 => Move.Volley, _ => Move.Wave };
                    if (pattern == Move.Charge && offset.magnitude > chargeRange) pattern = Move.Volley;
                    nextPattern = pattern == Move.Volley ? 2 : (nextPattern + 1) % 3;
                    heading = offset.normalized;
                    gapAngle = Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg + 30;
                    Body.linearVelocity = Vector2.zero;
                    move = Move.Warn;
                    timer = warningTime;
                    visual.color = pattern == Move.Charge ? new Color(1,.64f,.38f) : new Color(1,.36f,.35f);
                    if (pattern == Move.Wave) DrawWave(.9f, .045f);
                    break;
                case Move.Warn:
                    if (timer > 0) return;
                    visual.color = Color.white;
                    move = pattern;
                    timer = pattern == Move.Charge ? chargeRange / chargeSpeed : pattern == Move.Volley ? .45f : 1.25f;
                    if (pattern == Move.Volley) FireCircle(0);
                    waveHit = false;
                    break;
                case Move.Charge:
                    Body.linearVelocity = heading * chargeSpeed;
                    if (Hitbox.Distance(TargetHitbox).distance < .15f)
                    {
                        Target.TakeDamage(Stats.AttackDamage);
                        Rest();
                    }
                    else if (timer <= 0) Rest();
                    break;
                case Move.Volley:
                    if (timer <= 0) { FireCircle(15); Rest(); }
                    break;
                case Move.Wave:
                    float radius = .9f + (1.25f - timer) * 4.3f;
                    DrawWave(radius,.11f);
                    float angle = Mathf.Atan2(offset.y,offset.x) * Mathf.Rad2Deg;
                    if (!waveHit && Mathf.Abs(offset.magnitude-radius) < .38f &&
                        Mathf.Abs(Mathf.DeltaAngle(gapAngle,angle)) > 35 && HasClearApproach())
                    {
                        Target.TakeDamage(Stats.AttackDamage);
                        waveHit = true;
                    }
                    if (timer <= 0) Rest();
                    break;
                case Move.Recover:
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

        private void DrawWave(float radius, float width)
        {
            shockwave.enabled = true;
            shockwave.startWidth = shockwave.endWidth = width;
            for (int i = 0; i < shockwave.positionCount; i++)
            {
                float a = Mathf.Deg2Rad * (gapAngle + 35 + 290f * i / (shockwave.positionCount - 1));
                shockwave.SetPosition(i, Body.position + radius * new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
            }
        }

        protected override void OnDeath()
        {
            shockwave.enabled = false;
            base.OnDeath();
        }

        private void Rest()
        {
            Body.linearVelocity = Vector2.zero;
            shockwave.enabled = false;
            move = Move.Recover;
            timer = .85f;
        }
    }
}
