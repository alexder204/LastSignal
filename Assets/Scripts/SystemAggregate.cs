using UnityEngine;
using System;
using System.Collections.Generic;

public class SystemAggregate : MonoBehaviour
{
    public List<StationModule> modules = new();

    public int CurrentHP { get; private set; }
    public int MaxHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    public event Action<SystemAggregate> OnAggregateChanged;

    void Awake()
    {
        AutoFillIfEmpty();
        HookModules(true);
    }

    void Start()
    {
        Recalculate(); // ← AFTER all StationModule.Awake()
    }


    void OnDestroy()
    {
        HookModules(false);
    }

    void AutoFillIfEmpty()
    {
        if (modules.Count == 0)
            modules.AddRange(GetComponentsInChildren<StationModule>(true));
    }

    void HookModules(bool hook)
    {
        foreach (var m in modules)
        {
            if (!m) continue;
            if (hook) m.OnHPChanged += OnModuleChanged;
            else      m.OnHPChanged -= OnModuleChanged;
        }
    }

    void OnModuleChanged(StationModule _)
    {
        Recalculate();
    }

    public void Recalculate()
    {
        int cur = 0, max = 0;

        foreach (var m in modules)
        {
            if (!m) continue;
            cur += Mathf.Max(0, m.Health);
            max += Mathf.Max(0, m.MaxHealth);
        }

        CurrentHP = cur;
        MaxHP = max;
        OnAggregateChanged?.Invoke(this);
    }

    public StationModule GetRandomAliveModule()
    {
        if (modules == null || modules.Count == 0) return null;

        int aliveCount = 0;
        for (int i = 0; i < modules.Count; i++)
        {
            var m = modules[i];
            if (!m) continue;
            if (!m.IsDisabled && m.Health > 0) aliveCount++;
        }

        if (aliveCount == 0) return null;

        int pick = UnityEngine.Random.Range(0, aliveCount);
        for (int i = 0; i < modules.Count; i++)
        {
            var m = modules[i];
            if (!m) continue;
            if (m.IsDisabled || m.Health <= 0) continue;

            if (pick == 0) return m;
            pick--;
        }

        return null;
    }

    public bool TryDamageRandomAlive(int amount)
    {
        var m = GetRandomAliveModule();
        if (m == null) return false;
        m.Damage(amount);
        return true;
    }

}
