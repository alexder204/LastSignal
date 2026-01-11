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
    public Transform focusPoint;
    public Transform vfxPoint;

    [Header("State")]
    [Min(1)] public int maxHealth = 5;
    [SerializeField] private int health;
    [SerializeField] private bool disabled;

    [Header("Simple Visual Feedback (optional)")]
    public Renderer[] highlightRenderers;

    [Header("Event TV Feedback")]
    public Renderer tvRenderer;                 // assign the TV/screen renderer
    [Tooltip("Which material slot on the TV renderer is the screen? Usually 0.")]
    public int tvMaterialIndex = 0;

    public Color tvNormalColor = Color.white;
    public Color tvEventColor = Color.red;

    // cache
    private Material tvMatInstance;
    private Color cachedBaseColor;
    private Color cachedEmissionColor;
    private bool hasEmission;

    public int Health => health;
    public bool IsDamaged => health < maxHealth;
    public bool IsDisabled => disabled;

    void Awake()
    {
        if (focusPoint == null) focusPoint = transform;
        if (health <= 0) health = maxHealth;

        CacheTVMaterial();
        SetTVColor(tvNormalColor); // init
    }

    private void CacheTVMaterial()
    {
        tvMatInstance = null;
        hasEmission = false;

        if (tvRenderer == null) return;

        var mats = tvRenderer.materials; // creates instances (good for runtime changes)
        if (mats == null || mats.Length == 0) return;

        tvMaterialIndex = Mathf.Clamp(tvMaterialIndex, 0, mats.Length - 1);
        tvMatInstance = mats[tvMaterialIndex];

        // Cache what’s actually on the material
        if (tvMatInstance.HasProperty("_BaseColor"))
            cachedBaseColor = tvMatInstance.GetColor("_BaseColor");
        else if (tvMatInstance.HasProperty("_Color"))
            cachedBaseColor = tvMatInstance.GetColor("_Color");
        else
            cachedBaseColor = tvNormalColor;

        if (tvMatInstance.HasProperty("_EmissionColor"))
        {
            cachedEmissionColor = tvMatInstance.GetColor("_EmissionColor");
            hasEmission = true;
        }
    }

    private void SetTVColor(Color c)
    {
        if (tvMatInstance == null) return;

        // URP Lit uses _BaseColor, Standard uses _Color
        if (tvMatInstance.HasProperty("_BaseColor"))
            tvMatInstance.SetColor("_BaseColor", c);
        if (tvMatInstance.HasProperty("_Color"))
            tvMatInstance.SetColor("_Color", c);

        // If emission exists, set it too (often what screens actually show)
        if (tvMatInstance.HasProperty("_EmissionColor"))
        {
            tvMatInstance.EnableKeyword("_EMISSION");
            tvMatInstance.SetColor("_EmissionColor", c);
        }
    }

    public void SetEventTV(bool on)
    {
        // If renderer changed at runtime, recache once
        if (tvRenderer != null && tvMatInstance == null)
            CacheTVMaterial();

        if (tvMatInstance == null) return;

        if (on)
        {
            SetTVColor(tvEventColor);
        }
        else
        {
            // restore cached colors (not just white in case your normal isn’t pure white)
            if (tvMatInstance.HasProperty("_BaseColor"))
                tvMatInstance.SetColor("_BaseColor", cachedBaseColor);
            if (tvMatInstance.HasProperty("_Color"))
                tvMatInstance.SetColor("_Color", cachedBaseColor);

            if (hasEmission && tvMatInstance.HasProperty("_EmissionColor"))
                tvMatInstance.SetColor("_EmissionColor", cachedEmissionColor);
            else if (tvMatInstance.HasProperty("_EmissionColor"))
                tvMatInstance.SetColor("_EmissionColor", Color.black);
        }
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

        if (health == 0) disabled = true;
    }

    public void Repair(int amount)
    {
        if (amount <= 0) return;
        health = Mathf.Min(maxHealth, health + amount);

        if (health > 0) disabled = false;
    }

    public void SetDisabled(bool value)
    {
        disabled = value;
    }

    public void SetHighlighted(bool on)
    {
        if (highlightRenderers == null || highlightRenderers.Length == 0) return;
        if (normalMaterial == null || highlightMaterial == null) return;

        foreach (var r in highlightRenderers)
            if (r != null) r.material = on ? highlightMaterial : normalMaterial;
    }
}
