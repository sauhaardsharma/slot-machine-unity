using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Holds all symbol data and evaluates win conditions.
/// Win is determined by matching prefab names from all 3 reels.
/// </summary>
[CreateAssetMenu(fileName = "PayoutTable", menuName = "SlotMachine/Payout Table")]
public class PayoutTable : ScriptableObject
{
    [Header("All Symbols")]
    public List<SymbolData> symbols = new List<SymbolData>();

    /// <summary>
    /// Evaluates win by comparing centre symbol names from all 3 reels.
    /// Returns payout multiplier if all 3 match, 0 if no win.
    /// </summary>
    public int EvaluateWin(string name1, string name2, string name3)
    {
        if (name1 == name2 && name2 == name3)
        {
            // Find matching symbol data
            foreach (SymbolData s in symbols)
            {
                if (s.prefabName == name1)
                {
                    Debug.Log($"[PayoutTable] WIN: {s.symbolName} x3 " +
                              $"-- {s.payoutMultiplier}x payout");
                    return s.payoutMultiplier;
                }
            }
        }
        return 0;
    }

    /// <summary>
    /// Returns display name for a given prefab name.
    /// Used for win popup text.
    /// </summary>
    public string GetDisplayName(string prefabName)
    {
        foreach (SymbolData s in symbols)
        {
            if (s.prefabName == prefabName)
                return s.symbolName;
        }
        return prefabName;
    }
}