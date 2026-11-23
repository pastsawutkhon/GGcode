using System.Collections.Generic;
using UnityEngine;

namespace ATM.Gameplay
{
    // All pursuers of similar size share a route field. The walls are sampled
    // once per room and the field changes only when the player changes cells.
    internal sealed class ArenaRoutes
    {
        private const float Step = .65f;
        private readonly int width, height;
        private readonly Vector2 origin;
        private readonly bool[] walkable;
        private const byte East = 1, West = 2, North = 4, South = 8;
        private readonly byte[] links;
        private readonly int[] toward, queue;
        private readonly float radius;
        private int destination = -1;
        private readonly List<RaycastHit2D> casts = new(12);
        private readonly ContactFilter2D solids = new() { useTriggers = false };

        public ArenaRoutes(Bounds floor, float radius)
        {
            this.radius = radius;
            origin = floor.min;
            width = Mathf.CeilToInt(floor.size.x / Step) + 1;
            height = Mathf.CeilToInt(floor.size.y / Step) + 1;
            walkable = new bool[width * height];
            links = new byte[walkable.Length];
            toward = new int[walkable.Length];
            queue = new int[walkable.Length];
            var obstacles = new List<Collider2D>(8);
            for (int i = 0; i < walkable.Length; i++)
            {
                Physics2D.OverlapCircle(Point(i), radius, solids, obstacles);
                walkable[i] = !obstacles.Exists(c => c.attachedRigidbody == null ||
                    c.attachedRigidbody.bodyType == RigidbodyType2D.Static);
            }
            // Two empty cell centers may still have a thin wall between them.
            for (int i = 0; i < walkable.Length; i++)
            {
                if (!walkable[i]) continue;
                if (i % width + 1 < width) Link(i, i + 1, East, West);
                if (i + width < walkable.Length) Link(i, i + width, North, South);
            }
        }

        private void Link(int a, int b, byte forward, byte reverse)
        {
            if (!walkable[b] || !Clear(Point(a), Point(b))) return;
            links[a] |= forward;
            links[b] |= reverse;
        }

        private Vector2 Point(int index) => origin + new Vector2(index % width, index / width) * Step;
        private int Nearest(Vector2 p, bool fullClearance)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt((p.x - origin.x) / Step), 0, width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt((p.y - origin.y) / Step), 0, height - 1);
            int nearest = y * width + x;
            float connectionRadius = fullClearance ? radius : 0f;
            if (walkable[nearest] && Clear(p, Point(nearest), connectionRadius)) return nearest;
            nearest = -1;
            float best = float.PositiveInfinity;
            for (int dy = -3; dy <= 3; dy++)
            for (int dx = -3; dx <= 3; dx++)
            {
                int xx = x + dx, yy = y + dy;
                if (xx < 0 || xx >= width || yy < 0 || yy >= height) continue;
                int cell = yy * width + xx;
                float distance = (Point(cell) - p).sqrMagnitude;
                if (walkable[cell] && distance < best && Clear(p, Point(cell), connectionRadius)) { nearest = cell; best = distance; }
            }
            return nearest;
        }

        public bool Clear(Vector2 p, Vector2 target) => Clear(p, target, radius);

        private bool Clear(Vector2 p, Vector2 target, float clearance)
        {
            Vector2 offset = target - p;
            if (offset.sqrMagnitude < .000001f) return true;
            if (clearance > 0)
                Physics2D.CircleCast(p, clearance, offset.normalized, solids, casts, offset.magnitude);
            else
                Physics2D.Linecast(p, target, solids, casts);
            foreach (var hit in casts)
                if (hit.rigidbody == null || hit.rigidbody.bodyType == RigidbodyType2D.Static)
                    return false;
            return true;
        }

        public bool CanReach(Vector2 position, Vector2 target)
        {
            int start = Nearest(position, true);
            return start >= 0 && PrepareField(target) && toward[start] >= 0;
        }

        public Vector2 Waypoint(Vector2 p, Vector2 target)
        {
            if (!PrepareField(target)) return p;
            int start = Nearest(p, false);
            if (start < 0 || toward[start] < 0) return p;
            int next = toward[start];
            if (next == start) return target;
            Vector2 step = Point(next);
            return Clear(p, step) ? step : Point(start);
        }

        private bool PrepareField(Vector2 target)
        {
            int goal = Nearest(target, false);
            if (goal < 0) return false;
            if (goal == destination) return true;
            destination = goal;
            System.Array.Fill(toward, -1);
            int head = 0, tail = 1;
            queue[0] = goal;
            toward[goal] = goal;
            while (head < tail)
            {
                int cell = queue[head++];
                byte exits = links[cell];
                if ((exits & West) != 0) Visit(cell - 1, cell, ref tail);
                if ((exits & East) != 0) Visit(cell + 1, cell, ref tail);
                if ((exits & South) != 0) Visit(cell - width, cell, ref tail);
                if ((exits & North) != 0) Visit(cell + width, cell, ref tail);
            }
            return true;
        }

        private void Visit(int next, int from, ref int tail)
        {
            if (walkable[next] && toward[next] < 0)
            {
                toward[next] = from;
                queue[tail++] = next;
            }
        }
    }
}
