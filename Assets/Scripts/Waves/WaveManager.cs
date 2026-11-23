using System;
using System.Collections.Generic;
using ATM.Waves;

namespace ATM.Gameplay
{
    internal enum WavePhase { SpawningRegular, ClearingRegular, WaitingForBoss, BossFight, Completed }

    internal sealed class WaveManager : IDisposable
    {
        private const float InitialDelay = 1f;
        private const float SpawnRetryDelay = .2f;

        private readonly EnemySpawner spawner;
        private readonly float spawnInterval;
        private readonly HashSet<Enemy> livingEnemies = new();
        private readonly EnemySpawnSelector selector;
        private float spawnIn;
        private int regularSpawned, regularTarget;
        private bool bossWave;

        public int Number { get; private set; }
        public WavePhase Phase { get; private set; } = WavePhase.Completed;
        public bool Cleared => Phase == WavePhase.Completed;
        public event Action<Enemy> EnemyDefeated;

        public WaveManager(EnemySpawner spawner, float spawnInterval, EnemySpawnSelector selector)
        {
            this.spawner = spawner;
            this.selector = selector;
            this.spawnInterval = spawnInterval;
        }

        public void Begin(int number)
        {
            if (!Cleared)
                throw new InvalidOperationException("Complete the active wave before starting another.");

            regularTarget = DifficultyScaler.GetRegularEnemyCount(number);
            bossWave = DifficultyScaler.IsBossWave(number);
            Number = number;
            regularSpawned = 0;
            selector.Begin(SpawnRatioCalculator.GetRegularWeights(number), regularTarget);
            spawnIn = InitialDelay;
            Phase = WavePhase.SpawningRegular;
        }

        public void Tick(float dt)
        {
            if (Phase != WavePhase.SpawningRegular && Phase != WavePhase.WaitingForBoss)
                return;

            spawnIn -= dt;
            if (spawnIn > 0)
                return;

            bool spawningBoss = Phase == WavePhase.WaitingForBoss;
            if (!spawner.TryFindPoint(spawningBoss, out var point))
            {
                spawnIn = SpawnRetryDelay;
                return;
            }

            int roll = spawningBoss ? 0 : UnityEngine.Random.Range(0, selector.DrawRange);
            EnemyType type = spawningBoss ? default : selector.Peek(roll);
            Enemy enemy = spawner.Spawn(point, spawningBoss, type, Number);
            if (!spawningBoss)
                selector.Commit(roll);
            livingEnemies.Add(enemy);
            enemy.Died += RecordDefeat;
            spawnIn = spawnInterval;
            if (spawningBoss)
            {
                Phase = WavePhase.BossFight;
                return;
            }

            if (++regularSpawned == regularTarget)
                Phase = WavePhase.ClearingRegular;
        }

        private void RecordDefeat(Character character)
        {
            if (character is not Enemy enemy || !livingEnemies.Remove(enemy))
                return;

            enemy.Died -= RecordDefeat;
            if (livingEnemies.Count == 0)
            {
                if (Phase == WavePhase.ClearingRegular)
                {
                    Phase = bossWave ? WavePhase.WaitingForBoss : WavePhase.Completed;
                    spawnIn = InitialDelay;
                }
                else if (Phase == WavePhase.BossFight)
                {
                    Phase = WavePhase.Completed;
                }
            }

            EnemyDefeated?.Invoke(enemy);
        }

        public void Dispose()
        {
            foreach (Enemy enemy in livingEnemies)
                enemy.Died -= RecordDefeat;
            livingEnemies.Clear();
        }
    }
}
