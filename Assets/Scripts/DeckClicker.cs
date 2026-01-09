using System.Collections;
using UnityEngine;

public class DeckClicker : MonoBehaviour
{
    public DeckManager deck;
    public EventResolver resolver;
    public HandController hand;

    [Range(0f, 1f)] public float eventChance = 0.35f;
    public int maxDrawsThisDay = 10;

    private int drawsUsed;

    void Awake()
    {
        if (deck == null) deck = FindFirstObjectByType<DeckManager>();
        if (resolver == null) resolver = FindFirstObjectByType<EventResolver>();
        if (hand == null) hand = FindFirstObjectByType<HandController>();
    }

    public void OnDeckClicked()
    {
        if (drawsUsed >= maxDrawsThisDay) return;

        drawsUsed++;

        CardKind kind;
        CardData card = deck.DrawRandomByChance(eventChance, out kind);
        if (card == null) return;

        if (kind == CardKind.Event)
        {
            StartCoroutine(resolver.Resolve(card)); // instant
        }
        else
        {
            hand.AddCard(card); // save/play later
        }
    }
}
