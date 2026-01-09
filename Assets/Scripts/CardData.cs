// CardData.cs
using UnityEngine;

public enum CardKind { Event, Action }

[CreateAssetMenu(menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public CardKind kind;

    public enum TargetRule
    {
        AnyModule,      // wildcard
        SpecificType    // must match requiredType
    }

    [Header("Targeting Rules")]
    public TargetRule targetRule = TargetRule.AnyModule;
    public ModuleType requiredType;   // used when SpecificType

    [Header("Targeting")]
    public bool requiresTarget;
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
