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

        StationModule target = registry.GetRandom(m => true);
        if (target == null) yield break;

        // TURN ALARM ON
        target.SetAlarm(true);

        // Focus camera
        yield return cameraDirector.Focus(
            target.focusPoint != null ? target.focusPoint : target.transform
        );

        // Apply effect
        target.Damage(damageAmount);

        // Hold for readability
        yield return new WaitForSeconds(0.6f);

        // Return camera
        yield return cameraDirector.ReturnToDefault();

        // TURN ALARM OFF
        target.SetAlarm(false);
    }
}
