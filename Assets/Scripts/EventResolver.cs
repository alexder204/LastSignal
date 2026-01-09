// EventResolver.cs
using System.Collections;
using UnityEngine;

public class EventResolver : MonoBehaviour
{
    public CameraDirector cameraDirector;
    public ResourceManager resources;

    // Use this for EVENT cards and for actions that don't need a target.
    public IEnumerator Resolve(CardData card)
    {
        if (card == null) yield break;

        StationModule target = PickTargetFromCard(card);

        yield return ResolveInternal(card, target);
    }

    // Use this for ACTION cards when the player clicks a module.
    public IEnumerator Resolve(CardData card, StationModule forcedTarget)
    {
        if (card == null) yield break;

        StationModule target = forcedTarget != null ? forcedTarget : PickTargetFromCard(card);

        yield return ResolveInternal(card, target);
    }

    private StationModule PickTargetFromCard(CardData card)
    {
        if (ModuleRegistry.Instance == null) return null;

        // Only EVENTS auto-pick a target.
        if (card.kind == CardKind.Event)
        {
            if (card.requiresTarget)
                return ModuleRegistry.Instance.Get(card.requiredType);

            if (card.useRandomTargetIfNone)
                return ModuleRegistry.Instance.GetRandom(m => true);
        }

        // ACTIONS must be targeted by the player (forcedTarget).
        return null;
    }

    private IEnumerator ResolveInternal(CardData card, StationModule target)
    {
        // Highlight the affected module
        if (target != null) target.SetHighlighted(true);

        if (target != null && card.flashAlarmDuringResolve)
            target.SetAlarm(true);

        if (target != null && cameraDirector != null)
        {
            var fp = target.focusPoint != null ? target.focusPoint : target.transform;
            yield return cameraDirector.Focus(fp);
        }

        if (resources != null)
            resources.Apply(card.oxygenDelta, card.powerDelta, card.hullDelta, card.signalDelta);

        if (target != null)
        {
            if (card.moduleDamage > 0) target.Damage(card.moduleDamage);
            if (card.moduleRepair > 0) target.Repair(card.moduleRepair);
        }

        yield return new WaitForSeconds(Mathf.Max(0f, card.cameraHoldSeconds));

        if (cameraDirector != null)
            yield return cameraDirector.ReturnToDefault();

        if (target != null && card.flashAlarmDuringResolve)
            target.SetAlarm(false);

        // Stop highlighting
        if (target != null) target.SetHighlighted(false);
    }
}
