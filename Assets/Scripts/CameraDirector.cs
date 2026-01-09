// CameraDirector.cs
using System.Collections;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    [Header("Rig root that moves (the parent of your pivot/camera)")]
    public Transform rigRoot;

    [Header("Movement")]
    public float panDuration = 0.9f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 defaultPos;
    private bool hasDefault;
    public Vector3 focusOffset = Vector3.zero;


    void Awake()
    {
        if (rigRoot == null)
            rigRoot = Camera.main != null ? Camera.main.transform.root : null;

        if (rigRoot != null)
        {
            defaultPos = rigRoot.position;
            hasDefault = true;
        }
    }

    public void SetDefaultFromCurrent()
    {
        if (rigRoot == null) return;
        defaultPos = rigRoot.position;
        hasDefault = true;
    }

    public IEnumerator Focus(Transform target)
    {
        if (rigRoot == null || target == null) yield break;
        if (!hasDefault) SetDefaultFromCurrent();

        yield return PanTo(target.position + focusOffset);
    }


    public IEnumerator ReturnToDefault()
    {
        if (rigRoot == null || !hasDefault) yield break;
        yield return PanTo(defaultPos);
    }

    private IEnumerator PanTo(Vector3 worldPos)
    {
        Vector3 start = rigRoot.position;
        Vector3 end = new Vector3(worldPos.x, start.y, worldPos.z); // keep height constant

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, panDuration);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            rigRoot.position = Vector3.Lerp(start, end, k);
            yield return null;
        }
        rigRoot.position = end;
    }
}
