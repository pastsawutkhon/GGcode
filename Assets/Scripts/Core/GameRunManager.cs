using System.Collections;
using System.Collections.Generic;
using ATM.Waves;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ATM.Gameplay
{
    internal enum RunPhase { Menu, Transition, Playing, ChoosingCard, Paused, GameOver }

    [RequireComponent(typeof(RunHud))]
    public sealed class GameRunManager : MonoBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private MeleeEnemy meleePrefab;
        [SerializeField] private RangedEnemy rangedPrefab;
        [SerializeField] private FastEnemy fastPrefab;
        [SerializeField] private BossEnemy[] bossPrefabs;
        [SerializeField] private Transform regularSpawnGroup, bossSpawnGroup;
        [SerializeField] private GoldPickup goldDrop;
        [SerializeField, Min(.1f)] private float spawnInterval = .65f;
        [SerializeField, Min(0)] private float minimumSpawnDistance = 3f;
        [SerializeField, Min(.1f)] private float spawnClearance = .55f;

        private sealed class RunData
        {
            public readonly string id = System.Guid.NewGuid().ToString("N");
            public readonly Gold gold = new();
            public readonly UpgradeCards upgrades = new();
            public readonly EnemySpawnSelector selector = new();
            public CharacterStats stats;
            public int nextWave = 1, kills;
            public double playedSeconds;
        }

        private RunPhase phaseBeforePause;
        private bool clockFrozen, previousAudioPause;
        private float previousTimeScale;
        private static RunData incoming;
        private RunData data;
        private readonly HashSet<GoldPickup> drops = new();
        private WaveManager waves;
        private PlayerController controller;
        private RunHud hud;
        private PrecinctProfile profile;
        internal RunPhase Phase { get; private set; }
        internal UpgradeCard[] Cards { get; private set; }
        internal int WaveNumber => waves.Number;
        internal CharacterStats Stats => player.Stats;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTransfer() => incoming = null;

        private void Start()
        {
            hud = GetComponent<RunHud>();
            hud.Initialize(this);
            if (player == null || !player.TryGetComponent(out controller))
            {
                Debug.LogError("Assign a player with PlayerController to Game Run.", this);
                enabled = false;
                return;
            }
            controller.enabled = false;
            profile = new PrecinctProfile();
            if (gameObject.scene.name == "SampleScene")
            {
                incoming = null;
                Phase = RunPhase.Menu;
                hud.ShowMenu(profile);
                return;
            }

            if (regularSpawnGroup == null || regularSpawnGroup.childCount == 0 ||
                bossSpawnGroup == null || bossSpawnGroup.childCount == 0 ||
                controller.Map == null || Camera.main == null || goldDrop == null ||
                meleePrefab == null || rangedPrefab == null || fastPrefab == null ||
                bossPrefabs == null || bossPrefabs.Length == 0 ||
                System.Array.Exists(bossPrefabs, boss => boss == null) ||
                !player.TryGetComponent(out Collider2D hitbox))
            {
                Debug.LogError("Arena needs a floor, main camera, player collider, spawn markers, enemy and gold prefabs.", this);
                enabled = false;
                return;
            }
            data = incoming ?? new RunData();
            incoming = null;
            if (data.stats == null)
                profile.Apply(player.Stats);
            else
                player.RestoreStats(data.stats);
            var spawner = new EnemySpawner(Children(regularSpawnGroup), Children(bossSpawnGroup), meleePrefab, rangedPrefab,
                fastPrefab, bossPrefabs, player, hitbox, minimumSpawnDistance, spawnClearance);
            waves = new WaveManager(spawner, spawnInterval, data.selector);
            waves.EnemyDefeated += DropGold;
            player.Died += EndRun;
            RefreshGold();
            Phase = RunPhase.Transition;
            StartCoroutine(EnterArena());
        }

        private static Transform[] Children(Transform group)
        {
            var points = new Transform[group.childCount];
            for (int i = 0; i < points.Length; i++)
                points[i] = group.GetChild(i);
            return points;
        }

        private IEnumerator EnterArena()
        {
            yield return hud.Shutters(false, AreaName(gameObject.scene.name), data.nextWave);
            BeginWave(data.nextWave);
        }

        private static string AreaName(string scene) => scene switch
        {
            "MAP 1" => "Exercise yard",
            "MAP 2" => "Locker room",
            _ => "Basketball court"
        };

        internal void StartRun()
        {
            if (Phase == RunPhase.Menu || Phase == RunPhase.GameOver)
                StartCoroutine(Travel("MAP 1", new RunData()));
        }

        internal void BuyUpgrade(Training track)
        {
            if (Phase != RunPhase.Menu) return;
            profile.Buy(track);
            hud.ShowCharacter(profile);
        }

        private IEnumerator Travel(string destination, RunData transfer)
        {
            Phase = RunPhase.Transition;
            yield return hud.Shutters(true, AreaName(destination), transfer.nextWave);
            incoming = transfer;
            yield return SceneManager.LoadSceneAsync(destination);
        }

        private void Update()
        {
            if (Phase != RunPhase.Playing) return;
            data.playedSeconds += Time.deltaTime;
            waves.Tick(Time.deltaTime);
            if (!waves.Cleared) return;
            Phase = RunPhase.ChoosingCard;
            RefreshGold();
            SuspendCombat();
            Cards = data.upgrades.Draw(WaveNumber, player.Stats);
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

        internal void TogglePause()
        {
            if (Phase == RunPhase.Paused)
            {
                hud.TogglePauseDrawer();
                return;
            }
            if (Phase != RunPhase.Playing && Phase != RunPhase.ChoosingCard) return;
            phaseBeforePause = Phase;
            Phase = RunPhase.Paused;
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            clockFrozen = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            hud.OpenPause(player, WaveNumber, data.gold.Balance, data.kills, data.playedSeconds,
                Gold.Multiplier(profile.Level(Training.Bounty)));
        }

        internal void ResumeAfterSlide()
        {
            if (Phase != RunPhase.Paused) return;
            RestoreClock();
            Phase = phaseBeforePause;
        }

        private void RestoreClock()
        {
            if (!clockFrozen) return;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            clockFrozen = false;
        }

        internal void Forfeit()
        {
            if (Phase == RunPhase.Paused) Finish(false);
        }

        internal void ChooseCard(int index)
        {
            if (Phase != RunPhase.ChoosingCard || index < 0 || index >= Cards.Length) return;
            Cards[index].Apply(player.Stats);
            data.nextWave = WaveNumber + 1;
            if (!DifficultyScaler.IsBossWave(WaveNumber))
            {
                BeginWave(data.nextWave);
                return;
            }
            data.stats = player.Stats;
            string current = gameObject.scene.name;
            int mapIndex = current == "MAP 1" ? 0 : current == "MAP 2" ? 1 : 2;
            int next = (mapIndex + Random.Range(1, 3)) % 3 + 1;
            StartCoroutine(Travel("MAP " + next, data));
        }

        internal void RerollCards()
        {
            if (Phase != RunPhase.ChoosingCard ||
                !data.upgrades.TryReroll(WaveNumber, player.Stats, data.gold, out UpgradeCard[] offer)) return;
            Cards = offer;
            RefreshGold();
            hud.ShowCards(Cards, player.Stats);
        }

        private void DropGold(Enemy enemy)
        {
            if (Phase != RunPhase.Playing) return;
            data.kills++;
            var drop = Instantiate(goldDrop, enemy.transform.position, Quaternion.identity);
            drop.Initialize(this, player.transform, Gold.Reward(WaveNumber, profile.Level(Training.Bounty)));
            drops.Add(drop);
        }

        internal void Collect(GoldPickup drop)
        {
            if (Phase != RunPhase.Playing || !drops.Remove(drop)) return;
            data.gold.Credit(drop.Amount);
            Destroy(drop.gameObject);
            RefreshGold();
        }

        internal void Expire(GoldPickup drop)
        {
            if (drops.Remove(drop)) Destroy(drop.gameObject);
        }

        internal void Escape()
        {
            if (Phase == RunPhase.ChoosingCard) Finish(true);
        }

        private void RefreshGold() => hud.ShowGold(data.gold.Balance, data.upgrades.RerollCost);

        private void EndRun(Character dead)
        {
            if (Phase == RunPhase.Playing) Finish(false);
        }

        private void Finish(bool escaped)
        {
            Phase = RunPhase.GameOver;
            Cards = null;
            SuspendCombat();
            long score = escaped ? data.gold.Balance : 0;
            if (escaped) profile.Bank(data.id, score);
            hud.ShowResult(escaped, score, WaveNumber, data.kills, data.gold, data.playedSeconds);
        }

        private void SuspendCombat()
        {
            controller.enabled = false;
            foreach (Bullet shot in FindObjectsByType<Bullet>())
                shot.Despawn();
        }

        internal void Restart()
        {
            if (Phase != RunPhase.GameOver && Phase != RunPhase.Paused) return;
            Phase = RunPhase.Transition;
            SceneManager.LoadScene("SampleScene");
        }

        private void OnDestroy()
        {
            RestoreClock();
            if (player != null) player.Died -= EndRun;
            if (waves == null) return;
            waves.EnemyDefeated -= DropGold;
            waves.Dispose();
        }
    }
}
