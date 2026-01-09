using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public List<CardData> eventDeck = new();
    public List<CardData> actionDeck = new();

    public CardData DrawEvent() => DrawFrom(eventDeck);
    public CardData DrawAction() => DrawFrom(actionDeck);

    public CardData DrawRandomByChance(float eventChance, out CardKind kind)
    {
        bool isEvent = Random.value < Mathf.Clamp01(eventChance);
        kind = isEvent ? CardKind.Event : CardKind.Action;
        return isEvent ? DrawEvent() : DrawAction();
    }

    private CardData DrawFrom(List<CardData> deck)
    {
        if (deck == null || deck.Count == 0) return null;
        int i = Random.Range(0, deck.Count);
        return deck[i];
    }
}
