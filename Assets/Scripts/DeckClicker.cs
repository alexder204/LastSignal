using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckClicker : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text dayText;
    public TMP_Text drawsText;
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

        Debug.Log($"DAY {day} START  Draws:{maxDrawsThisDay}  EventChance:{eventChance:0.00}");
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

    private void RefreshUI()
    {
        if (dayText != null)
            dayText.text = $"Day {day}";

        if (drawsText != null)
            drawsText.text = $"{drawsUsed}/{maxDrawsThisDay}";
    }


    public void OnDeckClicked()
    {
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

        drawsUsed++;
        RefreshUI();

        if (kind == CardKind.Event)
        {
            StartCoroutine(ResolveEventAndUnlock(card));
        }
        else
        {
            hand.AddCard(card); // no camera pan
            StartCoroutine(UnlockNextFrame()); // small cooldown so you can't turbo-click
        }
    }

    private IEnumerator ResolveEventAndUnlock(CardData card)
    {
        yield return resolver.Resolve(card);

        // Wait until camera finishes returning/panning, just in case
        while ((cameraDirector != null && cameraDirector.IsPanning) ||
               (TargetingController.Instance != null && TargetingController.Instance.IsResolving))
        {
            yield return null;
        }

        UnlockDeck();
    }

    private IEnumerator UnlockNextFrame()
    {
        yield return null;
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
