using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class DeckClicker : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text dayText;
    public TMP_Text drawsText;
    public TMP_Text statusText;
    public Button deckButton;

    [Header("Progression")]
    public int day = 1;
    public int signalGoal = 10;

    [Header("Win / Lose Scenes")]
    public bool useScenesForEnd = true;

    public string winSceneName = "WinScene";
    public string loseSceneName = "LoseScene";

    [Tooltip("Optional: delay before switching scenes (seconds).")]
    public float endSceneDelay = 0f;

    private bool gameEnded;

    [Header("Day Rules")]
    public int startingDrawsPerDay = 10;
    public int drawsIncreasePerDay = 1;
    public float eventChanceIncreasePerDay = 0.02f;
    public float maxEventChance = 0.85f;
    private float baseEventChance;

    [Range(0f, 1f)] public float eventChance = 0.35f;
    public int maxDrawsThisDay = 10;

    [Header("Script References")]
    public CameraDirector cameraDirector;
    public DeckManager deck;
    public EventResolver resolver;
    public HandController hand;
    public ResourceManager resources;

    [Header("Module References")]
    public StationModule lifeSupport; // assign in Inspector (recommended)
    public StationModule reactor;     // assign in Inspector (recommended)

    [Header("Lose Rule: Life Support Collapse")]
    public int lifeSupportTurnsBeforeDeath = 2;

    [Header("Reactor Collapse Rule")]
    public int blackoutDamagePerTurn = 1;
    private bool reactorCollapseActive;

    private int drawsUsed;
    private bool deckLocked;

    // Life support rule state
    private bool lsCrisisActive;
    private int lsTurnsLeft;

    void Awake()
    {
        if (cameraDirector == null) cameraDirector = FindFirstObjectByType<CameraDirector>();
        if (deck == null) deck = FindFirstObjectByType<DeckManager>();
        if (resolver == null) resolver = FindFirstObjectByType<EventResolver>();
        if (hand == null) hand = FindFirstObjectByType<HandController>();
        if (deckButton == null) deckButton = GetComponent<Button>();
        if (resources == null) resources = FindFirstObjectByType<ResourceManager>();

        // Optional auto-find fallback:
        if (ModuleRegistry.Instance != null)
        {
            if (lifeSupport == null) lifeSupport = ModuleRegistry.Instance.Get(ModuleType.LifeSupport);
            if (reactor == null)     reactor     = ModuleRegistry.Instance.Get(ModuleType.Reactor); // NEW
        }
    }

    void Start()
    {
        baseEventChance = eventChance;
        StartDay(1);

        // NEW: initialize rule states at start
        UpdateLifeSupportCrisisState();
        UpdateReactorCollapseState();
        RefreshLifeSupportCrisisUI();
    }

    private void StartDay(int newDay)
    {
        day = newDay;
        drawsUsed = 0;

        maxDrawsThisDay = startingDrawsPerDay + (day - 1) * drawsIncreasePerDay;
        eventChance = Mathf.Min(maxEventChance, baseEventChance + (day - 1) * eventChanceIncreasePerDay);

        if (TargetingController.Instance != null)
            TargetingController.Instance.CancelTargeting();

        RefreshUI();
    }

    private void AdvanceDay()
    {
        StartDay(day + 1);
    }

    // DeckClicker.cs (add these methods anywhere in the class)
    private bool CheckWin()
    {
        if (resources == null) return false;
        return resources.signal >= signalGoal;
    }

    private void EndGameWin()
    {
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log("WIN: Signal goal reached.");

        LockDeck();
        if (TargetingController.Instance != null)
            TargetingController.Instance.CancelTargeting();

        if (useScenesForEnd)
            StartCoroutine(LoadEndScene(winSceneName));
    }

    private void EndGameLose(string reason)
    {
        if (gameEnded) return;
        gameEnded = true;

        Debug.Log($"LOSE: {reason}");

        LockDeck();
        if (TargetingController.Instance != null)
            TargetingController.Instance.CancelTargeting();

        if (useScenesForEnd)
            StartCoroutine(LoadEndScene(loseSceneName));
    }

    private IEnumerator LoadEndScene(string sceneName)
    {
        if (endSceneDelay > 0f)
            yield return new WaitForSeconds(endSceneDelay);

        if (!string.IsNullOrWhiteSpace(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    private void RefreshUI()
    {
        if (dayText != null)
            dayText.text = $"Day {day}";

        if (drawsText != null)
            drawsText.text = $"{drawsUsed}/{maxDrawsThisDay}";
    }

    // --- Life Support rule ---
    private bool IsLifeSupportDown()
    {
        if (lifeSupport == null && ModuleRegistry.Instance != null)
            lifeSupport = ModuleRegistry.Instance.Get(ModuleType.LifeSupport);

        return lifeSupport != null && lifeSupport.Health <= 0;
    }

    private void RefreshLifeSupportCrisisUI()
    {
        if (statusText == null) return;

        if (gameEnded)
        {
            statusText.text = "GAME OVER";
            return;
        }

        if (lsCrisisActive)
            statusText.text = $"LIFE SUPPORT OFFLINE — {lsTurnsLeft} turns left";
        else
            statusText.text = "";
    }

    private void UpdateLifeSupportCrisisState()
    {
        if (resources == null) return;

        bool down = IsLifeSupportDown();

        if (down)
        {
            resources.oxygen = 0;

            if (!lsCrisisActive)
            {
                lsCrisisActive = true;
                lsTurnsLeft = Mathf.Max(1, lifeSupportTurnsBeforeDeath);
                RefreshLifeSupportCrisisUI();
            }
        }
        else
        {
            if (lsCrisisActive)
            {
                lsCrisisActive = false;
                lsTurnsLeft = 0;
                RefreshLifeSupportCrisisUI();
            }
        }
    }

    private void TickLifeSupportCrisisOnTurn()
    {
        if (!lsCrisisActive) return;

        if (!IsLifeSupportDown())
        {
            lsCrisisActive = false;
            lsTurnsLeft = 0;
            RefreshLifeSupportCrisisUI();
            return;
        }

        lsTurnsLeft = Mathf.Max(0, lsTurnsLeft - 1);
        RefreshLifeSupportCrisisUI();

        if (lsTurnsLeft <= 0)
            EndGameLose("Life Support offline for too long.");
    }
    // --- end Life Support rule ---

    // --- Reactor rule ---
    private bool IsReactorDown()
    {
        if (reactor == null && ModuleRegistry.Instance != null)
            reactor = ModuleRegistry.Instance.Get(ModuleType.Reactor); // NEW fallback

        return reactor != null && reactor.Health <= 0;
    }

    private void UpdateReactorCollapseState()
    {
        if (resources == null) return;

        if (IsReactorDown())
        {
            resources.power = 0;           // lock power to 0 while reactor is dead
            reactorCollapseActive = true;
        }
        else
        {
            reactorCollapseActive = false;
        }
    }

    private void TickReactorBlackoutOnTurn()
    {
        if (!reactorCollapseActive) return;
        if (resources == null || resources.power > 0) return;
        if (blackoutDamagePerTurn <= 0) return;

        if (ModuleRegistry.Instance != null)
        {
            foreach (var m in ModuleRegistry.Instance.All)
                if (m != null) m.Damage(blackoutDamagePerTurn);
        }
        else
        {
            var modules = FindObjectsByType<StationModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in modules)
                if (m != null) m.Damage(blackoutDamagePerTurn);
        }
    }
    // --- end Reactor rule ---

    public void OnDeckClicked()
    {
        if (gameEnded) return;

        // Block drawing while player is choosing overflow replacement/trash
        if (hand != null && hand.IsChoosingOverflow) return;

        // Keep rules updated before doing anything
        UpdateLifeSupportCrisisState();
        UpdateReactorCollapseState();

        if (CheckWin())
        {
            EndGameWin();
            return;
        }

        // HARD STOP: if day is over, switch days (and do NOT draw)
        if (drawsUsed >= maxDrawsThisDay)
        {
            AdvanceDay();
            return;
        }

        if (deckLocked) return;

        // LOCK IMMEDIATELY
        LockDeck();

        CardKind kind;
        CardData card = deck.DrawRandomByChance(eventChance, out kind);
        if (card == null)
        {
            UnlockDeck();
            return;
        }

        // Successful draw = a turn happened
        drawsUsed++;
        RefreshUI();

        // Apply turn-ticks AFTER the draw
        UpdateLifeSupportCrisisState();
        UpdateReactorCollapseState();

        TickLifeSupportCrisisOnTurn();
        if (gameEnded)
        {
            UnlockDeck();
            return;
        }

        TickReactorBlackoutOnTurn();
        if (gameEnded)
        {
            UnlockDeck();
            return;
        }

        // Re-clamp after damage tick (reactor might already be dead; ensures power stays 0)
        UpdateLifeSupportCrisisState();
        UpdateReactorCollapseState();

        if (kind == CardKind.Event)
        {
            StartCoroutine(ResolveEventAndUnlock(card));
        }
        else
        {
            // If hand is full: start overflow choice and UNLOCK NOW (do not run UnlockNextFrame)
            if (!hand.AddCard(card))
            {
                hand.StartOverflowChoice(card);
                UnlockDeck();
                return;
            }

            StartCoroutine(UnlockNextFrame());
        }
    }


    private IEnumerator ResolveEventAndUnlock(CardData card)
    {
        yield return resolver.Resolve(card);

        if (CheckWin())
        {
            EndGameWin();
            yield break;
        }

        if (gameEnded) yield break;

        UpdateLifeSupportCrisisState();
        UpdateReactorCollapseState();

        while ((cameraDirector != null && cameraDirector.IsPanning) ||
            (TargetingController.Instance != null && TargetingController.Instance.IsResolving))
            yield return null;

        if (!gameEnded)
            UnlockDeck();
    }

    public void EvaluateEndConditions()
    {
        if (gameEnded) return;

        if (CheckWin())
            EndGameWin();
    }

    private IEnumerator UnlockNextFrame()
    {
        yield return null;

        if (CheckWin())
        {
            EndGameWin();
            yield break;
        }

        UpdateLifeSupportCrisisState();
        UpdateReactorCollapseState();

        if (!gameEnded)
            UnlockDeck();
    }

    private void LockDeck()
    {
        deckLocked = true;
        if (deckButton != null) deckButton.interactable = false;
    }

    private void UnlockDeck()
    {
        deckLocked = false;
        if (deckButton != null) deckButton.interactable = true;
    }
}