using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private Transform cam;
    private CharacterController cc;
    private float movementX;
    private float movementY;
    private float verticalVelocity;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();
        movementX = movementVector.x;
        movementY = movementVector.y;
    }


    void Update()
    {
        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 camF = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 camR = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        Vector3 move = camF * movementY + camR * movementX;

        if (move.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(move) * Quaternion.Euler(0f, 180f, 0f);

        cc.Move((move * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }
}