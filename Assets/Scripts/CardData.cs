// CardData.cs
using UnityEngine;

public enum CardKind { Event, Action }

[CreateAssetMenu(menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public CardKind kind;

    [Header("Targeting")]
    public bool requiresTarget;
    public ModuleType requiredType; // optional: set to a type for fixed-target cards
    public bool useRandomTargetIfNone; // for event cards

    [Header("Effect (simple for jam)")]
    public int oxygenDelta;
    public int powerDelta;
    public int hullDelta;
    public int signalDelta;

    [Header("Module Effect")]
    public int moduleDamage;
    public int moduleRepair;

    [Header("Presentation")]
    public float cameraHoldSeconds = 0.6f;
    public bool flashAlarmDuringResolve = true;
}
