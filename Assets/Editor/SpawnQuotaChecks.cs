using System;
using System.Collections.Generic;
using ATM.Waves;
using UnityEditor;
using UnityEngine;

internal static class SpawnQuotaChecks
{
    [MenuItem("Tools/ATM/Check Spawn Ratios")]
    public static void Run()
    {
        Check(new[] { 90, 10, 0 });
        foreach (int wave in new[] { 1, 5, 10, 20, 40 })
            Check(SpawnRatioCalculator.GetRegularWeights(wave));
    }

    private static void Check(int[] weights)
    {
        var hundred = new EnemySpawnSelector();
        hundred.Begin(weights, 100);
        for (int i = 0; i < 3; i++)
            if (hundred.Remaining((EnemyType)i) != weights[i])
                throw new Exception("100-spawn wave quota differs from its percentage.");

        int worst = 0, minB = 100, maxB = 0;
        for (int seed = 0; seed < 32; seed++)
        {
            var rng = new System.Random(seed);
            var selector = new EnemySpawnSelector();
            var window = new Queue<int>();
            int[] counts = new int[3];
            for (int wave = 1; wave <= 80; wave++)
            {
                int target = DifficultyScaler.GetRegularEnemyCount(wave);
                selector.Begin(weights, target);
                int[] quota = new int[3];
                for (int i = 0; i < quota.Length; i++)
                    quota[i] = selector.Remaining((EnemyType)i);
                for (int spawn = 0; spawn < target; spawn++)
                {
                    int roll = rng.Next(selector.DrawRange);
                    var type = selector.Peek(roll);
                    int index = (int)type;
                    if (selector.Peek(roll) != type || selector.Remaining(type) != quota[index])
                        throw new Exception("A blocked spawn consumed quota.");
                    selector.Commit(roll);
                    if (--quota[index] != selector.Remaining(type) || quota[index] < 0 || weights[index] == 0)
                        throw new Exception("Invalid remaining quota.");
                    window.Enqueue(index);
                    counts[index]++;
                    if (window.Count > 100) counts[window.Dequeue()]--;
                    if (window.Count == 100)
                    {
                        minB = Math.Min(minB, counts[1]);
                        maxB = Math.Max(maxB, counts[1]);
                        for (int i = 0; i < 3; i++)
                            worst = Math.Max(worst, Math.Abs(counts[i] - weights[i]));
                    }
                }
                if (selector.DrawRange != 0 || Array.Exists(quota, q => q != 0))
                    throw new Exception("Wave ended with remaining slots.");
            }
        }
        if (worst > 5) throw new Exception("100-spawn tolerance exceeded.");
        Debug.Log($"Spawn quota {string.Join("/", weights)}: 32 seeds, 80 waves each. " +
            $"B in every sliding 100: {minB}..{maxB}. Largest deviation across types: {worst}.");
    }
}
