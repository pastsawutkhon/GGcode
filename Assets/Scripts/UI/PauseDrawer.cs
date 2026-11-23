using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ATM.Gameplay
{
    public sealed class PauseDrawer : MonoBehaviour
    {
        [SerializeField] private RectTransform sheet;
        [SerializeField] private Image shade, portrait, healthFill;
        [SerializeField] private TMP_Text runInfo, health, survival, weapons, mobility, rewards;
        [SerializeField] private Button resume, restart, mainMenu;
        private Action onClosed;
        private GameObject previousSelection;
        private float progress;
        private bool opening;

        internal void Bind(GameRunManager run, Action closed)
        {
            onClosed = closed;
            resume.onClick.AddListener(Toggle);
            restart.onClick.AddListener(run.Forfeit);
            mainMenu.onClick.AddListener(run.Restart);
        }

        internal void Open(Player player, int wave, long gold, int kills, double seconds, decimal multiplier)
        {
            var s = player.Stats;
            var time = TimeSpan.FromSeconds(seconds);
            runInfo.text = $"WAVE {wave:00}     /     {(int)time.TotalMinutes:00}:{time.Seconds:00}";
            portrait.sprite = player.GetComponent<DirectionalCharacter>().Portrait;
            health.text = $"{s.CurrentHP:0.##} / {s.MaxHP:0.##} HP";
            healthFill.fillAmount = Mathf.Clamp01(s.CurrentHP / s.MaxHP);
            survival.text = $"{s.Defense:0.##}\n{s.Barrier:0.##}\n{s.MoveSpeed:0.##}\n{s.Luck:0.##}";
            weapons.text = $"{s.AttackDamage:0.##}\n{s.AttackSpeed:0.##}/s\n{s.ProjectileRange:0.##}\n{s.ProjectileCount}\n{s.Pierce}\n{s.Splash * 100f:0.##}%";
            mobility.text = $"{s.DashReady} / {s.DashCapacity}\n{s.DashRecharge:0.##} s\n{s.DashProtection:0.##} s";
            rewards.text = $"{gold:N0}\n×{multiplier:0.0}\n{kills:N0}";
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            previousSelection = EventSystem.current?.currentSelectedGameObject;
            EventSystem.current?.SetSelectedGameObject(resume.gameObject);
            progress = 0f;
            opening = true;
            Draw();
        }

        internal void Toggle() => opening = !opening;

        private void Update()
        {
            progress = Mathf.MoveTowards(progress, opening ? 1f : 0f, Time.unscaledDeltaTime / .23f);
            Draw();
            if (opening || progress > 0f) return;
            gameObject.SetActive(false);
            onClosed();
            EventSystem.current?.SetSelectedGameObject(previousSelection);
        }

        private void Draw()
        {
            float t = Mathf.SmoothStep(0f, 1f, progress);
            sheet.anchoredPosition = Vector2.left * ((1f - t) * sheet.rect.width);
            shade.color = new Color(.035f, .05f, .06f, .52f * t);
        }
    }
}
