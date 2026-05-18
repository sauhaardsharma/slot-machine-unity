using UnityEngine;
using System.Collections;

/// <summary>
/// Animates the BetPanel like a scroll unrolling —
/// expands from centre outward (top and bottom simultaneously)
/// on show, and collapses back to centre on hide.
/// Attach directly to the BetPanel GameObject.
/// </summary>
public class BetPanelAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float showDuration = 0.4f;
    public float hideDuration = 0.28f;

    // The full natural height of the panel set in editor
    // (script reads it automatically on Awake)
    private float fullHeight;
    private float fullAlpha = 1f;

    private RectTransform rectTransform;
    private CanvasGroup   canvasGroup;
    private Coroutine     currentAnim;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup   = GetComponent<CanvasGroup>();

        // If no CanvasGroup, add one automatically
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Record the full natural height defined in the editor
        fullHeight = rectTransform.sizeDelta.y;

        // Start collapsed
        SetHeight(0f);
        canvasGroup.alpha          = 0f;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    /// <summary>Expands panel from centre outward like a scroll unrolling.</summary>
    public void Show()
    {
        gameObject.SetActive(true);
        canvasGroup.blocksRaycasts = true;
        Play(0f, fullHeight, 0f, fullAlpha, showDuration, EaseOutBack);
    }

    /// <summary>Collapses panel back to centre like a scroll rolling up.</summary>
    public void Hide()
    {
        canvasGroup.blocksRaycasts = false;
        Play(fullHeight, 0f, fullAlpha, 0f, hideDuration, EaseInBack,
            onComplete: () => gameObject.SetActive(false));
    }

    private void Play(float fromH, float toH,
                      float fromA, float toA,
                      float duration,
                      System.Func<float, float> easing,
                      System.Action onComplete = null)
    {
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(Animate(fromH, toH, fromA, toA,
                                             duration, easing, onComplete));
    }

    private IEnumerator Animate(float fromH, float toH,
                                float fromA, float toA,
                                float duration,
                                System.Func<float, float> easing,
                                System.Action onComplete)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float et = easing(t);

            SetHeight(Mathf.Lerp(fromH, toH, et));
            canvasGroup.alpha = Mathf.Lerp(fromA, toA, et);

            yield return null;
        }

        SetHeight(toH);
        canvasGroup.alpha = toA;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Sets the panel height while keeping it centred —
    /// both top and bottom edges expand equally from middle.
    /// </summary>
    private void SetHeight(float height)
    {
        Vector2 size = rectTransform.sizeDelta;
        size.y = height;
        rectTransform.sizeDelta = size;
    }

    // ─── EASING ──────────────────────────────────────────────

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private float EaseInBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return c3 * t * t * t - c1 * t * t;
    }
}