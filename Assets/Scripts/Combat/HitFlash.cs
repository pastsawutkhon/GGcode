using UnityEngine;

namespace ATM.Gameplay
{
    [DefaultExecutionOrder(200)]
    public sealed class HitFlash : MonoBehaviour
    {
        private static Material material;
        private SpriteRenderer source, overlay;
        private const float Duration = .085f;
        private const float HoldDuration = .03f;
        private const float PeakOpacity = .85f;
        private const double MinimumInterval = .20;
        private static readonly int Opacity = Shader.PropertyToID("_FlashOpacity");
        private MaterialPropertyBlock properties;
        private double pulseStarted = double.NegativeInfinity;

        private void Awake()
        {
            properties = new MaterialPropertyBlock();
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

        public void Pulse()
        {
            double now = Time.timeAsDouble;
            if (now - pulseStarted >= MinimumInterval)
                pulseStarted = now;
        }

        private void LateUpdate()
        {
            float age = (float)(Time.timeAsDouble - pulseStarted);
            overlay.enabled = age < Duration;
            if (!overlay.enabled) return;
            overlay.sprite = source.sprite;
            overlay.flipX = source.flipX;
            overlay.flipY = source.flipY;
            float fade = Mathf.InverseLerp(HoldDuration, Duration, age);
            properties.SetFloat(Opacity, PeakOpacity * (1f - fade));
            overlay.SetPropertyBlock(properties);
        }
    }
}
