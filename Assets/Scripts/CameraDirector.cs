// CameraDirector.cs
using System.Collections;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    public bool IsPanning { get; private set; }

    [Header("Rig root that moves (the parent of your pivot/camera)")]
    public Transform rigRoot;

    [Header("Movement")]
    public float panDuration = 0.9f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 defaultPos;
    private bool hasDefault;
    public Vector3 focusOffset = Vector3.zero;

    [Header("Top-down focus")]
    public bool useTopDownOnFocus = true;
    public float topDownHeight = 20f;
    public float topDownHoldSeconds = 0.25f;
    public float topDownZoomHeight = 12f;
    public float topDownPitch = 90f;
    public Transform topDownCenter;
    public Vector3 topDownZoomOffset = Vector3.zero;
    public Transform lookPivot;

    private Vector3 lastPos;
    private bool hasLast;
    private Quaternion lastLookLocalRotation;
    private bool hasLastLookRotation;

    void Awake()
    {
        if (rigRoot == null)
            rigRoot = Camera.main != null ? Camera.main.transform.root : null;

        if (rigRoot != null)
        {
            defaultPos = rigRoot.position;
            hasDefault = true;
        }

        if (topDownCenter == null)
        {
            GameObject house = GameObject.Find("House");
            if (house != null) topDownCenter = house.transform;
        }

        if (lookPivot == null && rigRoot != null)
        {
            Camera cam = rigRoot.GetComponentInChildren<Camera>();
            if (cam != null) lookPivot = cam.transform;
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
        yield return Focus(target, useTopDownOnFocus);
    }

    public IEnumerator Focus(Transform target, bool useTopDown)
    {
        if (rigRoot == null || target == null) yield break;
        if (!hasDefault) SetDefaultFromCurrent();

        lastPos = rigRoot.position;
        hasLast = true;

        if (useTopDown)
        {
            CacheLookRotation();
            ApplyTopDownRotation();

            Vector3 centerPos = topDownCenter != null ? topDownCenter.position : target.position;
            Vector3 topDownPos = new Vector3(centerPos.x, topDownHeight, centerPos.z);
            yield return PanTo(topDownPos);

            if (topDownHoldSeconds > 0f)
                yield return new WaitForSeconds(topDownHoldSeconds);

            float zoomHeight = topDownZoomHeight > 0f ? topDownZoomHeight : topDownHeight;
            Vector3 zoomPos = new Vector3(
                target.position.x + topDownZoomOffset.x,
                zoomHeight + topDownZoomOffset.y,
                target.position.z + topDownZoomOffset.z);
            yield return PanTo(zoomPos);
            yield break;
        }

        Vector3 focusPos = new Vector3(
            target.position.x + focusOffset.x,
            lastPos.y + focusOffset.y,
            target.position.z + focusOffset.z);

        yield return PanTo(focusPos);
    }


    public IEnumerator ReturnToDefault()
    {
        if (rigRoot == null || !hasDefault) yield break;
        yield return PanTo(defaultPos);
        RestoreLookRotation();
    }

    public IEnumerator ReturnToLastPosition()
    {
        if (rigRoot == null || !hasLast) yield break;
        yield return PanTo(lastPos);
        RestoreLookRotation();
    }

    private IEnumerator PanTo(Vector3 worldPos)
    {
        IsPanning = true;

        Vector3 start = rigRoot.position;
        Vector3 end = worldPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, panDuration);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            rigRoot.position = Vector3.Lerp(start, end, k);
            yield return null;
        }
        rigRoot.position = end;

        IsPanning = false;
    }

    private void CacheLookRotation()
    {
        if (lookPivot == null) return;
        lastLookLocalRotation = lookPivot.localRotation;
        hasLastLookRotation = true;
    }

    private void RestoreLookRotation()
    {
        if (lookPivot == null || !hasLastLookRotation) return;
        lookPivot.localRotation = lastLookLocalRotation;
        hasLastLookRotation = false;
    }

    private void ApplyTopDownRotation()
    {
        if (lookPivot == null) return;
        Vector3 euler = lookPivot.localEulerAngles;
        euler.x = topDownPitch;
        euler.z = 0f;
        lookPivot.localEulerAngles = euler;
    }
}
