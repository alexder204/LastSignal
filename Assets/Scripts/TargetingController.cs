using System.Collections;
using UnityEngine;

public class TargetingController : MonoBehaviour
{
    public static TargetingController Instance { get; private set; }

    public EventResolver resolver;
    public HandController hand;

    private CardData pendingCard;
    private bool isTargeting;

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

    public void StartTargeting(CardData card)
    {
        if (isTargeting) return;

        pendingCard = card;
        isTargeting = true;

        HighlightModules(true);
    }

    public void OnModuleClicked(StationModule module)
    {
        if (!isTargeting || pendingCard == null) return;

        StartCoroutine(ResolveAndEnd(module));
    }

    private IEnumerator ResolveAndEnd(StationModule target)
    {
        HighlightModules(false);

        yield return resolver.Resolve(pendingCard, target);

        hand.RemoveCard(pendingCard);

        pendingCard = null;
        isTargeting = false;
    }

    private void HighlightModules(bool on)
    {
        foreach (var m in ModuleRegistry.Instance.All)
        {
            m.SetAlarm(on); // TEMP highlight (safe + visible)
        }
    }
}
