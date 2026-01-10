using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public StationModule lifeSupport;    // assign in Inspector (recommended)

    [Header("Lose Rule: Life Support Collapse")]
    public int lifeSupportTurnsBeforeDeath = 2;

    private int drawsUsed;
    private bool deckLocked;

    // Life support rule state
    private bool lsCrisisActive;
    private int lsTurnsLeft;

    private bool gameEnded;

    void Awake()
    {
        if (cameraDirector == null) cameraDirector = FindFirstObjectByType<CameraDirector>();
        if (deck == null) deck = FindFirstObjectByType<DeckManager>();
        if (resolver == null) resolver = FindFirstObjectByType<EventResolver>();
        if (hand == null) hand = FindFirstObjectByType<HandController>();
        if (deckButton == null) deckButton = GetComponent<Button>();
        if (resources == null) resources = FindFirstObjectByType<ResourceManager>();
        // lifeSupport: ideally assign in Inspector; optional auto-find fallback:
        if (lifeSupport == null && ModuleRegistry.Instance != null)
            lifeSupport = ModuleRegistry.Instance.Get(ModuleType.LifeSupport);
    }

    void Start()
    {
        baseEventChance = eventChance;
        StartDay(1);
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

    private bool CheckWin()
    {
        if (resources == null) return false;
        return resources.signal >= signalGoal;
    }

    private void EndGameLose(string reason)
    {
        gameEnded = true;
        Debug.Log($"GAME OVER: {reason}");
        if (statusText != null) statusText.text = "GAME OVER";
        LockDeck();
    }

    private void RefreshUI()
    {
        if (dayText != null)
            dayText.text = $"Day {day}";

        if (drawsText != null)
            drawsText.text = $"{drawsUsed}/{maxDrawsThisDay}";
    }

    // --- Life Support rule (core) ---
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
            // immediate effect: oxygen forced to 0 while LS is down
            resources.oxygen = 0;

            // on-enter: start countdown once
            if (!lsCrisisActive)
            {
                lsCrisisActive = true;
                lsTurnsLeft = Mathf.Max(1, lifeSupportTurnsBeforeDeath);
                RefreshLifeSupportCrisisUI();
            }
        }
        else
        {
            // on-exit: cancel countdown if repaired
            if (lsCrisisActive)
            {
                lsCrisisActive = false;
                lsTurnsLeft = 0;
                RefreshLifeSupportCrisisUI();
            }
        }
    }

    // Call ONLY when a card was successfully drawn (a "turn" happened)
    private void TickLifeSupportCrisisOnTurn()
    {
        if (!lsCrisisActive) return;

        // If they repaired LS before this draw, cancel
        if (!IsLifeSupportDown())
        {
            lsCrisisActive = false;
            lsTurnsLeft = 0;
            RefreshLifeSupportCrisisUI();
            return;
        }

        lsTurnsLeft = Mathf.Max(0, lsTurnsLeft - 1);
        RefreshLifeSupportCrisisUI();

        // Two turns means: after the 2nd successful draw, turns hits 0 and you lose immediately.
        if (lsTurnsLeft <= 0)
        {
            EndGameLose("Life Support offline for too long.");
        }
    }
    // --- end Life Support rule ---

    public void OnDeckClicked()
    {
        if (gameEnded) return;

        // Keep rule state up-to-date before doing anything
        UpdateLifeSupportCrisisState();

        if (CheckWin())
        {
            Debug.Log("WIN: Signal goal reached.");
            return;
        }

        if (drawsUsed >= maxDrawsThisDay)
        {
            AdvanceDay();
            return;
        }

        if (deckLocked) return;

        // LOCK IMMEDIATELY (prevents same-frame spam)
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

        // Tick life support countdown AFTER the draw (so they still get the "two more turns")
        TickLifeSupportCrisisOnTurn();
        if (gameEnded) return;

        if (kind == CardKind.Event)
        {
            StartCoroutine(ResolveEventAndUnlock(card));
        }
        else
        {
            hand.AddCard(card);
            StartCoroutine(UnlockNextFrame());
        }
    }

    private IEnumerator ResolveEventAndUnlock(CardData card)
    {
        yield return resolver.Resolve(card);

        // Rule might change after events resolve (repairs/damage)
        UpdateLifeSupportCrisisState();

        while ((cameraDirector != null && cameraDirector.IsPanning) ||
               (TargetingController.Instance != null && TargetingController.Instance.IsResolving))
        {
            yield return null;
        }

        if (!gameEnded)
            UnlockDeck();
    }

    private IEnumerator UnlockNextFrame()
    {
        yield return null;

        // Rule might change after drawing action (no resolve yet, but keep consistent)
        UpdateLifeSupportCrisisState();

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
