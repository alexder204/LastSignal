// StationModule.cs
using UnityEngine;

public class StationModule : MonoBehaviour
{
    [Header("Highlight (optional)")]
    public Material normalMaterial;
    public Material highlightMaterial;

    [Header("Identity")]
    public ModuleType type;

    [Header("Focus & VFX Points")]
    public Transform focusPoint;   // where the camera should pan
    public Transform vfxPoint;     // where sparks/steam can spawn (optional)

    [Header("State")]
    [Min(1)] public int maxHealth = 5;
    [SerializeField] private int health;
    [SerializeField] private bool disabled;

    [Header("Simple Visual Feedback (optional)")]
    public Light alarmLight;       // toggle this for events
    public Renderer[] highlightRenderers; // optional: set emissive/outline later

    public int Health => health;
    public bool IsDamaged => health < maxHealth;
    public bool IsDisabled => disabled;

    void Awake()
    {
        if (focusPoint == null) focusPoint = transform; // fallback
        if (health <= 0) health = maxHealth;
        SetAlarm(false);
    }

    void OnMouseDown()
    {
        if (TargetingController.Instance != null && TargetingController.Instance.IsResolving)
            return;

        if (TargetingController.Instance != null && TargetingController.Instance.IsTargeting)
        {
            TargetingController.Instance.OnModuleClicked(this);
            return;
        }

        ModuleTagController.Instance?.ShowFor(this);
    }

    public void Damage(int amount)
    {
        if (amount <= 0) return;
        health = Mathf.Max(0, health - amount);

        // If you want: auto-disable when health hits 0
        if (health == 0) disabled = true;

        SetAlarm(true);
    }

    public void Repair(int amount)
    {
        if (amount <= 0) return;
        health = Mathf.Min(maxHealth, health + amount);

        if (health > 0) disabled = false;
        if (!IsDamaged) SetAlarm(false);
    }

    public void SetDisabled(bool value)
    {
        disabled = value;
        SetAlarm(value || IsDamaged);
    }

    public void SetAlarm(bool on)
    {
        if (alarmLight != null) alarmLight.enabled = on;
    }

    public void SetHighlighted(bool on)
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0) return;
        if (normalMaterial == null || highlightMaterial == null) return;

        foreach (var r in highlightRenderers)
            if (r != null) r.material = on ? highlightMaterial : normalMaterial;
    }


    // For quick targeting/selection debugging
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Transform fp = focusPoint != null ? focusPoint : transform;
        Gizmos.DrawWireSphere(fp.position, 0.25f);
        Gizmos.DrawLine(fp.position, fp.position + Vector3.up * 0.75f);
    }
}
