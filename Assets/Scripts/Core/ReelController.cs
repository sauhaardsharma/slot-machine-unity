using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Prefab-based reel controller.
/// Spawns symbol prefabs in a shuffled order,
/// scrolls them downward, and snaps the target
/// symbol to the centre slot on stop.
///
/// Visible layout:
/// [Top symbol]    — 70% alpha (partially visible)
/// [Centre symbol] — 100% alpha (payline)
/// [Bottom symbol] — 70% alpha (partially visible)
/// </summary>
public class ReelController : MonoBehaviour
{
    // ─── INSPECTOR ───────────────────────────────────────────

    [Header("Reel Configuration")]
    public float symbolHeight = 125f;

    [Header("Symbol Prefabs")]
    [Tooltip("Drag all 4 symbol prefabs here")]
    public List<GameObject> symbolPrefabs = new List<GameObject>();

    [Header("Spin Settings")]
    public float accelerationTime = 0.3f;
    public float decelerationTime = 0.6f;
    public float maxSpinSpeed     = 1500f;
    public float bounceAmount     = 8f;

    [Header("References")]
    public RectTransform reelViewport;  // SymbolContainer
    public PayoutTable   payoutTable;

    // ─── PRIVATE ─────────────────────────────────────────────

    // All spawned symbol GameObjects on this reel
    private List<GameObject> spawnedSymbols = new List<GameObject>();

    // Prefab name of each spawned symbol (parallel to spawnedSymbols)
    private List<string> spawnedNames = new List<string>();

    // How many symbols we spawn (enough to fill reel + buffers)
    private const int SPAWN_COUNT = 12;

    // The predetermined winning symbol for this spin
    private SymbolData targetSymbol;

    // Current scroll position
    private float scrollY = 0f;

    // Total height of all spawned symbols
    private float TotalHeight => SPAWN_COUNT * symbolHeight;

    // State
    private bool isSpinning = false;
    private bool stopCalled  = false;

    // Index of the symbol currently in centre slot
    private int centreSymbolIndex = 0;

    // ─── INIT ────────────────────────────────────────────────

    private void Start()
    {
        SpawnSymbols();
        UpdateAlpha();
    }

    /// <summary>
    /// Spawns SPAWN_COUNT symbol prefabs in a randomized order.
    /// Each reel gets a different random sequence.
    /// Positioned top-to-bottom starting at Y=0.
    /// </summary>
    private void SpawnSymbols()
    {
        // Clear existing
        foreach (Transform t in reelViewport) Destroy(t.gameObject);
        spawnedSymbols.Clear();
        spawnedNames.Clear();

        // Build a shuffled sequence from available prefabs
        List<GameObject> pool = new List<GameObject>();

        // Fill pool with enough symbols (repeat to reach SPAWN_COUNT)
        while (pool.Count < SPAWN_COUNT)
        {
            // Add all prefabs in shuffled order
            List<GameObject> batch = new List<GameObject>(symbolPrefabs);
            ShuffleList(batch);
            pool.AddRange(batch);
        }

        // Trim to exact count
        while (pool.Count > SPAWN_COUNT) pool.RemoveAt(pool.Count - 1);

        // Spawn
        for (int i = 0; i < SPAWN_COUNT; i++)
        {
            GameObject prefab = pool[i];
            GameObject go     = Instantiate(prefab, reelViewport);
            go.name           = prefab.name; // keep exact prefab name

            RectTransform rt  = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();

            // Anchor top-center
            rt.anchorMin        = new Vector2(0.5f, 1f);
            rt.anchorMax        = new Vector2(0.5f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(140f, symbolHeight);

            // Stack downward: symbol 0 at top, each one below
            rt.anchoredPosition = new Vector2(0f, -i * symbolHeight);

            spawnedSymbols.Add(go);
            spawnedNames.Add(prefab.name);
        }

        // Centre symbol starts at index 1 (middle of first 3)
        centreSymbolIndex = 1;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j    = Random.Range(0, i + 1);
            T   tmp  = list[i];
            list[i]  = list[j];
            list[j]  = tmp;
        }
    }

    // ─── ALPHA CONTROL ───────────────────────────────────────

    /// <summary>
    /// Updates symbol alpha based on their visible position.
    /// Centre = 100%, top and bottom = 70%, others = 0%.
    /// Creates the effect of symbols fading at edges.
    /// </summary>
    private void UpdateAlpha()
    {
        for (int i = 0; i < spawnedSymbols.Count; i++)
        {
            Image img = spawnedSymbols[i].GetComponentInChildren<Image>();
            if (img == null) continue;

            int offset = i - centreSymbolIndex;
            float alpha;

            switch (Mathf.Abs(offset))
            {
                case 0:  alpha = 1.0f;  break; // centre = full
                case 1:  alpha = 0.7f;  break; // adjacent = 70%
                default: alpha = 0.0f;  break; // others = invisible
            }

            Color c = img.color;
            c.a     = alpha;
            img.color = c;
        }
    }

    // ─── SPIN ────────────────────────────────────────────────

    public void StartSpin(SymbolData result)
    {
        if (isSpinning) return;
        targetSymbol = result;
        stopCalled   = false;
        isSpinning   = true;
        StartCoroutine(SpinRoutine());
    }

    public void StopSpin() => stopCalled = true;

    private IEnumerator SpinRoutine()
    {
        // ── Phase 1: Accelerate ──────────────────────────────
        float e = 0f;
        while (e < accelerationTime)
        {
            e += Time.deltaTime;
            float speed = Mathf.Lerp(0f, maxSpinSpeed, e / accelerationTime);
            MoveSymbols(speed * Time.deltaTime);
            yield return null;
        }

        // ── Phase 2: Full speed until stop called ───────────
        while (!stopCalled)
        {
            MoveSymbols(maxSpinSpeed * Time.deltaTime);
            yield return null;
        }

        // ── Phase 3: Decelerate to target ───────────────────
        yield return StartCoroutine(DecelerateToTarget());

        // ── Phase 4: Hard snap guarantee ────────────────────
        SnapToCentre();

        // ── Phase 5: Bounce ──────────────────────────────────
        yield return StartCoroutine(DoBounce());

        isSpinning = false;

        Debug.Log($"[Reel:{name}] Final centre: {spawnedNames[centreSymbolIndex]}");
    }

    /// <summary>
    /// Moves all symbols downward by deltaY.
    /// When a symbol moves below the visible area,
    /// it gets recycled to the top.
    /// </summary>
    private void MoveSymbols(float deltaY)
    {
        scrollY += deltaY;

        // Move all symbols down
        for (int i = 0; i < spawnedSymbols.Count; i++)
        {
            RectTransform rt = spawnedSymbols[i].GetComponent<RectTransform>();
            Vector2 pos      = rt.anchoredPosition;
            pos.y           -= deltaY;
            rt.anchoredPosition = pos;
        }

        // Recycle: if any symbol goes below bottom of reel,
        // move it to the top
        float bottomLimit = -(symbolHeight * (SPAWN_COUNT - 1));

        for (int i = 0; i < spawnedSymbols.Count; i++)
        {
            RectTransform rt = spawnedSymbols[i].GetComponent<RectTransform>();

            if (rt.anchoredPosition.y < -(symbolHeight * 3))
            {
                // Find topmost symbol position
                float topY = float.MinValue;
                foreach (var sym in spawnedSymbols)
                {
                    float y = sym.GetComponent<RectTransform>().anchoredPosition.y;
                    if (y > topY) topY = y;
                }

                // Place this symbol above the topmost
                rt.anchoredPosition = new Vector2(0f, topY + symbolHeight);

                // Cycle its name in the list too
                string name = spawnedNames[i];
                spawnedNames.RemoveAt(i);
                spawnedNames.Insert(0, name);

                GameObject go = spawnedSymbols[i];
                spawnedSymbols.RemoveAt(i);
                spawnedSymbols.Insert(0, go);
            }
        }

        // Update which index is closest to centre Y
        UpdateCentreIndex();
        UpdateAlpha();
    }

    /// <summary>
    /// Finds which symbol is currently closest to the centre
    /// slot Y position (-symbolHeight = centre of 3 visible).
    /// </summary>
    private void UpdateCentreIndex()
    {
        float centreY    = -symbolHeight; // centre slot Y in reel space
        float closestDist = float.MaxValue;
        int   closestIdx  = 0;

        for (int i = 0; i < spawnedSymbols.Count; i++)
        {
            RectTransform rt = spawnedSymbols[i].GetComponent<RectTransform>();
            float dist = Mathf.Abs(rt.anchoredPosition.y - centreY);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIdx  = i;
            }
        }

        centreSymbolIndex = closestIdx;
    }

    /// <summary>
    /// Decelerates while scrolling until target prefab
    /// is closest to centre position.
    /// Guarantees at least 1 full rotation before stopping.
    /// </summary>
 private IEnumerator DecelerateToTarget()
{
    // Safety timeout — never hang longer than 10 seconds
    float timeout  = 10f;
    float elapsed  = 0f;
    float minTravelDist = TotalHeight; // at least one full rotation
    float travelled = 0f;

    // Phase A: Spin at full speed for minimum distance
    while (travelled < minTravelDist)
    {
        float step = maxSpinSpeed * Time.deltaTime;
        MoveSymbols(step);
        travelled += step;
        elapsed   += Time.deltaTime;

        if (elapsed > timeout)
        {
            Debug.LogWarning($"[Reel:{name}] Timeout in phase A!");
            break;
        }
        yield return null;
    }

    // Phase B: Slow down while searching for target
    elapsed = 0f;
    float slowSpeed = maxSpinSpeed;

    while (elapsed < timeout)
    {
        // Check if target is now in centre
        if (spawnedNames[centreSymbolIndex] == targetSymbol.prefabName)
            break;

        // Gradually slow down
        slowSpeed  = Mathf.Lerp(slowSpeed, 100f, Time.deltaTime * 2f);
        float step = slowSpeed * Time.deltaTime;
        MoveSymbols(step);
        elapsed   += Time.deltaTime;
        yield return null;
    }

    // If still not found — force it
    if (spawnedNames[centreSymbolIndex] != targetSymbol.prefabName)
    {
        Debug.LogWarning($"[Reel:{name}] Target not found after timeout. Forcing snap.");
    }
}

    /// <summary>
    /// Hard snaps target symbol to exact centre Y position.
    /// Guarantees pixel-perfect alignment regardless of float drift.
    /// </summary>
private void SnapToCentre()
{
    float centreY     = -symbolHeight;
    int   targetIdx   = -1;
    float closestDist = float.MaxValue;

    // Find the matching symbol CLOSEST to centre — not just first match
    // This handles duplicate symbols on the reel correctly
    for (int i = 0; i < spawnedNames.Count; i++)
    {
        if (spawnedNames[i] != targetSymbol.prefabName) continue;

        float y    = spawnedSymbols[i].GetComponent<RectTransform>().anchoredPosition.y;
        float dist = Mathf.Abs(y - centreY);

        if (dist < closestDist)
        {
            closestDist = dist;
            targetIdx   = i;
        }
    }

    if (targetIdx < 0)
    {
        Debug.LogWarning($"[Reel:{name}] Target {targetSymbol.prefabName} not found!");
        return;
    }

    // Snap that symbol exactly to centre Y
    RectTransform targetRT = spawnedSymbols[targetIdx].GetComponent<RectTransform>();
    float offset           = centreY - targetRT.anchoredPosition.y;

    foreach (var sym in spawnedSymbols)
    {
        RectTransform rt    = sym.GetComponent<RectTransform>();
        rt.anchoredPosition += new Vector2(0f, offset);
    }

    centreSymbolIndex = targetIdx;
    UpdateAlpha();

    Debug.Log($"[Reel:{name}] Snapped: {spawnedNames[centreSymbolIndex]}");
}

    private IEnumerator DoBounce()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.08f;
            float offset = Mathf.Lerp(0f, -bounceAmount, Mathf.Clamp01(t));
            foreach (var sym in spawnedSymbols)
            {
                RectTransform rt = sym.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(
                    rt.anchoredPosition.x,
                    rt.anchoredPosition.y + offset * Time.deltaTime * 10f);
            }
            yield return null;
        }

        // Re-snap after bounce
        SnapToCentre();
    }

    // ─── PUBLIC ──────────────────────────────────────────────

    public bool IsSpinning => isSpinning;

    /// <summary>
    /// Returns the prefab name of the symbol currently in centre.
    /// Called by SlotMachine after spin completes.
    /// Always correct — set by SnapToCentre() which runs last.
    /// </summary>
public string GetCentreSymbolName()
{
    if (spawnedNames.Count == 0) return "";

    // Double-check: return the name of whichever symbol is
    // physically closest to centre Y right now
    // This is the ground truth — ignores centreSymbolIndex entirely
    float centreY     = -symbolHeight;
    float closestDist = float.MaxValue;
    int   closestIdx  = 0;

    for (int i = 0; i < spawnedSymbols.Count; i++)
    {
        float y    = spawnedSymbols[i].GetComponent<RectTransform>().anchoredPosition.y;
        float dist = Mathf.Abs(y - centreY);
        if (dist < closestDist)
        {
            closestDist = dist;
            closestIdx  = i;
        }
    }

    return spawnedNames[closestIdx];
}
}