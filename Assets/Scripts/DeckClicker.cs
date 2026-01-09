using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DeckClicker : MonoBehaviour
{
    public CameraDirector cameraDirector;
    public DeckManager deck;
    public EventResolver resolver;
    public HandController hand;

    [Header("Optional: disable the UI button")]
    public Button deckButton;

    [Range(0f, 1f)] public float eventChance = 0.35f;
    public int maxDrawsThisDay = 10;

    private int drawsUsed;
    private bool deckLocked;

    void Awake()
    {
        if (cameraDirector == null) cameraDirector = FindFirstObjectByType<CameraDirector>();
        if (deck == null) deck = FindFirstObjectByType<DeckManager>();
        if (resolver == null) resolver = FindFirstObjectByType<EventResolver>();
        if (hand == null) hand = FindFirstObjectByType<HandController>();
        if (deckButton == null) deckButton = GetComponent<Button>(); // if this script is on the button object
    }

    public void OnDeckClicked()
    {
        if (deckLocked) return;
        if (drawsUsed >= maxDrawsThisDay) return;

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
