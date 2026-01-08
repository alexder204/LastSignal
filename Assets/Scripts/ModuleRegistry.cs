// ModuleRegistry.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ModuleRegistry : MonoBehaviour
{
    public static ModuleRegistry Instance { get; private set; }

    private readonly List<StationModule> modules = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-register all StationModule in the scene at start
        modules.Clear();
        modules.AddRange(FindObjectsByType<StationModule>(FindObjectsInactive.Include, FindObjectsSortMode.None));
    }

    public IReadOnlyList<StationModule> All => modules;

    public StationModule Get(ModuleType type) =>
        modules.FirstOrDefault(m => m.type == type);

    public List<StationModule> GetDamaged() =>
        modules.Where(m => m.IsDamaged && !m.IsDisabled).ToList();

    public StationModule GetRandom(System.Predicate<StationModule> filter = null)
    {
        var candidates = (filter == null) ? modules : modules.Where(m => filter(m)).ToList();
        if (candidates.Count == 0) return null;
        return candidates[Random.Range(0, candidates.Count)];
    }
}
