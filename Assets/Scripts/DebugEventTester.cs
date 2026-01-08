// DebugEventTester.cs
using System.Collections;
using UnityEngine;

public class DebugEventTester : MonoBehaviour
{
    public CameraDirector cameraDirector;
    public int damageAmount = 1;

    void Update()
    {
        // Press E to simulate drawing an event card
        if (Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(RunTestEvent());
        }
    }

    private IEnumerator RunTestEvent()
    {
        var registry = ModuleRegistry.Instance;
        if (registry == null) yield break;

        // Pick a random module (prefer one not already dead)
        StationModule target = registry.GetRandom(m => true);
        if (target == null) yield break;

        // Focus camera
        yield return cameraDirector.Focus(target.focusPoint != null ? target.focusPoint : target.transform);

        // Apply effect (damage + alarm)
        target.Damage(damageAmount);

        // Hold briefly
        yield return new WaitForSeconds(0.6f);

        // Return
        yield return cameraDirector.ReturnToDefault();
    }
}
