using ATM.Waves;
using UnityEngine;

namespace ATM.Gameplay
{
    internal sealed class EnemySpawner
    {
        private readonly Transform[] spawnPoints;
        private readonly Enemy[] regularPrefabs;
        private readonly BossEnemy[] bossPrefabs;
        private readonly Player player;
        private readonly Collider2D playerHitbox;
        private readonly float minimumDistanceSquared;
        private readonly float clearance;
        private readonly Collider2D[] obstruction = new Collider2D[1];
        private readonly ContactFilter2D solidColliders = new() { useTriggers = false };

        public EnemySpawner(Transform[] spawnPoints, MeleeEnemy melee, RangedEnemy ranged,
            FastEnemy fast, BossEnemy[] bossPrefabs, Player player, Collider2D playerHitbox,
            float minimumDistance, float clearance)
        {
            this.spawnPoints = spawnPoints;
            regularPrefabs = new Enemy[] { melee, ranged, fast };
            this.bossPrefabs = bossPrefabs;
            this.player = player;
            this.playerHitbox = playerHitbox;
            minimumDistanceSquared = minimumDistance * minimumDistance;
            this.clearance = clearance;
        }

        public Enemy GetRegularPrefab(EnemyType type) => regularPrefabs[(int)type];

        public BossEnemy PickBossPrefab() => bossPrefabs[Random.Range(0, bossPrefabs.Length)];

        public bool TrySpawn(Enemy prefab, int wave, out Enemy enemy)
        {
            float radius = Mathf.Max(clearance, prefab.SpawnClearance);
            int start = Random.Range(0, spawnPoints.Length);
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform point = spawnPoints[(start + i) % spawnPoints.Length];
                if (point == null)
                    continue;

                Vector2 offset = (Vector2)(point.position - player.transform.position);
                if (offset.sqrMagnitude < minimumDistanceSquared ||
                    Physics2D.OverlapCircle(point.position, radius, solidColliders, obstruction) > 0)
                    continue;

                enemy = Object.Instantiate(prefab, point.position, point.rotation);
                enemy.Spawn(player, playerHitbox, wave);
                return true;
            }

            enemy = null;
            return false;
        }
    }
}
