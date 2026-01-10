using System.Collections;
using UnityEngine;

public class TargetingController : MonoBehaviour
{
    public static TargetingController Instance { get; private set; }

    public EventResolver resolver;
    public HandController hand;

    private CardData pendingCard;
    private bool isTargeting;

    private bool isResolving;
    public bool IsResolving => isResolving;
    public bool IsTargeting => isTargeting;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (resolver == null) resolver = FindFirstObjectByType<EventResolver>();
        if (hand == null) hand = FindFirstObjectByType<HandController>();
    }

    private bool IsValidTarget(CardData card, StationModule module)
    {
        if (card == null || module == null) return false;

        return card.targetRule switch
        {
            CardData.TargetRule.AnyModule => true,
            CardData.TargetRule.SpecificType => module.type == card.requiredType,
            _ => false
        };
    }

    public void StartTargeting(CardData card)
    {
        if (card == null) return;

        // If already targeting the same card -> deselect (cancel)
        if (isTargeting && pendingCard == card)
        {
            CancelTargeting();
            return;
        }

        // If targeting a different card -> switch to it
        pendingCard = card;
        isTargeting = true;

        HighlightModules(true);
    }

    public void CancelTargeting()
    {
        if (isResolving) return;
        if (!isTargeting) return;

        HighlightModules(false);
        pendingCard = null;
        isTargeting = false;
    }


    public void OnModuleClicked(StationModule module)
    {
        if (!isTargeting || pendingCard == null) return;
        if (isResolving) return;

        if (!IsValidTarget(pendingCard, module))
            return;

        isResolving = true;

        CardData cardToResolve = pendingCard;
        pendingCard = null;
        isTargeting = false;

        HighlightModules(false);

        StartCoroutine(ResolveAndEnd(cardToResolve, module));
    }


    private IEnumerator ResolveAndEnd(CardData card, StationModule target)
    {
        yield return resolver.Resolve(card, target);
        FindFirstObjectByType<DeckClicker>()?.SendMessage("CheckWinAndEnd");

        hand.RemoveCard(card);

        isResolving = false;
    }

    private void HighlightModules(bool on)
    {
        foreach (var m in ModuleRegistry.Instance.All)
        {
            if (m == null) continue;

            if (!on)
            {
                m.SetHighlighted(false);
                m.SetAlarm(false);
                continue;
            }

            bool valid = IsValidTarget(pendingCard, m);
            m.SetHighlighted(valid);
            m.SetAlarm(valid); // optional, remove if you don’t want alarms
        }
    }
}
