using ATM.Waves;
using UnityEngine;

namespace ATM.Gameplay
{
    internal sealed class EnemySpawner
    {
        private readonly Transform[] edges, center;
        private readonly Enemy[] regular;
        private readonly BossEnemy[] bosses;
        private readonly Player player;
        private readonly Bounds floor;
        private readonly ArenaRoutes regularRoutes;
        private ArenaRoutes bossRoutes;
        private readonly Collider2D hitbox;
        private readonly Camera camera;
        private readonly float minimumDistanceSquared, regularRadius, bossRadius;
        private readonly Collider2D[] obstruction = new Collider2D[1];
        private readonly ContactFilter2D solids = new() { useTriggers = false };

        public EnemySpawner(Transform[] edges, Transform[] center, MeleeEnemy melee, RangedEnemy ranged,
            FastEnemy fast, BossEnemy[] bosses, Player player, Collider2D hitbox, float distance, float clearance)
        {
            this.edges = edges;
            this.center = center;
            this.bosses = bosses;
            this.player = player;
            this.hitbox = hitbox;
            floor = player.GetComponent<PlayerController>().Map.bounds;
            regularRoutes = new ArenaRoutes(floor, .48f);
            camera = Camera.main;
            regular = new Enemy[] { melee, ranged, fast };
            regularRadius = clearance;
            foreach (var prefab in regular)
                regularRadius = Mathf.Max(regularRadius, prefab.SpawnClearance);
            bossRadius = clearance;
            foreach (var prefab in bosses)
                bossRadius = Mathf.Max(bossRadius, prefab.SpawnClearance);
            minimumDistanceSquared = distance * distance;
        }

        private ArenaRoutes Routes(bool boss) => boss ? bossRoutes ??= new ArenaRoutes(floor, bossRadius) : regularRoutes;

        public bool TryFindPoint(bool boss, out Transform point)
        {
            point = null;
            int valid = 0;
            float radius = boss ? bossRadius : regularRadius;
            ArenaRoutes routes = Routes(boss);
            Vector2 target = hitbox.bounds.center;
            foreach (Transform candidate in boss ? center : edges)
            {
                if (candidate == null || !candidate.gameObject.activeInHierarchy)
                    continue;
                Vector2 position = candidate.position;
                if ((position - (Vector2)player.transform.position).sqrMagnitude < minimumDistanceSquared ||
                    (!boss && IsVisible(position, radius)) ||
                    Physics2D.OverlapCircle(position, radius, solids, obstruction) > 0 ||
                    !routes.CanReach(position, target))
                    continue;
                if (Random.Range(0, ++valid) == 0)
                    point = candidate;
            }
            return point != null;
        }

        private bool IsVisible(Vector2 position, float radius)
        {
            if (camera == null)
                return true;
            Vector3 viewport = camera.WorldToViewportPoint(position);
            float marginY = (radius + .25f) / (2f * camera.orthographicSize);
            float marginX = marginY / camera.aspect;
            return viewport.x >= -marginX && viewport.x <= 1 + marginX &&
                   viewport.y >= -marginY && viewport.y <= 1 + marginY;
        }

        public Enemy Spawn(Transform point, bool boss, EnemyType type, int wave)
        {
            Enemy prefab = boss ? bosses[Random.Range(0, bosses.Length)] : regular[(int)type];
            Enemy enemy = Object.Instantiate(prefab, point.position, point.rotation);
            enemy.Spawn(player, hitbox, wave, Routes(boss));
            return enemy;
        }
    }
}
