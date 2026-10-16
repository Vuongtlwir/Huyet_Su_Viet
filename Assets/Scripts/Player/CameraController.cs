using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 1.5f;
    [SerializeField] private float sensitivity = 0.15f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float cameraRadius = 0.2f; // Độ dày bán kính va chạm camera

    private float yaw;
    private float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue() * sensitivity;
        yaw += delta.x;
        pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 pivot = target.position + Vector3.up * height;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetDir = rot * new Vector3(0f, 0f, -distance);
        Vector3 desired = pivot + targetDir;

        // Dùng SphereCast thay vì Linecast để tính tới độ dày/bán kính của Camera
        if (Physics.SphereCast(pivot, cameraRadius, targetDir.normalized, out RaycastHit hit, distance, collisionMask))
        {
            // Đẩy camera ra xa điểm va chạm một khoảng bằng cameraRadius
            desired = pivot + targetDir.normalized * Mathf.Max(hit.distance - cameraRadius, 0.4f);
        }

        transform.position = desired;
        transform.rotation = rot;
    }
}