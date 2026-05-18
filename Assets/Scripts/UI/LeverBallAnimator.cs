using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the lever ball visual state.
/// Swaps between LeverBallNormal and LeverBallPulled GameObjects.
/// Call Pull() from GameManager when spin starts.
/// </summary>
public class LeverBallAnimator : MonoBehaviour
{
    [Header("Lever Ball States")]
    public GameObject leverBallNormal;   // Drag LeverBallNormal here
    public GameObject leverBallPulled;   // Drag LeverBallPulled here

    [Header("Timing")]
    public float pulledHoldDuration = 1.0f;  // How long to stay in pulled state

    private Coroutine pullRoutine;

    private void Start()
    {
        SetNormal();
    }

    /// <summary>
    /// Call this when a bet is selected / spin starts.
    /// Switches to pulled, waits, then returns to normal.
    /// </summary>
    public void Pull()
    {
        // Cancel any in-progress pull before starting a new one
        if (pullRoutine != null) StopCoroutine(pullRoutine);
        pullRoutine = StartCoroutine(PullRoutine());
    }

    private IEnumerator PullRoutine()
    {
        SetPulled();
        yield return new WaitForSeconds(pulledHoldDuration);
        SetNormal();
        pullRoutine = null;
    }

    private void SetNormal()
    {
        if (leverBallNormal != null) leverBallNormal.SetActive(true);
        if (leverBallPulled != null) leverBallPulled.SetActive(false);
    }

    private void SetPulled()
    {
        if (leverBallNormal != null) leverBallNormal.SetActive(false);
        if (leverBallPulled != null) leverBallPulled.SetActive(true);
    }
}