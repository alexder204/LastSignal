// ResourceManager.cs
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public int oxygen = 10, power = 10, stability = 10, signal = 0;

    public void Apply(int o, int p, int s, int st)
    {
        oxygen += o; power += p; stability += st; signal += s;
        oxygen = Mathf.Clamp(oxygen, 0, 99);
        power  = Mathf.Clamp(power, 0, 99);
        stability   = Mathf.Clamp(stability, 0, 99);
        signal = Mathf.Clamp(signal, 0, 99);
    }

    public bool IsDead() => oxygen <= 0 || power <= 0 || stability <= 0;
}
