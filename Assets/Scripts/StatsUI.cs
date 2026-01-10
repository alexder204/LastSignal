using TMPro;
using UnityEngine;

public class StatsUI : MonoBehaviour
{
    public ResourceManager resources;

    [Header("Text References")]
    public TMP_Text oxygenText;
    public TMP_Text powerText;
    public TMP_Text stabilityText;
    public TMP_Text signalText;

    void Start()
    {
        if (resources == null)
            resources = FindFirstObjectByType<ResourceManager>();

        Refresh();
    }

    void Update()
    {
        // Jam-safe approach: refresh every frame.
        // Later you can optimize by firing events on change.
        Refresh();
    }

    public void Refresh()
    {
        if (resources == null) return;

        if (oxygenText) oxygenText.text = $"O2: {resources.oxygen}";
        if (powerText)  powerText.text  = $"PWR: {resources.power}";
        if (stabilityText)   stabilityText.text   = $"Stability: {resources.stability}";
        if (signalText) signalText.text = $"SIG: {resources.signal}";
    }
}
