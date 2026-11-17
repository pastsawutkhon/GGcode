using System;
using UnityEngine;

public enum UpgradeRarity
{
    Legendary = 0,
    Epic = 1,
    Rare = 2,
    Uncommon = 3,
    Common = 4
}

/// <summary>
/// Computes five rarity probabilities (percentage points) for a given wave.
/// No card database, currency, or UI dependencies.
/// </summary>
public static class UpgradeRarityCalculator
{
    private const double GrowthPerWave = 1.10;
    private const double LegendaryHardCap = 15.0;
    private const double SoftCapThreshold = 0.50;
    private const double SoftCapInitialStrength = 10.0;
    private const double SoftCapDecayPerWave = 0.10;
    private const double SoftCapMinStrength = 0.10;
    private const double SoftCapFractionPerStrength = 0.075;
    private const double Epsilon = 1e-8;

    // Legendary, Epic, Rare, Uncommon, Common
    private static readonly double[] Minimum = { 0.0, 30.0, 10.0, 1.0, 0.1 };

    public static double[] GetPercentages(int wave, double luck = 100d)
    {
        if (wave < 1) throw new ArgumentOutOfRangeException(nameof(wave), "Wave must be >= 1.");

        double growth = Math.Pow(GrowthPerWave, wave - 1);
        double[] requested =
        {
            Math.Min(LegendaryHardCap, 0.2 * growth),
            1.0 * growth,
            10.0 * growth,
            25.0 * growth
        };

        // Stage 1: allocate by rarity priority, reserving conditional lower-tier minima.
        double[] before = new double[5];
        double remaining = 100.0;
        for (int i = 0; i < 4; i++)
        {
            double reserve = 0.0;
            for (int j = i + 1; j < 5; j++) reserve += Minimum[j];
            double value = Math.Min(requested[i], Math.Max(0.0, remaining - reserve));
            before[i] = value;
            remaining -= value;
        }
        before[4] = remaining;

        // A tier is conditionally protected only if it was crowded down to its minimum.
        bool[] protectedTier = new bool[5];
        for (int i = 1; i < 4; i++)
        {
            protectedTier[i] = before[i] <= Minimum[i] + Epsilon && requested[i] > before[i] + Epsilon;
        }
        protectedTier[4] = before[4] <= Minimum[4] + Epsilon;

        // Stage 2: snapshot-based soft cap. Every threshold is measured from 'before'.
        double[] after = (double[])before.Clone();
        double strength = Math.Max(SoftCapMinStrength,
            SoftCapInitialStrength - SoftCapDecayPerWave * (wave - 1));
        double deductionFraction = Math.Min(1.0, SoftCapFractionPerStrength * strength);

        for (int i = 0; i < 4; i++)
        {
            double threshold = before[i + 1] * SoftCapThreshold;
            if (before[i] <= threshold) continue;

            double deduction = (before[i] - threshold) * deductionFraction;
            after[i] -= deduction;

            double lowerTotal = 0.0;
            for (int j = i + 1; j < 5; j++) lowerTotal += before[j];
            if (lowerTotal > 0.0)
            {
                for (int j = i + 1; j < 5; j++)
                    after[j] += deduction * before[j] / lowerTotal;
            }
            else
            {
                after[4] += deduction;
            }
        }

        // Apply Luck in priority order while reserving the protected lower tiers.
        if (luck > 100d)
        {
            double budget = 100d;
            for (int i = 0; i < 4; i++)
            {
                double reserve = Minimum[4];
                for (int j = i + 1; j < 4; j++)
                    if (protectedTier[j])
                        reserve += Minimum[j];

                double value = after[i] * (luck / 100d);
                if (i == (int)UpgradeRarity.Legendary)
                    value = Math.Min(value, LegendaryHardCap);
                if (protectedTier[i])
                    value = Math.Max(value, Minimum[i]);
                after[i] = Math.Min(value, Math.Max(0d, budget - reserve));
                budget -= after[i];
            }
            after[4] = budget;
        }

        // Final conditional minimum protection.
        // Transfer only the deficit, in priority order, from tiers with spare probability.
        for (int i = 1; i < 5; i++)
        {
            if (!protectedTier[i] || after[i] >= Minimum[i]) continue;
            double deficit = Minimum[i] - after[i];
            for (int j = 0; j < 5 && deficit > Epsilon; j++)
            {
                if (j == i) continue;
                double floor = protectedTier[j] ? Minimum[j] : 0.0;
                double available = Math.Max(0.0, after[j] - floor);
                double taken = Math.Min(deficit, available);
                after[j] -= taken;
                after[i] += taken;
                deficit -= taken;
            }
        }

        return after;
    }

    /// <summary>Rolls one rarity from the final probability distribution.</summary>
    public static UpgradeRarity Roll(int wave, double luck = 100d) => Roll(GetPercentages(wave, luck));

    public static UpgradeRarity Roll(double[] percentages)
    {
        double roll = UnityEngine.Random.value * 100.0;
        double cumulative = 0.0;
        for (int i = 0; i < percentages.Length; i++)
        {
            cumulative += percentages[i];
            if (roll < cumulative) return (UpgradeRarity)i;
        }
        return UpgradeRarity.Common; // Floating-point fallback.
    }
}
