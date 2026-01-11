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
        StartDay(1);
        RefreshStatusUI();
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

    private void RefreshStatusUI()
    {
        if (statusText == null) return;
        statusText.text = gameEnded ? "GAME OVER" : "";
    }

    public void OnDeckClicked()
    {
        if (gameEnded) return;

        // Block drawing while player is choosing overflow replacement/trash
        if (hand != null && hand.IsChoosingOverflow) return;

        if (CheckWin())
        {
            EndGameWin();
            return;
        }

        // Day end -> advance day (no draw)
        if (drawsUsed >= maxDrawsThisDay)
        {
            AdvanceDay();
            return;
        }

        if (deckLocked) return;

        LockDeck();

        CardKind kind;
        CardData card = deck.DrawRandomByChance(eventChance, out kind);
        if (card == null)
        {
            UnlockDeck();
            return;
        }

        drawsUsed++;
        RefreshUI();

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