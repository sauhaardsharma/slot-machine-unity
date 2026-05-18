using UnityEngine;

/// <summary>
/// ScriptableObject holding data for a single slot symbol.
/// prefabName MUST match the prefab GameObject name exactly.
/// </summary>
[CreateAssetMenu(fileName = "SymbolData", menuName = "SlotMachine/Symbol Data")]
public class SymbolData : ScriptableObject
{
    [Header("Symbol Identity")]
    public string symbolName;       // Display name e.g. "Cherry"
    public string prefabName;       // Must match prefab name e.g. "SymbolSlot_Cherry"
    public Sprite symbolSprite;     // Not used for spawning but keep for reference

    [Header("Payout")]
    public int payoutMultiplier;    // e.g. Cherry=2, Bell=5, Bar=10, Seven=25

    [Header("RNG Weight")]
    [Range(1, 100)]
    public int weight = 10;
}