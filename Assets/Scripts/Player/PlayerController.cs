using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    private Rigidbody rb;
    private float movementX;
    private float movementY;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();
        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    void FixedUpdate()
    {
        // 1. Tạo hướng di chuyển
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        rb.linearVelocity = new Vector3(movementX * speed, rb.linearVelocity.y, movementY * speed);
    }
}