using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HandController : MonoBehaviour
{
    public Transform handRoot;
    public CardView cardPrefab;

    private readonly List<CardData> cards = new();
    private readonly List<CardView> views = new();

    void Awake()
    {
        if (handRoot == null) handRoot = transform;
    }

    public void AddCard(CardData card)
    {
        cards.Add(card);
        Render();
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

    private void OnCardClicked(CardData card)
    {
        if (card.requiresTarget)
        {
            TargetingController.Instance.StartTargeting(card);
        }
        else
        {
            // no target → resolve instantly
            StartCoroutine(ResolveImmediate(card));
        }
    }

    private IEnumerator ResolveImmediate(CardData card)
    {
        yield return FindFirstObjectByType<EventResolver>().Resolve(card);
        RemoveCard(card);
    }

}
