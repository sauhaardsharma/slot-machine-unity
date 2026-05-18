using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles weighted random number generation for symbol selection.
/// Higher weight = more frequent appearance on reels.
/// </summary>
public static class RNGSystem
{
    /// <summary>
    /// Picks a random SymbolData using weighted probability.
    /// </summary>
    public static SymbolData GetWeightedRandom(List<SymbolData> symbols)
    {
        int total = 0;
        foreach (var s in symbols) total += s.weight;

        int roll = Random.Range(0, total);
        int cumulative = 0;

        foreach (var s in symbols)
        {
            cumulative += s.weight;
            if (roll < cumulative) return s;
        }

        return symbols[symbols.Count - 1];
    }

    /// <summary>
    /// Generates one result per reel using weighted RNG.
    /// Called before spin animation starts — fully fair.
    /// </summary>
    public static SymbolData[] GenerateSpinResult(
        List<SymbolData> symbols, int reelCount)
    {
        var results = new SymbolData[reelCount];
        for (int i = 0; i < reelCount; i++)
            results[i] = GetWeightedRandom(symbols);
        return results;
    }
}