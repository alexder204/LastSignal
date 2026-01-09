using UnityEngine;

public class ModuleTagController : MonoBehaviour
{
    public static ModuleTagController Instance { get; private set; }

    public FloatingModuleTag tagPrefab;

    private FloatingModuleTag activeTag;
    private StationModule activeModule;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ShowFor(StationModule module)
    {
        if (module == null || tagPrefab == null) return;

        // Clicking the same module toggles off
        if (activeModule == module)
        {
            Hide();
            return;
        }

        Hide();

        activeModule = module;
        activeTag = Instantiate(tagPrefab);
        activeTag.Attach(module);
    }

    public void Hide()
    {
        if (activeTag != null) Destroy(activeTag.gameObject);
        activeTag = null;
        activeModule = null;
    }
}
