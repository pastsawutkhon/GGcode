using UnityEngine;
using UnityEngine.InputSystem;

namespace ATM.Gameplay
{
    [RequireComponent(typeof(SpriteRenderer), typeof(Rigidbody2D))]
    [RequireComponent(typeof(Player))]
    [DefaultExecutionOrder(100)]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private SpriteRenderer map;
        [SerializeField] private Transform aimPivot;
        private Player player;
        private Collider2D movementCollider;
        private Rigidbody2D movementBody;
        private Vector2 moveInput;
        private Vector3 aimScale;
        private Vector2 aimDirection = Vector2.right;
        private int enabledFrame;
        private float dashLeft;
        private Vector2 dashDirection;

        internal SpriteRenderer Map => map;

        private void OnEnable() => enabledFrame = Time.frameCount;

        private void OnDisable()
        {
            moveInput = Vector2.zero;
            dashLeft = 0f;
            if (movementBody != null)
                movementBody.linearVelocity = Vector2.zero;
        }

        private void Awake()
        {
            player = GetComponent<Player>();
            movementCollider = GetComponent<Collider2D>();
            movementBody = GetComponent<Rigidbody2D>();
            if (aimCamera == null)
                aimCamera = Camera.main;

            if (aimPivot != null)
            {
                aimScale = aimPivot.localScale;
                aimDirection = aimPivot.right;
            }
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                moveInput = Vector2.zero;
                return;
            }

            float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            moveInput = new Vector2(x, y).normalized;
            player.Stats.TickDash(Time.deltaTime);
            if (keyboard.spaceKey.wasPressedThisFrame && dashLeft <= 0 && player.Stats.UseDash())
            {
                dashDirection = moveInput == Vector2.zero ? aimDirection : moveInput;
                dashLeft = .16f;
                player.Protect(player.Stats.DashProtection);
            }
        }

        private void FixedUpdate()
        {
            if (!player.IsAlive)
                return;

            Vector2 velocity = moveInput * player.Stats.MoveSpeed;
            if (dashLeft > 0)
            {
                velocity = dashDirection * 14f;
                dashLeft -= Time.fixedDeltaTime;
            }
            if (map != null)
            {
                Vector2 current = movementBody.position;
                Vector2 target = current + velocity * Time.fixedDeltaTime;
                Bounds floor = map.bounds;
                Vector3 padding = movementCollider.bounds.extents;
                target.x = Mathf.Clamp(target.x, floor.min.x + padding.x, floor.max.x - padding.x);
                target.y = Mathf.Clamp(target.y, floor.min.y + padding.y, floor.max.y - padding.y);
                velocity = (target - current) / Time.fixedDeltaTime;
            }
            movementBody.linearVelocity = velocity;
        }

        private void LateUpdate()
        {
            if (Time.timeScale == 0f || !player.IsAlive || aimPivot == null || aimCamera == null)
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.enabled)
                return;

            Ray ray = aimCamera.ScreenPointToRay(mouse.position.ReadValue());
            var gameplayPlane = new Plane(Vector3.forward, aimPivot.position);
            Vector2? aimPoint = null;
            if (gameplayPlane.Raycast(ray, out float distance))
            {
                aimPoint = ray.GetPoint(distance);
                Vector2 direction = aimPoint.Value - (Vector2)aimPivot.position;
                if (direction.sqrMagnitude > .0001f)
                    aimDirection = direction.normalized;
            }

            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            aimPivot.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            aimPivot.localScale = new Vector3(aimScale.x, aimDirection.x < 0 ? -aimScale.y : aimScale.y, aimScale.z);

            // A quick press can be released before LateUpdate on the same frame.
            if (Time.frameCount != enabledFrame && (mouse.leftButton.isPressed || mouse.leftButton.wasPressedThisFrame))
                player.TryFire(aimDirection, aimPoint);
        }
    }
}
