using UnityEngine;
using UnityEngine.SceneManagement;

namespace ATM.Gameplay
{
    internal enum RunPhase { Playing, ChoosingCard, GameOver }

    [RequireComponent(typeof(RunHud))]
    public sealed class GameRunManager : MonoBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private MeleeEnemy meleePrefab;
        [SerializeField] private RangedEnemy rangedPrefab;
        [SerializeField] private FastEnemy fastPrefab;
        [SerializeField] private BossEnemy[] bossPrefabs;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField, Min(.1f)] private float spawnInterval = .65f;
        [SerializeField, Min(0)] private float minimumSpawnDistance = 3f;
        [SerializeField, Min(.1f)] private float spawnClearance = .55f;

        private WaveManager waves;
        private PlayerController controller;
        private RunHud hud;
        private readonly Gold gold = new();
        private readonly UpgradeCards upgrades = new();

        internal RunPhase Phase { get; private set; }
        internal UpgradeCard[] Cards { get; private set; }
        internal int WaveNumber => waves.Number;

        private void Start()
        {
            if (player == null || !player.TryGetComponent(out Collider2D playerHitbox) ||
                !player.TryGetComponent(out controller) || meleePrefab == null || rangedPrefab == null ||
                fastPrefab == null || bossPrefabs == null || bossPrefabs.Length == 0 ||
                System.Array.Exists(bossPrefabs, prefab => prefab == null) ||
                spawnPoints == null || !System.Array.Exists(spawnPoints, point => point != null))
            {
                Debug.LogError("GameRunManager needs a Player with a collider and controller, enemy prefabs, and spawn points.", this);
                enabled = false;
                return;
            }

            hud = GetComponent<RunHud>();
            hud.Initialize(this);
            player.Died += EndRun;

            var spawner = new EnemySpawner(spawnPoints, meleePrefab, rangedPrefab, fastPrefab,
                bossPrefabs, player, playerHitbox, minimumSpawnDistance, spawnClearance);
            waves = new WaveManager(spawner, spawnInterval);
            waves.EnemyDefeated += AwardGold;
            RefreshGold();
            BeginWave(1);
        }

        private void Update()
        {
            if (Phase != RunPhase.Playing)
                return;

            waves.Tick(Time.deltaTime);
            if (!waves.Cleared)
                return;

            Phase = RunPhase.ChoosingCard;
            SuspendCombat();
            Cards = upgrades.Draw(WaveNumber, player.Stats.Luck);
            hud.ShowCards(Cards, player.Stats);
        }

        private void BeginWave(int number)
        {
            Cards = null;
            waves.Begin(number);
            hud.ShowWave(number);
            Phase = RunPhase.Playing;
            controller.enabled = true;
        }

        internal void ChooseCard(int index)
        {
            if (Phase != RunPhase.ChoosingCard || index < 0 || index >= Cards.Length)
                return;

            Cards[index].Apply(player.Stats);
            BeginWave(WaveNumber + 1);
        }

        internal void RerollCards()
        {
            if (Phase != RunPhase.ChoosingCard)
                return;

            if (!upgrades.TryReroll(WaveNumber, player.Stats.Luck, gold, out UpgradeCard[] offer))
                return;

            Cards = offer;
            RefreshGold();
            hud.ShowCards(Cards, player.Stats);
        }

        private void AwardGold()
        {
            if (Phase != RunPhase.Playing)
                return;

            gold.CreditEnemyDefeat(WaveNumber);
            RefreshGold();
        }

        private void RefreshGold() => hud.ShowGold(gold.Balance, upgrades.RerollCost);

        private void EndRun(Character dead)
        {
            Phase = RunPhase.GameOver;
            Cards = null;
            SuspendCombat();
            hud.ShowGameOver(WaveNumber);
        }

        private void SuspendCombat()
        {
            controller.enabled = false;
            foreach (Bullet shot in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
                shot.Despawn();
        }

        internal void Restart()
        {
            if (Phase == RunPhase.GameOver)
                SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        private void OnDestroy()
        {
            if (player != null)
                player.Died -= EndRun;
            if (waves != null)
            {
                waves.EnemyDefeated -= AwardGold;
                waves.Dispose();
            }
        }
    }
}
