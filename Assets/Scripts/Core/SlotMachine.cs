using UnityEngine;
using System.Collections;

/// <summary>
/// Orchestrates all 3 reels.
/// Pre-determines results via weighted RNG before animation.
/// Stops reels with classic stagger (L -> C -> R).
/// Evaluates win by comparing centre prefab names from all 3 reels.
/// </summary>
public class SlotMachine : MonoBehaviour
{
    [Header("Reels")]
    public ReelController reel1;
    public ReelController reel2;
    public ReelController reel3;

    [Header("Configuration")]
    public PayoutTable payoutTable;

    [Tooltip("Minimum spin time before first reel stops")]
    public float minSpinDuration = 2.0f;

    [Tooltip("Delay between each reel stopping")]
    public float reelStopInterval = 0.45f;

    [Tooltip("Wait after last reel stops before evaluating")]
    public float resultEvaluationDelay = 0.8f;

    // ─── STATE ───────────────────────────────────────────────

    private bool         isMachineSpinning = false;
    private SymbolData[] spinResults;
    private string       winningSymbolName = "";

    // ─── EVENTS ──────────────────────────────────────────────

    public System.Action      OnSpinStart;
    public System.Action      OnSpinComplete;
    public System.Action<int> OnWin;
    public System.Action      OnLose;

    // ─── PUBLIC API ──────────────────────────────────────────

    public void Spin()
    {
        if (isMachineSpinning) return;
        StartCoroutine(SpinSequence());
    }

    public bool   IsMachineSpinning  => isMachineSpinning;
    public string GetWinningSymbolName() => winningSymbolName;

    // ─── SPIN SEQUENCE ───────────────────────────────────────

 private IEnumerator SpinSequence()
{
    isMachineSpinning = true;
    winningSymbolName = "";
    OnSpinStart?.Invoke();

    // Step 1: Pre-determine results
    spinResults = RNGSystem.GenerateSpinResult(payoutTable.symbols, 3);

    Debug.Log($"[SlotMachine] Predetermined: " +
              $"{spinResults[0].symbolName} | " +
              $"{spinResults[1].symbolName} | " +
              $"{spinResults[2].symbolName}");

    // Step 2: Start all reels
    reel1.StartSpin(spinResults[0]);
    reel2.StartSpin(spinResults[1]);
    reel3.StartSpin(spinResults[2]);

    // Step 3: Minimum spin duration
    yield return new WaitForSeconds(minSpinDuration);

    // Step 4: Stagger stop (L -> C -> R)
    reel1.StopSpin();
    yield return new WaitForSeconds(reelStopInterval);

    reel2.StopSpin();
    yield return new WaitForSeconds(reelStopInterval);

    reel3.StopSpin();

    // Step 5: Wait until ALL reels have fully stopped
    // This is the real fix — don't use a fixed delay,
    // poll until every reel reports IsSpinning = false
    float timeout = 15f;
    float waited  = 0f;
    while (reel1.IsSpinning || reel2.IsSpinning || reel3.IsSpinning)
    {
        waited += Time.deltaTime;
        if (waited > timeout)
        {
            Debug.LogWarning("[SlotMachine] Timeout waiting for reels to stop!");
            break;
        }
        yield return null;
    }

    // Small buffer after all reels confirm stopped
    yield return new WaitForSeconds(0.1f);

    // Step 6: Evaluate
    EvaluateResult();

    isMachineSpinning = false;
    OnSpinComplete?.Invoke();
}

    private void EvaluateResult()
    {
        // Read centre symbol name directly from each reel
        // These are prefab names e.g. "SymbolSlot_Cherry"
        string n1 = reel1.GetCentreSymbolName();
        string n2 = reel2.GetCentreSymbolName();
        string n3 = reel3.GetCentreSymbolName();

        Debug.Log($"[SlotMachine] Centre symbols: {n1} | {n2} | {n3}");

        int payout = payoutTable.EvaluateWin(n1, n2, n3);

        if (payout > 0)
        {
            // Get display name for popup
            winningSymbolName = payoutTable.GetDisplayName(n1);
            Debug.Log($"[SlotMachine] WIN! {winningSymbolName} x3 -- {payout}x");
            OnWin?.Invoke(payout);
        }
        else
        {
            Debug.Log($"[SlotMachine] No win.");
            OnLose?.Invoke();
        }
    }
}