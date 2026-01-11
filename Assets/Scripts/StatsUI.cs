// StatsUI.cs
using TMPro;
using UnityEngine;

public class StatsUI : MonoBehaviour
{
    public ResourceManager resources;

    [Header("Text Reference")]
    public TMP_Text signalText;

    void Start()
    {
        if (resources == null)
            resources = FindFirstObjectByType<ResourceManager>();

        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (resources == null) return;

        if (signalText)
            signalText.text = $"SIG: {resources.signal}";
    }
}