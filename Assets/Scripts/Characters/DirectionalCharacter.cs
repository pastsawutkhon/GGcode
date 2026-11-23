using UnityEngine;

namespace ATM.Gameplay
{
    public sealed class DirectionalCharacter : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private Sprite idle;
        [SerializeField] private Sprite[] front, back, side;
        [SerializeField, Min(1)] private float framesPerSecond = 8;
        internal Sprite Portrait => idle;
        private Sprite[] facing;
        private float time;
        private bool moving;

        private void LateUpdate()
        {
            Vector2 velocity = body.linearVelocity;
            if (velocity.sqrMagnitude < .01f)
            {
                visual.sprite = facing == null || facing == front ? idle : facing[0];
                time = 0;
                moving = false;
                return;
            }
            bool horizontal = Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y) + .05f;
            if (Mathf.Abs(Mathf.Abs(velocity.x) - Mathf.Abs(velocity.y)) <= .05f)
                horizontal = facing == side;
            Sprite[] next = horizontal ? side : velocity.y > 0 ? back : front;
            if (!moving || facing != next)
                time = 0;
            facing = next;
            moving = true;
            visual.flipX = horizontal && velocity.x > 0; // side art faces left
            visual.sprite = facing[(int)time % facing.Length];
            time = (time + Time.deltaTime * framesPerSecond) % facing.Length;
        }
    }
}
