// EventResolver.cs
using System.Collections;
using UnityEngine;

public class EventResolver : MonoBehaviour
{
    public CameraDirector cameraDirector;
    public ResourceManager resources;
    public EventPopupUI eventPopup;

    public IEnumerator Resolve(CardData card)
    {
        if (card == null) yield break;

        StationModule target = PickTargetFromCard(card);
        yield return ResolveInternal(card, target);
    }

    public IEnumerator Resolve(CardData card, StationModule forcedTarget)
    {
        if (card == null) yield break;

        StationModule target = forcedTarget != null ? forcedTarget : PickTargetFromCard(card);
        yield return ResolveInternal(card, target);
    }

    private StationModule PickTargetFromCard(CardData card)
    {
        if (ModuleRegistry.Instance == null || card == null) return null;
        if (card.kind != CardKind.Event) return null;

        if (card.targetRule == CardData.TargetRule.SpecificType)
            return ModuleRegistry.Instance.GetRandom(m => m != null && m.type == card.requiredType);

        if (card.useRandomTargetIfNone)
            return ModuleRegistry.Instance.GetRandom(m => m != null);

        return null;
    }

    private IEnumerator ResolveInternal(CardData card, StationModule target)
    {
        if (card.kind == CardKind.Event && eventPopup != null)
            eventPopup.Show(card);

        if (target != null)
        {
            target.SetHighlighted(true);
            target.SetEventTV(true);
        }

        if (target != null && cameraDirector != null && card.kind == CardKind.Event)
        {
            var fp = target.focusPoint != null ? target.focusPoint : target.transform;
            yield return cameraDirector.Focus(fp, true);
        }

        // Signal-only apply
        if (resources != null)
            resources.ApplySignal(card.signalDelta);

        if (target != null)
        {
            if (card.moduleDamage > 0) target.Damage(card.moduleDamage);
            if (card.moduleRepair > 0) target.Repair(card.moduleRepair);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, card.cameraHoldSeconds));

        if (cameraDirector != null && card.kind == CardKind.Event)
            yield return cameraDirector.ReturnToLastPosition();

        if (target != null)
        {
            target.SetEventTV(false);
            target.SetHighlighted(false);
        }
    }
}