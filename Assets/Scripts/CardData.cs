// CardData.cs
using UnityEngine;

public enum CardKind { Event, Action }

[CreateAssetMenu(menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;

    [Header("Art")]
    public Sprite cardArt;

    public CardKind kind;

    [Header("Draw Weight (rarity)")]
    [Min(0f)] public float drawWeight = 1f; // 0 = never draw, 1 = normal, 0.2 = rare

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

    [Header("Effect (signal-only)")]
    public int signalDelta;

    [Header("Module Effect")]
    public int moduleDamage;
    public int moduleRepair;

    [Header("Presentation")]
    public float cameraHoldSeconds = 0.6f;
    public bool flashAlarmDuringResolve = true;
}