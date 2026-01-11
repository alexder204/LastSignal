// HandController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandController : MonoBehaviour
{
    [Header("Hand Limit")]
    public int maxHandSize = 5;

    [Header("Overflow Popup UI (assign in Inspector)")]
    public ActionOverflowPopupUI overflowPopup;

    public Transform handRoot;
    public CardView cardPrefab;

    private readonly List<CardData> cards = new();
    private readonly List<CardView> views = new();

    private CardData pendingOverflowCard;
    public bool IsChoosingOverflow => pendingOverflowCard != null;

    void Awake()
    {
        if (handRoot == null) handRoot = transform;
    }

    // returns true if added, false if full
    public bool AddCard(CardData card)
    {
        if (card == null) return false;
        if (cards.Count >= maxHandSize) return false;

        cards.Add(card);
        Render();
        return true;
    }

    // call when AddCard fails (hand full)
    public void StartOverflowChoice(CardData drawnCard)
    {
        if (drawnCard == null) return;
        if (pendingOverflowCard != null) return;

        pendingOverflowCard = drawnCard;

        if (overflowPopup != null)
            overflowPopup.Show(drawnCard, DiscardOverflow);

        Render(); // clicking hand cards now means "replace"
    }

    public void RemoveCard(CardData card)
    {
        cards.Remove(card);
        Render();
    }

    private void Render()
    {
        for (int i = 0; i < views.Count; i++)
            if (views[i] != null) Destroy(views[i].gameObject);
        views.Clear();

        foreach (var c in cards)
        {
            var v = Instantiate(cardPrefab, handRoot);
            v.Bind(c, OnCardClicked);
            views.Add(v);
        }
    }

    private void OnCardClicked(CardData clickedCard)
    {
        if (clickedCard == null) return;

        // Overflow mode: click a card to replace it
        if (pendingOverflowCard != null)
        {
            ReplaceCard(clickedCard);
            return;
        }

        // Normal behavior: play card
        if (clickedCard.requiresTarget)
        {
            TargetingController.Instance.StartTargeting(clickedCard);
        }
        else
        {
            StartCoroutine(ResolveImmediate(clickedCard));
        }
    }

    private IEnumerator ResolveImmediate(CardData card)
    {
        yield return FindFirstObjectByType<EventResolver>().Resolve(card);
        RemoveCard(card);
    }

    private void ReplaceCard(CardData toReplace)
    {
        int idx = cards.IndexOf(toReplace);
        if (idx < 0) return;

        cards[idx] = pendingOverflowCard;
        EndOverflowChoice();
    }

    private void DiscardOverflow()
    {
        EndOverflowChoice();
    }

    private void EndOverflowChoice()
    {
        pendingOverflowCard = null;

        if (overflowPopup != null)
            overflowPopup.Hide();

        Render();
    }
}
