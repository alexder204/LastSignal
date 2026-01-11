// StationModule.cs
using UnityEngine;
using System;
using System.Collections;

public class StationModule : MonoBehaviour
{
    [Header("Highlight")]
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

    public event Action<StationModule> OnHPChanged;

    [Header("Auto Highlight")]
    public bool autoCollectRenderers = true;
    private Renderer[] cachedRenderers;

    [Header("Event TV Feedback")]
    public Renderer tvRenderer;
    [Tooltip("Which material slot on the TV renderer is the screen? Usually 0.")]
    public int tvMaterialIndex = 0;

    public Color tvNormalColor = Color.white;
    public Color tvEventColor = Color.red;

    // TV cache
    private Material tvMatInstance;
    private Color cachedBaseColor;
    private Color cachedEmissionColor;
    private bool hasEmission;

    [Header("Animation (General / Best-Effort)")]
    public Animator animator;

    // These are STATE names (case-sensitive). If your states are in a sub-state machine,
    // this script will also try "Base Layer.<Name>" and "Base Layer/<Name>".
    public string idleState = "Idle";
    public string brokenIdleState = "BrokenIdle";
    public string eventState = "Event";
    public string actionState = "Action"; // or "Repair"

    // Jam-safe fixed durations (avoids relying on clip lookup)
    public float eventSeconds = 0.6f;
    public float actionSeconds = 0.6f;

    public int Health => health;
    public int MaxHealth => maxHealth;
    public bool IsDamaged => health < maxHealth;
    public bool IsDisabled => disabled;

    void Awake()
    {
        if (focusPoint == null) focusPoint = transform;

        if (autoCollectRenderers)
            CollectRenderers();

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        // Your original behavior: if health not set, start at max
        if (health <= 0) health = maxHealth;
        health = Mathf.Clamp(health, 0, maxHealth);
        if (health == 0) disabled = true;

        CacheTVMaterial();
        SetTVColor(tvNormalColor);

        // Best-effort: settle into correct idle if it exists
        TrySettleIdleFromHP();
    }

    void CollectRenderers()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

    #region Animation Helpers (GENERAL)

    private bool TryPlayStateFast(string stateName, float normalizedTime = 0f)
    {
        if (string.IsNullOrWhiteSpace(stateName)) return false;

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (animator == null) return false;

        // Try a few common forms Unity accepts
        string[] candidates =
        {
            stateName,
            $"Base Layer.{stateName}",
            $"Base Layer/{stateName}",
        };

        for (int layer = 0; layer < animator.layerCount; layer++)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                int h = Animator.StringToHash(candidates[i]);
                if (animator.HasState(layer, h))
                {
                    animator.Play(h, layer, normalizedTime);
                    return true;
                }
            }
        }

        return false;
    }

    private void TrySettleIdleFromHP()
    {
        // IMPORTANT: only settle into a state if THAT state exists.
        // If it doesn't exist on this module's animator/controller, do nothing.
        if (health <= 0)
        {
            TryPlayStateFast(brokenIdleState, 0f);
        }
        else
        {
            TryPlayStateFast(idleState, 0f);
        }
    }

    public IEnumerator PlayEventAnimThenSettle()
    {
        bool played = TryPlayStateFast(eventState, 0f);
        if (played && eventSeconds > 0f)
            yield return new WaitForSeconds(eventSeconds);

        TrySettleIdleFromHP();
    }

    public IEnumerator PlayActionAnimThenSettle()
    {
        bool played = TryPlayStateFast(actionState, 0f);
        if (played && actionSeconds > 0f)
            yield return new WaitForSeconds(actionSeconds);

        TrySettleIdleFromHP();
    }

    #endregion

    #region TV

    private void CacheTVMaterial()
    {
        tvMatInstance = null;
        hasEmission = false;

        if (tvRenderer == null) return;

        var mats = tvRenderer.materials;
        if (mats == null || mats.Length == 0) return;

        tvMaterialIndex = Mathf.Clamp(tvMaterialIndex, 0, mats.Length - 1);
        tvMatInstance = mats[tvMaterialIndex];

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

        if (tvMatInstance.HasProperty("_BaseColor"))
            tvMatInstance.SetColor("_BaseColor", c);
        if (tvMatInstance.HasProperty("_Color"))
            tvMatInstance.SetColor("_Color", c);

        if (tvMatInstance.HasProperty("_EmissionColor"))
        {
            tvMatInstance.EnableKeyword("_EMISSION");
            tvMatInstance.SetColor("_EmissionColor", c);
        }
    }

    public void SetEventTV(bool on)
    {
        if (tvRenderer != null && tvMatInstance == null)
            CacheTVMaterial();

        if (tvMatInstance == null) return;

        if (on)
        {
            SetTVColor(tvEventColor);
        }
        else
        {
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

    #endregion

    void OnMouseDown()
    {
        // Allow clicking even when disabled (so you can repair broken parts)
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
        if (amount <= 0 || disabled) return;

        int before = health;
        health = Mathf.Max(0, health - amount);

        if (health == 0) disabled = true;

        if (health != before)
            OnHPChanged?.Invoke(this);
    }

    public void Repair(int amount)
    {
        // IMPORTANT: allow repair from 0
        if (amount <= 0) return;

        int before = health;
        health = Mathf.Min(maxHealth, health + amount);

        if (health > 0)
            disabled = false;

        if (health != before)
            OnHPChanged?.Invoke(this);
    }

    public void SetDisabled(bool value)
    {
        if (disabled == value) return;

        disabled = value;
        if (disabled) health = 0;

        OnHPChanged?.Invoke(this);
    }

    public void SetHighlighted(bool on)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0) return;
        if (normalMaterial == null || highlightMaterial == null) return;

        foreach (var r in cachedRenderers)
        {
            if (!r) continue;
            if (tvRenderer != null && r == tvRenderer) continue;

            var mats = r.materials;
            if (mats == null || mats.Length == 0) continue;

            for (int i = 0; i < mats.Length; i++)
                mats[i] = on ? highlightMaterial : normalMaterial;

            r.materials = mats;
        }
    }
}
