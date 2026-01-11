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

    private int drawsUsed;
    private bool deckLocked;

    [Header("System Failure Rules")]
    public int lifeSupportTurnsBeforeDeath = 2;
    public int buildingIntegrityTurnsBeforeDeath = 1;
    public int powerBlackoutDamagePerTurn = 1;
    [Range(0f, 1f)] public float defenseEventChancePenalty = 0.10f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip drawCardSFX;

    // cached modules
    private StationModule lifeSupport;
    private StationModule power;
    private StationModule comms;
    private StationModule buildingIntegrity;
    private StationModule defense;

    // state
    private bool lsCrisisActive;
    private int lsTurnsLeft;

    private bool biCrisisActive;
    private int biTurnsLeft;

    private bool commsSignalResetDone;

    void Awake()
    {
        if (cameraDirector == null) cameraDirector = FindFirstObjectByType<CameraDirector>();
        if (deck == null) deck = FindFirstObjectByType<DeckManager>();
        if (resolver == null) resolver = FindFirstObjectByType<EventResolver>();
        if (hand == null) hand = FindFirstObjectByType<HandController>();
        if (deckButton == null) deckButton = GetComponent<Button>();
        if (resources == null) resources = FindFirstObjectByType<ResourceManager>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

    }

    void Start()
    {
        baseEventChance = eventChance;
        CacheModules();
        StartDay(1);
        UpdateRuleStates();   // initialize warnings/counters if any module starts disabled
        RefreshStatusUI();
    }

    private void CacheModules()
    {
        if (ModuleRegistry.Instance == null) return;

        if (lifeSupport == null)        lifeSupport        = ModuleRegistry.Instance.Get(ModuleType.LifeSupport);
        if (power == null)              power              = ModuleRegistry.Instance.Get(ModuleType.Power);
        if (comms == null)              comms              = ModuleRegistry.Instance.Get(ModuleType.Comms);
        if (buildingIntegrity == null)  buildingIntegrity  = ModuleRegistry.Instance.Get(ModuleType.BuildingIntegrity);
        if (defense == null)            defense            = ModuleRegistry.Instance.Get(ModuleType.DefenseSystem);
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
        RefreshStatusUI();
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

    private void EndGameWin()
    {
        if (gameEnded) return;
        gameEnded = true;

        LockDeck();
        if (TargetingController.Instance != null)
            TargetingController.Instance.CancelTargeting();

        RefreshStatusUI();

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

        RefreshStatusUI();

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

    private float GetEffectiveEventChance()
    {
        CacheModules();

        float c = eventChance;
        if (defense != null && defense.IsDisabled)
            c += defenseEventChancePenalty;

        return Mathf.Clamp01(c);
    }

    private void RefreshStatusUI()
    {
        if (statusText == null) return;

        if (gameEnded)
        {
            statusText.text = "GAME OVER";
            return;
        }

        CacheModules();

        string msg = "";

        if (lsCrisisActive)
            msg += $"LIFE SUPPORT OFFLINE — {lsTurnsLeft} turns left\n";

        if (biCrisisActive)
            msg += $"BUILDING INTEGRITY FAILED — {biTurnsLeft} turns left\n";

        if (power != null && power.IsDisabled)
            msg += $"POWER OFFLINE — blackout damage each turn\n";

        if (comms != null && comms.IsDisabled)
            msg += $"COMMS OFFLINE — signal reset\n";

        if (defense != null && defense.IsDisabled)
            msg += $"DEFENSE OFFLINE — events +{Mathf.RoundToInt(defenseEventChancePenalty * 100f)}%\n";

        statusText.text = msg.TrimEnd();
    }

    private void UpdateRuleStates()
    {
        CacheModules();

        // Comms: reset signal once per "down" state entry
        bool commsDown = comms != null && comms.IsDisabled;
        if (commsDown)
        {
            if (!commsSignalResetDone && resources != null)
            {
                resources.signal = 0;
                commsSignalResetDone = true;
            }
        }
        else
        {
            commsSignalResetDone = false;
        }

        // Life Support: arm/de-arm countdown
        bool lsDown = lifeSupport != null && lifeSupport.IsDisabled;
        if (lsDown)
        {
            if (!lsCrisisActive)
            {
                lsCrisisActive = true;
                lsTurnsLeft = Mathf.Max(1, lifeSupportTurnsBeforeDeath);
            }
        }
        else
        {
            lsCrisisActive = false;
            lsTurnsLeft = 0;
        }

        // Building Integrity: arm/de-arm countdown
        bool biDown = buildingIntegrity != null && buildingIntegrity.IsDisabled;
        if (biDown)
        {
            if (!biCrisisActive)
            {
                biCrisisActive = true;
                biTurnsLeft = Mathf.Max(1, buildingIntegrityTurnsBeforeDeath);
            }
        }
        else
        {
            biCrisisActive = false;
            biTurnsLeft = 0;
        }

        RefreshStatusUI();
    }

    private void ApplyTurnTicksAfterDraw()
    {
        CacheModules();

        // Power down -> all modules take damage each turn
        if (power != null && power.IsDisabled && powerBlackoutDamagePerTurn > 0)
        {
            if (ModuleRegistry.Instance != null)
            {
                foreach (var m in ModuleRegistry.Instance.All)
                    if (m != null) m.Damage(powerBlackoutDamagePerTurn);
            }
            else
            {
                var modules = FindObjectsByType<StationModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var m in modules)
                    if (m != null) m.Damage(powerBlackoutDamagePerTurn);
            }
        }

        // Re-evaluate after potential cascade damage
        UpdateRuleStates();

        // Countdowns tick ONCE per turn (per draw)
        if (lsCrisisActive)
        {
            // if fixed somehow, UpdateRuleStates would have cleared it
            lsTurnsLeft = Mathf.Max(0, lsTurnsLeft - 1);
            if (lsTurnsLeft <= 0 && (lifeSupport != null && lifeSupport.IsDisabled))
            {
                EndGameLose("Life Support offline too long.");
                return;
            }
        }

        if (biCrisisActive)
        {
            biTurnsLeft = Mathf.Max(0, biTurnsLeft - 1);
            if (biTurnsLeft <= 0 && (buildingIntegrity != null && buildingIntegrity.IsDisabled))
            {
                EndGameLose("Building Integrity failed.");
                return;
            }
        }

        RefreshStatusUI();
    }

    public void OnDeckClicked()
    {
        if (gameEnded) return;

        // Block drawing while player is choosing overflow replacement/trash
        if (hand != null && hand.IsChoosingOverflow) return;

        UpdateRuleStates();

        if (CheckWin())
        {
            EndGameWin();
            return;
        }

        // Day end -> advance day (no draw), but NO "extra pull" for failures
        if (drawsUsed >= maxDrawsThisDay)
        {
            if (biCrisisActive && buildingIntegrity != null && buildingIntegrity.IsDisabled)
            {
                EndGameLose("Building Integrity failed (no turns left).");
                return;
            }

            if (lsCrisisActive && lifeSupport != null && lifeSupport.IsDisabled)
            {
                EndGameLose("Life Support offline (no turns left).");
                return;
            }

            AdvanceDay();
            return;
        }

        if (deckLocked) return;

        LockDeck();

        // play sound
        if (audioSource != null && drawCardSFX != null)
        {
            audioSource.PlayOneShot(drawCardSFX);
        }
            
        CardKind kind;
        float effectiveChance = GetEffectiveEventChance();
        CardData card = deck.DrawRandomByChance(effectiveChance, out kind);
        if (card == null)
        {
            UnlockDeck();
            return;
        }

        // A turn happened
        drawsUsed++;
        RefreshUI();

        // Turn effects (blackout damage, countdown ticks, comms reset, defense modifier via chance)
        ApplyTurnTicksAfterDraw();
        if (gameEnded)
        {
            UnlockDeck();
            return;
        }

        if (kind == CardKind.Event)
        {
            StartCoroutine(ResolveEventAndUnlock(card));
        }
        else
        {
            // If hand is full: start overflow choice and unlock immediately
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

        // Card resolution may repair/break modules, so update rules now
        UpdateRuleStates();

        if (CheckWin())
        {
            EndGameWin();
            yield break;
        }

        if (gameEnded) yield break;

        while ((cameraDirector != null && cameraDirector.IsPanning) ||
               (TargetingController.Instance != null && TargetingController.Instance.IsResolving))
            yield return null;

        if (!gameEnded)
            UnlockDeck();
    }

    public void EvaluateEndConditions()
    {
        if (gameEnded) return;

        // After playing an action, modules might change state -> update rules
        UpdateRuleStates();

        if (CheckWin())
            EndGameWin();
    }

    private IEnumerator UnlockNextFrame()
    {
        yield return null;

        // After drawing an action, nothing changed yet, but keep UI consistent
        UpdateRuleStates();

        if (CheckWin())
        {
            EndGameWin();
            yield break;
        }

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
