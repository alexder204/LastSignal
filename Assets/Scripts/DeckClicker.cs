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

    // === SYSTEM AGGREGATES (THE TRUTH) ===
    private SystemAggregate lifeSupport;
    private SystemAggregate power;
    private SystemAggregate comms;
    private SystemAggregate buildingIntegrity;
    private SystemAggregate defense;

    // === STATE ===
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
    }

    void Start()
    {
        baseEventChance = eventChance;
        CacheAggregates();
        StartDay(1);
        UpdateRuleStates();
        RefreshStatusUI();
    }

    // 🔴 IMPORTANT: each aggregate should live on a GameObject
    // named after its system (LifeSupport, Power, Comms, etc.)
    private void CacheAggregates()
    {
        if (lifeSupport == null)
            lifeSupport = GameObject.Find("LifeSupport")?.GetComponent<SystemAggregate>();

        if (power == null)
            power = GameObject.Find("Power")?.GetComponent<SystemAggregate>();

        if (comms == null)
            comms = GameObject.Find("Comms")?.GetComponent<SystemAggregate>();

        if (buildingIntegrity == null)
            buildingIntegrity = GameObject.Find("BuildingIntegrity")?.GetComponent<SystemAggregate>();

        if (defense == null)
            defense = GameObject.Find("DefenseSystem")?.GetComponent<SystemAggregate>();
    }

    private void StartDay(int newDay)
    {
        day = newDay;
        drawsUsed = 0;

        maxDrawsThisDay = startingDrawsPerDay + (day - 1) * drawsIncreasePerDay;
        eventChance = Mathf.Min(maxEventChance, baseEventChance + (day - 1) * eventChanceIncreasePerDay);

        TargetingController.Instance?.CancelTargeting();

        RefreshUI();
        RefreshStatusUI();
    }

    private void AdvanceDay() => StartDay(day + 1);

    private bool CheckWin()
    {
        return resources != null && resources.signal >= signalGoal;
    }

    private float GetEffectiveEventChance()
    {
        CacheAggregates();

        float c = eventChance;
        if (defense != null && defense.IsDead)
            c += defenseEventChancePenalty;

        return Mathf.Clamp01(c);
    }

    private void UpdateRuleStates()
    {
        CacheAggregates();

        // === COMMS ===
        if (comms != null && comms.IsDead)
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

        // === LIFE SUPPORT ===
        if (lifeSupport != null && lifeSupport.IsDead)
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

        // === BUILDING INTEGRITY ===
        if (buildingIntegrity != null && buildingIntegrity.IsDead)
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
        CacheAggregates();

        // === POWER BLACKOUT ===
        if (power != null && power.IsDead && powerBlackoutDamagePerTurn > 0)
        {
            foreach (var m in ModuleRegistry.Instance.All)
                if (m != null)
                    m.Damage(powerBlackoutDamagePerTurn);
        }

        UpdateRuleStates();

        if (lsCrisisActive)
        {
            lsTurnsLeft--;
            if (lsTurnsLeft <= 0 && lifeSupport.IsDead)
            {
                EndGameLose("Life Support offline too long.");
                return;
            }
        }

        if (biCrisisActive)
        {
            biTurnsLeft--;
            if (biTurnsLeft <= 0 && buildingIntegrity.IsDead)
            {
                EndGameLose("Building Integrity failed.");
                return;
            }
        }

        RefreshStatusUI();
    }

    private void RefreshUI()
    {
        if (dayText) dayText.text = $"Day {day}";
        if (drawsText) drawsText.text = $"{drawsUsed}/{maxDrawsThisDay}";
    }

    private void RefreshStatusUI()
    {
        if (statusText == null) return;

        if (gameEnded)
        {
            statusText.text = "GAME OVER";
            return;
        }

        string msg = "";

        if (lsCrisisActive)
            msg += $"LIFE SUPPORT OFFLINE — {lsTurnsLeft} turns left\n";

        if (biCrisisActive)
            msg += $"BUILDING INTEGRITY FAILED — {biTurnsLeft} turns left\n";

        if (power != null && power.IsDead)
            msg += "POWER OFFLINE — blackout damage each turn\n";

        if (comms != null && comms.IsDead)
            msg += "COMMS OFFLINE — signal reset\n";

        if (defense != null && defense.IsDead)
            msg += $"DEFENSE OFFLINE — events +{Mathf.RoundToInt(defenseEventChancePenalty * 100f)}%\n";

        statusText.text = msg.TrimEnd();
    }

    public void OnDeckClicked()
    {
        if (gameEnded) return;
        if (hand != null && hand.IsChoosingOverflow) return;

        UpdateRuleStates();

        if (CheckWin())
        {
            EndGameWin();
            return;
        }

        if (drawsUsed >= maxDrawsThisDay)
        {
            if (biCrisisActive && buildingIntegrity.IsDead)
            {
                EndGameLose("Building Integrity failed (no turns left).");
                return;
            }

            if (lsCrisisActive && lifeSupport.IsDead)
            {
                EndGameLose("Life Support offline (no turns left).");
                return;
            }

            AdvanceDay();
            return;
        }

        if (deckLocked) return;

        LockDeck();

        CardKind kind;
        CardData card = deck.DrawRandomByChance(GetEffectiveEventChance(), out kind);
        if (card == null)
        {
            UnlockDeck();
            return;
        }

        drawsUsed++;
        RefreshUI();

        ApplyTurnTicksAfterDraw();
        if (gameEnded)
        {
            UnlockDeck();
            return;
        }

        if (kind == CardKind.Event)
            StartCoroutine(ResolveEventAndUnlock(card));
        else
        {
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
        UpdateRuleStates();

        if (CheckWin())
        {
            EndGameWin();
            yield break;
        }

        while ((cameraDirector != null && cameraDirector.IsPanning) ||
               (TargetingController.Instance != null && TargetingController.Instance.IsResolving))
            yield return null;

        UnlockDeck();
    }

    public void EvaluateEndConditions()
    {
        if (gameEnded) return;
        UpdateRuleStates();
        if (CheckWin()) EndGameWin();
    }

    private IEnumerator UnlockNextFrame()
    {
        yield return null;
        UpdateRuleStates();
        if (CheckWin()) EndGameWin();
        UnlockDeck();
    }

    private void EndGameWin()
    {
        if (gameEnded) return;
        gameEnded = true;
        LockDeck();
        TargetingController.Instance?.CancelTargeting();
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
        TargetingController.Instance?.CancelTargeting();
        RefreshStatusUI();
        if (useScenesForEnd)
            StartCoroutine(LoadEndScene(loseSceneName));
    }

    private IEnumerator LoadEndScene(string scene)
    {
        if (endSceneDelay > 0f)
            yield return new WaitForSeconds(endSceneDelay);
        SceneManager.LoadScene(scene);
    }

    private void LockDeck()
    {
        deckLocked = true;
        if (deckButton) deckButton.interactable = false;
    }

    private void UnlockDeck()
    {
        deckLocked = false;
        if (deckButton) deckButton.interactable = true;
    }
}
