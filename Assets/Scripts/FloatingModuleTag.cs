using TMPro;
using UnityEngine;

public class FloatingModuleTag : MonoBehaviour
{
    public TMP_Text labelText;
    public Vector3 offset = new Vector3(0f, 2.5f, 0f);

    private StationModule module;
    private Camera cam;

    public void Attach(StationModule m)
    {
        module = m;
        cam = Camera.main;
        Refresh();
    }

    private void Refresh()
    {
        if (labelText == null || module == null) return;
        labelText.text = $"{module.type}\nHP: {module.Health}/{module.maxHealth}";
    }

    void LateUpdate()
    {
        if (module == null) { Destroy(gameObject); return; }

        transform.position = module.transform.position + offset;

        if (cam != null)
            transform.forward = cam.transform.forward;

        Refresh(); // <- keeps HP updated
    }
}

