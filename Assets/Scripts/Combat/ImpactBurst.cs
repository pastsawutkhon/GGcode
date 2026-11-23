using System.Collections.Generic;
using UnityEngine;

namespace ATM.Gameplay
{
    public sealed class ImpactBurst : MonoBehaviour
    {
        [SerializeField] private LineRenderer ring;
        [SerializeField] private ParticleSystem sparks;
        private Stack<ImpactBurst> idle;
        private float age, extent;

        public void Show(Vector2 position, float radius)
        {
            idle ??= new Stack<ImpactBurst>();
            ImpactBurst fx = null;
            while (idle.Count > 0 && fx == null) fx = idle.Pop();
            if (fx == null) fx = Instantiate(this);
            fx.idle = idle;
            fx.transform.position = position;
            fx.age = 0;
            fx.extent = radius;
            fx.gameObject.SetActive(true);
            fx.Animate();
            fx.sparks.Clear();
            fx.sparks.Emit(9);
            fx.sparks.Play();
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age < .24f) { Animate(); return; }
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            gameObject.SetActive(false);
            if (idle.Count < 64) idle.Push(this); else Destroy(gameObject);
        }

        private void Animate()
        {
            float t = Mathf.Clamp01(age / .24f);
            float radius = extent * (1f - (1f - t) * (1f - t)) + .05f;
            Color c = Color.Lerp(new Color(1,.98f,.79f,1), new Color(1,.45f,.18f,0), t);
            ring.startColor = ring.endColor = c;
            ring.widthMultiplier = Mathf.Lerp(.13f, .006f, t);
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * 2f * Mathf.PI / ring.positionCount;
                ring.SetPosition(i, radius * new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0));
            }
        }
    }
}
