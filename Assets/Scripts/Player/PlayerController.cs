using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 1.2f;
    [SerializeField] private float sprintSpeed = 2.4f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private Transform cam;
    [SerializeField] private PlayerAnimator playerAnimator;

    private CharacterController cc;
    private float movementX;
    private float movementY;
    private float verticalVelocity;
    private bool isSprintingMode;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (playerAnimator == null) playerAnimator = GetComponent<PlayerAnimator>();
    }

    void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();
        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    void Update()
    {
        // Nhấn phím Ctrl để bật/tắt chế độ chạy
        if (Keyboard.current != null && 
           (Keyboard.current.leftCtrlKey.wasPressedThisFrame || Keyboard.current.rightCtrlKey.wasPressedThisFrame))
        {
            isSprintingMode = !isSprintingMode;
        }

        if (cc.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 camF = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 camR = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        Vector3 move = camF * movementY + camR * movementX;

        if (playerAnimator != null && playerAnimator.IsAttacking) move = Vector3.zero;

        float currentSpeed = isSprintingMode ? sprintSpeed : speed;

        if (move.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), turnSpeed * Time.deltaTime);

        cc.Move((move * currentSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    public bool IsSprintingMode => isSprintingMode;
}