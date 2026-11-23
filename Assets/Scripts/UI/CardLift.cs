using UnityEngine;
using UnityEngine.EventSystems;

namespace ATM.Gameplay
{
    public sealed class CardLift : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform rect;
        private Vector2 rest;
        private bool over;
        private void OnEnable() { rect = (RectTransform)transform; rest = rect.anchoredPosition; }
        public void OnPointerEnter(PointerEventData e) => over = true;
        public void OnPointerExit(PointerEventData e) => over = false;
        private void Update() => rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition,
            rest + Vector2.up * (over ? 10 : 0), 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        private void OnDisable() { over = false; if (rect != null) rect.anchoredPosition = rest; }
    }
}
