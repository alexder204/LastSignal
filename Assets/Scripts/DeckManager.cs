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

        float total = 0f;
        for (int i = 0; i < deck.Count; i++)
        {
            var c = deck[i];
            if (c == null) continue;
            if (c.drawWeight <= 0f) continue;
            total += c.drawWeight;
        }

        if (total <= 0f) return null;

        float roll = Random.value * total;
        float acc = 0f;

        for (int i = 0; i < deck.Count; i++)
        {
            var c = deck[i];
            if (c == null) continue;
            if (c.drawWeight <= 0f) continue;

            acc += c.drawWeight;
            if (roll <= acc) return c;
        }

        return null;
    }
}
