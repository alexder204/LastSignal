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

    [Header("Animation (State Names Only)")]
    public Animator animator;

    public string idleState = "Idle";
    public string brokenIdleState = "BrokenIdle";
    public string eventState = "Event";
    public string actionState = "Action"; // or "Repair"

    public float fallbackAnimSeconds = 0.8f;

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

    public int Health => health;
    public int MaxHealth => maxHealth;
    public bool IsDamaged => health < maxHealth;
    public bool IsDisabled => disabled;

    void Awake()
    {
        if (focusPoint == null) focusPoint = transform;

        if (autoCollectRenderers)
            CollectRenderers();

        if (health <= 0) health = maxHealth;
        health = Mathf.Clamp(health, 0, maxHealth);
        if (health == 0) disabled = true;

        CacheTVMaterial();
        SetTVColor(tvNormalColor);
    }

    float GetAnimLength(string stateName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return fallbackAnimSeconds;

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == stateName)
                return clip.length;
        }

        return fallbackAnimSeconds;
    }

    void SetIdleFromHealth()
    {
        if (animator == null) return;

        if (health <= 0)
            animator.Play(brokenIdleState, 0, 0f);
        else
            animator.Play(idleState, 0, 0f);
    }

    public IEnumerator PlayEventAnimThenSettle()
    {
        if (animator == null) yield break;

        animator.Play(eventState, 0, 0f);
        yield return new WaitForSeconds(GetAnimLength(eventState));

        SetIdleFromHealth();
    }

    public IEnumerator PlayActionAnimThenSettle()
    {
        if (animator == null) yield break;

        animator.Play(actionState, 0, 0f);
        yield return new WaitForSeconds(GetAnimLength(actionState));

        SetIdleFromHealth();
    }


    void CollectRenderers()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
    }

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
