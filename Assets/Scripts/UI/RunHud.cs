using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ATM.Gameplay
{
    public sealed class RunHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private TMP_Text gameGoldLabel;
        [SerializeField] private TMP_Text cardGoldLabel;
        [SerializeField] private TMP_Text rerollLabel;
        [SerializeField] private GameObject inGamePanel;
        [SerializeField] private GameObject cardPanel;
        [SerializeField] private UpgradeCardView[] cardViews;
        [SerializeField] private Button rerollButton;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private Button restartButton;

        [SerializeField] private GameObject menuPanel, shutterPanel;
        [SerializeField] private RectTransform leftDoor, rightDoor;
        [SerializeField] private TMP_Text areaLabel, creditsLabel, vitalityLabel, powerLabel;
        [SerializeField] private Button startButton, vitalityButton, powerButton, defenseButton, bountyButton;

        [SerializeField] private GameObject characterPage;
        [SerializeField] private Button characterButton, backButton, escapeButton, againButton;
        [SerializeField] private TMP_Text baseStats, scoreLabel, outcomeTitle, dashLabel;
        [SerializeField] private RawImage blurredArena;
        private readonly RunCounter wallet = new(), scoreCounter = new(), creditCounter = new();
        private PrecinctProfile menuProfile;
        private Gold resultGold;
        private string resultHeader;
        private float resultProgress;
        private long secured;
        private TMP_Text escapeLabel;
        private RenderTexture snapshot;
        private Material blur;
        private int shownDash = -1, shownSeconds = -1;

        private static readonly Key[] CardKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };
        private GameRunManager run;
        private PauseDrawer pauseDrawer;
        private CanvasGroup cardInput;

        internal void OpenPause(Player player, int wave, long gold, int kills, double seconds, decimal multiplier)
        {
            if (pauseDrawer == null)
            {
                var canvas = inGamePanel.GetComponentInParent<Canvas>(true).rootCanvas;
                pauseDrawer = Instantiate(Resources.Load<PauseDrawer>("Pause Drawer"), canvas.transform, false);
                pauseDrawer.Bind(run, OnDrawerClosed);
            }
            if (cardPanel.activeSelf)
            {
                if (!cardPanel.TryGetComponent(out cardInput)) cardInput = cardPanel.AddComponent<CanvasGroup>();
                cardInput.interactable = false;
            }
            pauseDrawer.Open(player, wave, gold, kills, seconds, multiplier);
        }

        internal void TogglePauseDrawer() => pauseDrawer.Toggle();

        private void OnDrawerClosed()
        {
            if (cardInput != null) cardInput.interactable = true;
            run.ResumeAfterSlide();
        }

        internal void Initialize(GameRunManager runManager)
        {
            run = runManager;
            characterButton.onClick.AddListener(() => ShowCharacter(menuProfile));
            backButton.onClick.AddListener(() => ShowMenu(menuProfile));
            escapeButton.onClick.AddListener(run.Escape);
            escapeLabel = escapeButton.GetComponentInChildren<TMP_Text>(true);
            againButton.onClick.AddListener(run.StartRun);
            startButton.onClick.AddListener(run.StartRun);
            vitalityButton.onClick.AddListener(() => run.BuyUpgrade(Training.Vitality));
            powerButton.onClick.AddListener(() => run.BuyUpgrade(Training.Power));
            defenseButton.onClick.AddListener(() => run.BuyUpgrade(Training.Defense));
            bountyButton.onClick.AddListener(() => run.BuyUpgrade(Training.Bounty));
            for (int i = 0; i < cardViews.Length; i++)
            {
                int cardIndex = i;
                cardViews[i].Bind(() => run.ChooseCard(cardIndex));
            }
            rerollButton.onClick.AddListener(run.RerollCards);
            restartButton.onClick.AddListener(run.Restart);
        }

        internal void ShowMenu(PrecinctProfile profile)
        {
            menuProfile = profile;
            characterPage.SetActive(false);
            inGamePanel.SetActive(false);
            cardPanel.SetActive(false);
            gameOverPanel.SetActive(false);
            shutterPanel.SetActive(false);
            menuPanel.SetActive(true);
        }

        internal void ShowCharacter(PrecinctProfile profile)
        {
            menuPanel.SetActive(false);
            characterPage.SetActive(true);
            creditCounter.Set(profile.Credits);
            creditsLabel.text = $"CREDITS    {creditCounter.Value:N0}";
            var raw = run.Stats;
            var trained = raw.CopyForPreview();
            profile.Apply(trained);
            baseStats.text = $"MAX HP     {trained.MaxHP:0.##}\nATTACK     {trained.AttackDamage:0.##}\nDEFENSE     {trained.Defense:0.##}\nMOVE SPEED     {trained.MoveSpeed:0.##}\nFIRE RATE     {trained.AttackSpeed:0.##}/s";
            ShowTraining(vitalityButton, vitalityLabel, Training.Vitality, "Max HP", raw.MaxHP, profile);
            ShowTraining(powerButton, powerLabel, Training.Power, "Attack", raw.AttackDamage, profile);
            ShowTraining(defenseButton, defenseButton.GetComponentInChildren<TMP_Text>(), Training.Defense, "Defense", raw.Defense, profile);
            ShowTraining(bountyButton, bountyButton.GetComponentInChildren<TMP_Text>(), Training.Bounty, "Gold multiplier", 1, profile);
        }

        private static void ShowTraining(Button button, TMP_Text label, Training track, string effect, float baseValue, PrecinctProfile profile)
        {
            int level = profile.Level(track);
            string preview;
            if (track == Training.Bounty)
                preview = $"×{Gold.Multiplier(level):0.0} → ×{Gold.Multiplier(level + 1):0.0}";
            else
            {
                float offset = track == Training.Defense ? 10f : 0f;
                float now = CharacterStats.Trained(baseValue + offset, level) - offset;
                float next = CharacterStats.Trained(baseValue + offset, level + 1) - offset;
                preview = $"{now:G5} → {next:G5}";
            }
            label.text = $"{effect.ToUpperInvariant()}    Lv {level}\n{preview}      /      {profile.Price(track):N0} credits";
            button.interactable = profile.CanBuy(track);
        }

        internal IEnumerator Shutters(bool closing, string area, int wave)
        {
            shutterPanel.SetActive(true);
            areaLabel.text = $"{area.ToUpperInvariant()}   /   WAVE {wave:00}";
            float elapsed = 0f;
            while (elapsed < .32f)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / .32f);
                SetDoorOpening(closing ? 1f - t : t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            SetDoorOpening(closing ? 0 : 1);
            if (closing)
            {
                menuPanel.SetActive(false);
                cardPanel.SetActive(false);
                yield return new WaitForSecondsRealtime(.18f);
            }
            else
                shutterPanel.SetActive(false);
        }

        private void SetDoorOpening(float open)
        {
            float width = ((RectTransform)shutterPanel.transform).rect.width;
            leftDoor.anchoredPosition = Vector2.left * open * width * .5f;
            rightDoor.anchoredPosition = Vector2.right * open * width * .5f;
            areaLabel.alpha = 1f - open;
        }

        internal void ShowWave(int number)
        {
            ReleaseSnapshot();
            waveLabel.text = $"WAVE   {number:00}";
            inGamePanel.SetActive(true);
            cardPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        internal void ShowGold(long gold, long cost)
        {
            wallet.Set(gold);
            escapeLabel.text = $"ESCAPE    {gold:N0}";
            gameGoldLabel.text = cardGoldLabel.text = $"GOLD   {wallet.Value:N0}";
            rerollLabel.text = cost == 0 ? "Reroll (Free)" : $"Reroll ({cost})";
            rerollButton.interactable = gold >= cost;
        }

        internal void ShowCards(UpgradeCard[] cards, CharacterStats stats)
        {
            for (int i = 0; i < cards.Length; i++)
                cardViews[i].Show(cards[i], stats);
            inGamePanel.SetActive(false);
            if (snapshot == null) CaptureArena();
            cardPanel.SetActive(true);
        }

        internal void ShowResult(bool escaped, long score, int wave, int kills, Gold gold, double seconds)
        {
            cardPanel.SetActive(false);
            inGamePanel.SetActive(false);
            ReleaseSnapshot();
            if (pauseDrawer != null) pauseDrawer.gameObject.SetActive(false);
            outcomeTitle.text = escaped ? "ESCAPE SUCCESS" : "GAME OVER";
            scoreLabel.text = "00000";
            scoreCounter.Set(score);
            var time = System.TimeSpan.FromSeconds(seconds);
            resultHeader = $"WAVE   {wave:00}       DEFEATED   {kills}       TIME   {(int)time.TotalMinutes:00}:{time.Seconds:00}";
            resultGold = gold;
            resultProgress = 0;
            secured = score;
            RefreshResult();
            gameOverPanel.SetActive(true);
        }

        private void RefreshResult()
        {
            int t = Mathf.RoundToInt(Mathf.SmoothStep(0,1,resultProgress) * 1000f);
            var earned = resultGold.Earned * t / 1000;
            var spent = resultGold.Spent * t / 1000;
            long banked = (long)((decimal)secured * t / 1000);
            long balance = (long)((decimal)resultGold.Balance * t / 1000);
            resultLabel.text = $"{resultHeader}\nCollected   {earned:N0}       Spent   {spent:N0}\nRemaining   {balance:N0}       Credits secured   {banked:N0}";
        }

        private void CaptureArena()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            int w = Mathf.Max(16, cam.pixelWidth / 7), h = Mathf.Max(9, cam.pixelHeight / 7);
            var source = RenderTexture.GetTemporary(w,h,24);
            snapshot = RenderTexture.GetTemporary(w,h,0);
            snapshot.filterMode = FilterMode.Bilinear;
            var old = cam.targetTexture;
            var active = RenderTexture.active;
            try
            {
                cam.targetTexture = source;
                cam.Render();
                if (blur == null) blur = new Material(Resources.Load<Shader>("SoftFocus"));
                Graphics.Blit(source, snapshot, blur);
                blurredArena.texture = snapshot;
            }
            finally
            {
                cam.targetTexture = old;
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(source);
            }
        }

        private void ReleaseSnapshot()
        {
            if (snapshot == null) return;
            blurredArena.texture = null;
            RenderTexture.ReleaseTemporary(snapshot);
            snapshot = null;
        }

        private void OnDestroy()
        {
            ReleaseSnapshot();
            if (blur != null) Destroy(blur);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && run != null)
            {
                run.TogglePause();
                return;
            }
            if (run != null && run.Phase == RunPhase.Paused) return;
            if (wallet.Tick(Time.unscaledDeltaTime))
                gameGoldLabel.text = cardGoldLabel.text = $"GOLD   {wallet.Value:N0}";
            if (resultGold != null && resultProgress < 1)
            {
                resultProgress = Mathf.Min(1f, resultProgress + Time.unscaledDeltaTime / .42f);
                RefreshResult();
            }
            if (scoreCounter.Tick(Time.unscaledDeltaTime)) scoreLabel.text = scoreCounter.Value.ToString("D5");
            if (creditCounter.Tick(Time.unscaledDeltaTime)) creditsLabel.text = $"CREDITS    {creditCounter.Value:N0}";
            if (run != null && run.Phase == RunPhase.Playing)
            {
                var stats = run.Stats;
                int seconds = Mathf.CeilToInt(stats.DashRecharge);
                if (shownDash != stats.DashReady || shownSeconds != seconds)
                {
                    dashLabel.text = stats.DashCapacity == 0 ? "" : $"SPACE   DASH   {stats.DashReady}/{stats.DashCapacity}" +
                        (seconds > 0 ? $"    {seconds}s" : "");
                    shownDash = stats.DashReady;
                    shownSeconds = seconds;
                }
            }
            if (run == null || keyboard == null)
                return;

            if (run.Phase == RunPhase.GameOver)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                    run.Restart();
                return;
            }

            if (run.Phase != RunPhase.ChoosingCard)
                return;

            if (keyboard.rKey.wasPressedThisFrame)
            {
                run.RerollCards();
                return;
            }

            for (int i = 0; i < CardKeys.Length; i++)
            {
                if (keyboard[CardKeys[i]].wasPressedThisFrame)
                {
                    run.ChooseCard(i);
                    return;
                }
            }
        }
    }
}
