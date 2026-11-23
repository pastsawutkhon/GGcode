using UnityEngine;

namespace ATM.Gameplay
{
    public sealed class GoldPickup : MonoBehaviour
    {
        [SerializeField, Min(.2f)] private float magnetRadius = 2f;
        private GameRunManager run;
        private Transform target;
        private float age;
        private SpriteRenderer visual;
        internal long Amount { get; private set; }

        internal void Initialize(GameRunManager manager, Transform player, long amount)
        {
            visual = GetComponent<SpriteRenderer>();
            run = manager;
            target = player;
            Amount = amount;
        }

        private void Update()
        {
            if (run.Phase != RunPhase.Playing) return;
            age += Time.deltaTime;
            if (age >= 30f)
            {
                run.Expire(this);
                return;
            }
            if (age >= 25f) visual.enabled = Mathf.Repeat(age, .42f) < .29f;
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance > magnetRadius)
                return;
            transform.position = Vector3.MoveTowards(transform.position, target.position, 9f * Time.deltaTime);
            if (distance < .4f)
                run.Collect(this);
        }
    }
}
