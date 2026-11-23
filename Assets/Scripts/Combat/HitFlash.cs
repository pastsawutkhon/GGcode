using UnityEngine;

namespace ATM.Gameplay
{
    [DefaultExecutionOrder(200)]
    public sealed class HitFlash : MonoBehaviour
    {
        private static Material material;
        private SpriteRenderer source, overlay;
        private float left;

        private void Awake()
        {
            source = transform.Find("Character Art").GetComponent<SpriteRenderer>();
            var image = new GameObject("Hit highlight");
            image.transform.SetParent(source.transform, false);
            overlay = image.AddComponent<SpriteRenderer>();
            if (material == null) material = new Material(Resources.Load<Shader>("HitFlash"));
            overlay.sharedMaterial = material;
            overlay.sortingLayerID = source.sortingLayerID;
            overlay.sortingOrder = source.sortingOrder + 1;
            overlay.enabled = false;
        }

        public void Pulse() => left = .085f;

        private void LateUpdate()
        {
            overlay.enabled = left > 0f;
            if (!overlay.enabled) return;
            overlay.sprite = source.sprite;
            overlay.flipX = source.flipX;
            overlay.flipY = source.flipY;
            overlay.color = new Color(1, 1, 1, .58f * left / .085f);
            left -= Time.deltaTime;
        }
    }
}
