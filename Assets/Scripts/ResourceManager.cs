// ResourceManager.cs
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [Header("Signal")]
    public int signal = 0;
    public int maxSignal = 99;

    public void ApplySignal(int delta)
    {
        signal = Mathf.Clamp(signal + delta, 0, maxSignal);
    }
}