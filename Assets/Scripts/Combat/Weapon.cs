using UnityEngine;

namespace ATM.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class Weapon : MonoBehaviour
    {
        [SerializeField] private Transform firePoint1;
        [SerializeField] private Transform firePoint2;
        [SerializeField] private SpriteRenderer muzzleFlash1;
        [SerializeField] private SpriteRenderer muzzleFlash2;
        [SerializeField, Min(0)] private float muzzleFlashDuration = .045f;
        [SerializeField] private Bullet bulletPrefab;
        [SerializeField, Min(.1f)] private float projectileSpeed = 30f;
        [SerializeField, Range(0f, 180f), Tooltip("Total width of the random spread, centered on the aim direction.")]
        private float spreadAngle = 50f;

        private double nextShotTime = double.NegativeInfinity;
        private double flash1Until, flash2Until;
        private bool useSecondFirePoint;

        private void Update()
        {
            double now = Time.timeAsDouble;
            if (muzzleFlash1 != null && muzzleFlash1.enabled && now >= flash1Until)
                muzzleFlash1.enabled = false;
            if (muzzleFlash2 != null && muzzleFlash2.enabled && now >= flash2Until)
                muzzleFlash2.enabled = false;
        }

        public bool TryFire(Character owner, Vector2 direction, Vector2? aimPoint = null)
        {
            if (!isActiveAndEnabled || owner == null || !owner.IsAlive || bulletPrefab == null ||
                firePoint1 == null || direction.sqrMagnitude < .0001f)
                return false;

            CharacterStats stats = owner.Stats;
            if (stats.AttackSpeed <= 0f || stats.ProjectileCount < 1 || stats.ProjectileRange <= 0f)
                return false;

            double now = Time.timeAsDouble;
            if (now < nextShotTime)
                return false;

            Transform muzzle = useSecondFirePoint && firePoint2 != null ? firePoint2 : firePoint1;
            Vector2 forward = direction.normalized;
            if (aimPoint.HasValue)
            {
                Vector2 fromMuzzle = aimPoint.Value - (Vector2)muzzle.position;
                // Keep close clicks from bending the shot back toward the weapon.
                if (Vector2.Dot(fromMuzzle, forward) > .25f)
                    forward = fromMuzzle.normalized;
            }
            FireVolley(owner, muzzle.position, forward, stats);
            if (muzzle == firePoint2)
            {
                if (muzzleFlash2 != null)
                    muzzleFlash2.enabled = muzzleFlashDuration > 0;
                flash2Until = now + muzzleFlashDuration;
            }
            else
            {
                if (muzzleFlash1 != null)
                    muzzleFlash1.enabled = muzzleFlashDuration > 0;
                flash1Until = now + muzzleFlashDuration;
            }
            useSecondFirePoint = !useSecondFirePoint;

            double interval = 1d / stats.AttackSpeed;
            // Carry sub-frame timing forward within a frame; discard older missed shots.
            double scheduledNextShot = nextShotTime + interval;
            nextShotTime = scheduledNextShot > now ? scheduledNextShot : now + interval;
            return true;
        }

        private void FireVolley(Character owner, Vector3 origin, Vector2 direction, CharacterStats stats)
        {
            float aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int count = stats.ProjectileCount;
            float sectorWidth = spreadAngle / count;
            float leftEdge = -.5f * spreadAngle;
            for (int i = 0; i < count; i++)
            {
                float offset = leftEdge + (i + Random.value) * sectorWidth;
                Quaternion rotation = Quaternion.AngleAxis(aimAngle + offset, Vector3.forward);
                Bullet shot = Instantiate(bulletPrefab, origin, rotation);
                shot.Launch(owner, rotation * Vector3.right, projectileSpeed, stats.ProjectileRange, stats.AttackDamage);
            }
        }
    }
}
