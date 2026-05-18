using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// GameManager: Central controller for all game state.
/// Manages balance, bet selection, spin triggering,
/// win/lose responses, game over handling, and all UI state transitions.
/// Singleton pattern ensures only one instance exists.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── SINGLETON ───────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ─── CORE ────────────────────────────────────────────────
    [Header("Core")]
    public SlotMachine slotMachine;
    public int startingBalance = 1000;

    // ─── HUD ─────────────────────────────────────────────────
    [Header("UI - HUD")]
    public TextMeshProUGUI balanceText;
    public TextMeshProUGUI messageText;

    // ─── BET PANEL ───────────────────────────────────────────
    [Header("UI - Bet Panel")]
    public BetPanelAnimator betPanel;
    public Button bet10Button;
    public Button bet50Button;
    public Button bet100Button;
    public Button exitButton;

    // ─── WIN POPUP ───────────────────────────────────────────
    [Header("UI - Win Popup")]
    public GameObject      winPopup;
    public TextMeshProUGUI winAmountText;
    public TextMeshProUGUI winSymbolText;

    // ─── GAME OVER POPUP ─────────────────────────────────────
    [Header("UI - Game Over Popup")]
    public GameObject      gameOverPopup;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI noCoinLeftText;

    // ─── LEVER BALL ──────────────────────────────────────────
[Header("UI - Lever Ball")]
    public LeverBallAnimator leverBall;

    // ─── PRIVATE STATE ───────────────────────────────────────
    private int playerBalance;
    private int currentBet = 10;

    private static readonly int[] BetOptions = { 10, 50, 100 };
    private int betIndex = 0;

    // ─── UNITY LIFECYCLE ─────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        playerBalance = startingBalance;

        // Reset alpha and hide all popups on start
        if (winPopup != null)
        {
            CanvasGroup wcg = winPopup.GetComponent<CanvasGroup>();
            if (wcg != null) wcg.alpha = 0f;
            winPopup.SetActive(false);
        }

        if (gameOverPopup != null)
        {
            CanvasGroup gcg = gameOverPopup.GetComponent<CanvasGroup>();
            if (gcg != null) gcg.alpha = 0f;
            gameOverPopup.SetActive(false);
        }

        UpdateBalanceUI();
        SetMessage("PRESS YOUR LUCK");
        SubscribeEvents();
        ShowBetPanel();
    }

    // ─── EVENT SUBSCRIPTIONS ─────────────────────────────────

    private void SubscribeEvents()
    {
        slotMachine.OnSpinStart    += HandleSpinStart;
        slotMachine.OnSpinComplete += HandleSpinComplete;
        slotMachine.OnWin          += HandleWin;
        slotMachine.OnLose         += HandleLose;
    }

    private void OnDestroy()
    {
        if (slotMachine == null) return;
        slotMachine.OnSpinStart    -= HandleSpinStart;
        slotMachine.OnSpinComplete -= HandleSpinComplete;
        slotMachine.OnWin          -= HandleWin;
        slotMachine.OnLose         -= HandleLose;
    }

    // ─── BET SELECTION ───────────────────────────────────────

    /// <summary>Called by Bet10 button OnClick.</summary>
    public void SelectBet10()  => SelectBet(0);

    /// <summary>Called by Bet50 button OnClick.</summary>
    public void SelectBet50()  => SelectBet(1);

    /// <summary>Called by Bet100 button OnClick.</summary>
    public void SelectBet100() => SelectBet(2);

    /// <summary>
    /// Sets current bet from BetOptions array by index,
    /// hides the bet panel, then triggers spin after short delay.
    /// </summary>
    private void SelectBet(int index)
    {
        betIndex   = index;
        currentBet = BetOptions[betIndex];
        AudioManager.Instance?.PlayClick();
        HideBetPanel();
        StartCoroutine(DelayThen(0.4f, TriggerSpin));
    }

    /// <summary>Cycles to next bet amount. Wired to right arrow button.</summary>
    public void NextBet()
    {
        betIndex   = (betIndex + 1) % BetOptions.Length;
        currentBet = BetOptions[betIndex];
        AudioManager.Instance?.PlayClick();
        SetMessage($"BET: {currentBet}G");
    }

    /// <summary>Cycles to previous bet amount. Wired to left arrow button.</summary>
    public void PreviousBet()
    {
        betIndex   = (betIndex - 1 + BetOptions.Length) % BetOptions.Length;
        currentBet = BetOptions[betIndex];
        AudioManager.Instance?.PlayClick();
        SetMessage($"BET: {currentBet}G");
    }

    // ─── SPIN ────────────────────────────────────────────────

    /// <summary>
    /// Validates balance and triggers spin.
    /// Called directly or after bet selection delay.
    /// </summary>
    public void TriggerSpin()
    {
        if (slotMachine.IsMachineSpinning) return;

        // Hard game over — no coins at all
        if (playerBalance <= 0)
        {
            SetMessage("-- NO COINS LEFT --");
            StartCoroutine(DelayThen(0.5f, ShowGameOverPopup));
            return;
        }

        // Soft block — not enough for selected bet
        if (playerBalance < currentBet)
        {
            SetMessage($"NEED {currentBet}G -- LOWER YOUR BET");
            ShowBetPanel();
            return;
        }

        // Deduct bet before spin starts
        playerBalance -= currentBet;
        UpdateBalanceUI();
        slotMachine.Spin();
    }

    // ─── SPIN EVENT HANDLERS ─────────────────────────────────

    /// <summary>Fired when spin begins. Updates UI and lever state.</summary>
    private void HandleSpinStart()
    {
        SetMessage("-- SPINNING --");
        SetBetButtonsInteractable(false);
        TriggerLeverAnim("Pull");
        AudioManager.Instance?.PlayReelSpin();
    }

    /// <summary>Fired when all reels have fully stopped.</summary>
    private void HandleSpinComplete()
    {
        TriggerLeverAnim("Release");
        AudioManager.Instance?.StopReelSpin();
    }

    /// <summary>
    /// Fired on a winning spin. Calculates payout,
    /// updates balance, and shows win popup.
    /// </summary>
    private void HandleWin(int multiplier)
    {
        int winAmount  = currentBet * multiplier;
        playerBalance += winAmount;
        UpdateBalanceUI();
        SetMessage("-- JACKPOT --");
        ShowWinPopup(winAmount, multiplier);
        AudioManager.Instance?.PlayJackpot();
    }

    /// <summary>
    /// Fired on a losing spin. Checks balance state
    /// then shows appropriate message or game over.
    /// </summary>
 private void HandleLose()
{
    if (playerBalance <= 0)
    {
        SetMessage("-- NO COINS LEFT --");
        StartCoroutine(DelayThen(1.2f, ShowGameOverPopup));
        // Lose audio plays in ShowGameOverPopup, not here
    }
    else
    {
        SetMessage("NO LUCK THIS TIME");
        StartCoroutine(DelayThen(0.6f, ShowBetPanel));
    }
}

    // ─── WIN POPUP ───────────────────────────────────────────

    /// <summary>
    /// Activates and populates the win popup with
    /// amount won and winning symbol info, then fades it in.
    /// </summary>
    private void ShowWinPopup(int amount, int multiplier)
    {
        if (winPopup == null) return;

        // Reset alpha BEFORE SetActive to prevent one-frame flash
        CanvasGroup cg = winPopup.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;

        winPopup.SetActive(true);

        string symbol = slotMachine.GetWinningSymbolName();

        if (winAmountText != null)
            winAmountText.text = $"YOU WIN!\n+{amount} COINS";

        if (winSymbolText != null)
            winSymbolText.text = $"3  x  {symbol.ToUpper()}   ({multiplier}x)";

        StartCoroutine(FadeInCanvasGroup(winPopup));
    }

    /// <summary>Called by Close/Collect button on WinPopup.</summary>
    public void OnWinPopupClose()
    {
        AudioManager.Instance?.PlayClick();
        StartCoroutine(FadeOutCanvasGroup(winPopup, () =>
        {
            winPopup.SetActive(false);
            StartCoroutine(DelayThen(0.25f, ShowBetPanel));
        }));
    }

    // ─── GAME OVER POPUP ─────────────────────────────────────

    /// <summary>
    /// Shows the game over popup with appropriate messaging.
    /// Hides bet panel and win popup before showing.
    /// </summary>
private void ShowGameOverPopup()
{
    if (winPopup != null) winPopup.SetActive(false);
    HideBetPanel();

    if (gameOverPopup == null) return;

    CanvasGroup cg = gameOverPopup.GetComponent<CanvasGroup>();
    if (cg != null) cg.alpha = 0f;

    gameOverPopup.SetActive(true);
    AudioManager.Instance?.PlayLose();

    if (gameOverText != null)
        gameOverText.text = "GAME OVER";

    if (noCoinLeftText != null)
        noCoinLeftText.text = "YOU'VE RUN OUT OF COINS!";

    StartCoroutine(FadeInCanvasGroup(gameOverPopup));
}

    /// <summary>
    /// Called by PlayAgain button on GameOverPopup.
    /// Resets balance and restarts game loop.
    /// </summary>
    public void OnPlayAgainButton()
    {
        AudioManager.Instance?.PlayClick();
        StartCoroutine(FadeOutCanvasGroup(gameOverPopup, () =>
        {
            gameOverPopup.SetActive(false);

            playerBalance = startingBalance;
            betIndex      = 0;
            currentBet    = BetOptions[betIndex];

            UpdateBalanceUI();
            SetMessage($"FRESH START BET RESET TO {currentBet}G");
            StartCoroutine(DelayThen(0.4f, ShowBetPanel));
        }));
    }

    // ─── BET PANEL ───────────────────────────────────────────

    /// <summary>Slides bet panel into view and re-enables bet buttons.</summary>
    public void ShowBetPanel()
    {
        if (betPanel != null) betPanel.Show();
        SetBetButtonsInteractable(true);
        SetMessage("CHOOSE YOUR BET");
    }

    /// <summary>Slides bet panel out of view.</summary>
    public void HideBetPanel()
    {
        if (betPanel != null) betPanel.Hide();
    }

    /// <summary>Exit button — quits game or stops play mode in editor.</summary>
    public void OnExitButton()
    {
        AudioManager.Instance?.PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── UI HELPERS ──────────────────────────────────────────

    /// <summary>Updates balance display text.</summary>
    private void UpdateBalanceUI()
    {
        if (balanceText != null)
            balanceText.text = playerBalance.ToString("N0");
    }

    /// <summary>Updates the message bar text.</summary>
    private void SetMessage(string msg)
    {
        if (messageText != null)
            messageText.text = msg;
    }

    /// <summary>Enables or disables all bet selection buttons.</summary>
    private void SetBetButtonsInteractable(bool state)
    {
        if (bet10Button)  bet10Button.interactable  = state;
        if (bet50Button)  bet50Button.interactable  = state;
        if (bet100Button) bet100Button.interactable = state;
    }

    /// <summary>
    /// Safely triggers lever animator parameter.
    /// Silently skips if animator not yet configured.
    /// </summary>
private void TriggerLeverAnim(string trigger)
{
    if (trigger == "Pull")
        leverBall?.Pull();
    // "Release" is handled automatically by the coroutine after hold duration
}

    // ─── POPUP ANIMATIONS ────────────────────────────────────

    private IEnumerator FadeInCanvasGroup(GameObject popup, float duration = 0.35f)
    {
        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        if (cg == null) yield break;

        cg.alpha      = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        cg.alpha = 1f;
    }

    private IEnumerator FadeOutCanvasGroup(GameObject popup,
        System.Action onComplete, float duration = 0.2f)
    {
        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        cg.alpha      = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        cg.alpha = 0f;
        onComplete?.Invoke();
    }

    private IEnumerator DelayThen(float delay, System.Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }
}