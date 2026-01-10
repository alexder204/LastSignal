using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float minY = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            move += Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }

        if (Input.GetKey(KeyCode.S))
        {
            move -= Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        }

        if (Input.GetKey(KeyCode.A))
        {
            move -= Vector3.ProjectOnPlane(transform.right, Vector3.up);
        }

        if (Input.GetKey(KeyCode.D))
        {
            move += Vector3.ProjectOnPlane(transform.right, Vector3.up);
        }

        if (Input.GetKey(KeyCode.Z))
        {
            move += Vector3.down;
        }

        if (Input.GetKey(KeyCode.X))
        {
            move += Vector3.up;
        }

        if (move.sqrMagnitude > 0f)
        {
            transform.position += move.normalized * moveSpeed * Time.deltaTime;
        }

        if (transform.position.y < minY)
        {
            Vector3 clamped = transform.position;
            clamped.y = minY;
            transform.position = clamped;
        }

        float rotateInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rotateInput -= 1f;
        if (Input.GetKey(KeyCode.E)) rotateInput += 1f;
        if (Mathf.Abs(rotateInput) > 0f)
        {
            transform.Rotate(Vector3.up, rotateInput * rotateSpeed * Time.deltaTime, Space.World);
        }
    }
}
