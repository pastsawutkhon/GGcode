using UnityEngine;

namespace ATM.Gameplay
{
    [RequireComponent(typeof(Player))]
    public sealed class WorldHealthBar : MonoBehaviour
    {
        [SerializeField] private RectTransform healthFill;

        private Player player;

        private void Awake() => player = GetComponent<Player>();

        private void LateUpdate()
        {
            float fraction = player.Stats.CurrentHP / player.Stats.MaxHP;
            healthFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1);
        }
    }
}
