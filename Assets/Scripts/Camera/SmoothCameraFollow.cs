using UnityEngine;

namespace ATM.Gameplay
{
    [RequireComponent(typeof(Camera))]
    public sealed class SmoothCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Min(.01f)] private float smoothTime = .16f;

        private Camera followCamera;
        private PlayerController playerController;
        private float preferredSize;
        private Vector2 velocity;

        private void Awake()
        {
            followCamera = GetComponent<Camera>();
            preferredSize = followCamera.orthographicSize;
            if (target != null)
                target.TryGetComponent(out playerController);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 position = transform.position;
            Vector2 destination = target.position;
            SpriteRenderer map = playerController != null ? playerController.Map : null;
            Vector2 next;

            if (map != null && followCamera.orthographic)
            {
                Bounds bounds = map.bounds;
                float aspect = followCamera.aspect;
                float halfHeight = Mathf.Min(preferredSize, bounds.extents.y, bounds.extents.x / aspect);
                followCamera.orthographicSize = halfHeight;

                Vector2 center = bounds.center;
                Vector2 travel = Vector2.Max(Vector2.zero,
                    (Vector2)bounds.extents - new Vector2(halfHeight * aspect, halfHeight));
                Vector2 min = center - travel;
                Vector2 max = center + travel;
                destination = ClampCenter(destination, min, max);
                Vector2 smoothed = Vector2.SmoothDamp(position, destination, ref velocity, smoothTime);
                next = ClampCenter(smoothed, min, max);
                if (next.x != smoothed.x)
                    velocity.x = 0f;
                if (next.y != smoothed.y)
                    velocity.y = 0f;
            }
            else
            {
                next = Vector2.SmoothDamp(position, destination, ref velocity, smoothTime);
            }

            transform.position = new Vector3(next.x, next.y, position.z);
        }

        private static Vector2 ClampCenter(Vector2 position, Vector2 min, Vector2 max) =>
            new(Mathf.Clamp(position.x, min.x, max.x), Mathf.Clamp(position.y, min.y, max.y));
    }
}
