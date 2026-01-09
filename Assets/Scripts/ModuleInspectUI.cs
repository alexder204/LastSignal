using TMPro;
using UnityEngine;

public class ModuleInspectUI : MonoBehaviour
{
    public static ModuleInspectUI Instance { get; private set; }

    [Header("UI")]
    public GameObject panel;   // parent panel to show/hide
    public TMP_Text nameText;
    public TMP_Text healthText;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panel != null) panel.SetActive(false);
    }

    public void Show(StationModule module)
    {
        if (module == null) return;

        if (panel != null) panel.SetActive(true);

        if (nameText != null) nameText.text = module.type.ToString();
        if (healthText != null) healthText.text = $"HP: {module.Health}/{module.maxHealth}";
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
