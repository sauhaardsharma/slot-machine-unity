using UnityEngine;
using System.Collections;

/// <summary>
/// Central audio manager. Singleton.
/// Handles BG music, reel spin loop, win/lose stings, UI sounds.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgMusicSource;   // Looping background music
    public AudioSource reelSource;      // Looping reel spin sound
    public AudioSource sfxSource;       // One-shot SFX (win, lose, UI)

    [Header("Background Music")]
    public AudioClip bgMusicClip;

    [Header("Reel")]
    public AudioClip reelSpinClip;      // Short loop while spinning

    [Header("UI")]
    public AudioClip buttonHoverClip;
    public AudioClip buttonClickClip;

    [Header("Result")]
    public AudioClip jackpotClip;
    public AudioClip loseClip;

    [Header("Settings")]
    [Range(0f, 1f)] public float bgMusicVolume  = 0.4f;
    [Range(0f, 1f)] public float reelVolume      = 0.6f;
    [Range(0f, 1f)] public float sfxVolume       = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Start BG music immediately on game start
        if (bgMusicSource != null && bgMusicClip != null)
        {
            bgMusicSource.clip   = bgMusicClip;
            bgMusicSource.loop   = true;
            bgMusicSource.volume = bgMusicVolume;
            bgMusicSource.Play();
        }

        // Reel source setup (not playing yet)
        if (reelSource != null)
        {
            reelSource.clip   = reelSpinClip;
            reelSource.loop   = true;
            reelSource.volume = reelVolume;
        }
    }

    // ─── REEL ────────────────────────────────────────────────

    public void PlayReelSpin()
    {
        if (reelSource == null || reelSpinClip == null) return;
        if (reelSource.isPlaying) return;
        reelSource.Play();
    }

    public void StopReelSpin()
    {
        if (reelSource == null) return;
        StartCoroutine(FadeOutSource(reelSource, 0.3f));
    }

    // ─── UI ──────────────────────────────────────────────────

    public void PlayHover()
    {
        PlaySFX(buttonHoverClip);
    }

    public void PlayClick()
    {
        PlaySFX(buttonClickClip);
    }

    // ─── RESULT ──────────────────────────────────────────────

    public void PlayJackpot()
    {
        // Duck BG music during jackpot
        StartCoroutine(DuckBGMusic(0.15f, 2.5f));
        PlaySFX(jackpotClip);
    }

    public void PlayLose()
    {
        PlaySFX(loseClip);
    }

    // ─── HELPERS ─────────────────────────────────────────────

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>
    /// Smoothly fades an AudioSource volume to 0 then stops it.
    /// </summary>
    private IEnumerator FadeOutSource(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed     = 0f;

        while (elapsed < duration)
        {
            elapsed       += Time.deltaTime;
            source.volume  = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.Stop();
        source.volume = startVolume; // restore for next spin
    }

    /// <summary>
    /// Ducks BG music to targetVolume for holdDuration then fades back.
    /// </summary>
    private IEnumerator DuckBGMusic(float targetVolume, float holdDuration)
    {
        float original = bgMusicVolume;
        float elapsed  = 0f;
        float duckTime = 0.2f;

        // Fade down
        while (elapsed < duckTime)
        {
            elapsed             += Time.deltaTime;
            bgMusicSource.volume = Mathf.Lerp(original, targetVolume, elapsed / duckTime);
            yield return null;
        }

        yield return new WaitForSeconds(holdDuration);

        // Fade back up
        elapsed = 0f;
        while (elapsed < duckTime)
        {
            elapsed             += Time.deltaTime;
            bgMusicSource.volume = Mathf.Lerp(targetVolume, original, elapsed / duckTime);
            yield return null;
        }

        bgMusicSource.volume = original;
    }
}