using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimator : MonoBehaviour
{
    private static readonly int IS_MOVING = Animator.StringToHash("IsMoving");
    private static readonly int IS_SPRINTING = Animator.StringToHash("IsSprinting");
    private static readonly int ATTACK = Animator.StringToHash("Attack");
    private static readonly int ATTACK_STATE = Animator.StringToHash("Armature|Sword_Attack");
    private const float MOVE_EPSILON_SQR = 0.01f;
    private const float ATTACK_ENTER_TIMEOUT = 0.5f;

    [SerializeField] private Animator animator;

    private CharacterController cc;
    private PlayerController playerController;
    private bool isAttacking;
    private bool attackStateEntered;
    private float attackTimer;

    public bool IsAttacking => isAttacking;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();

        if (animator == null) animator = FindAnimatorWithController();

    }

    Animator FindAnimatorWithController()
    {
        Animator[] all = GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < all.Length; i++)
            if (all[i].runtimeAnimatorController != null) return all[i];
        return null;
    }

    void Update()
    {
        if (animator == null) return;

        UpdateAttackState();

        if (cc == null) return;

        Vector3 velocity = cc.velocity;
        velocity.y = 0f;
        bool isMoving = velocity.sqrMagnitude > MOVE_EPSILON_SQR;

        // Chỉ kích hoạt animation Sprint khi nhân vật đang di chuyển VÀ đang ở chế độ Chạy
        bool isSprinting = isMoving && (playerController != null && playerController.IsSprintingMode);

        animator.SetBool(IS_MOVING, isMoving);
        animator.SetBool(IS_SPRINTING, isSprinting);
    }

    void UpdateAttackState()
    {
        if (!isAttacking) return;

        attackTimer += Time.deltaTime;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (state.shortNameHash == ATTACK_STATE)
        {
            attackStateEntered = true;
            if (state.normalizedTime >= 1f || animator.IsInTransition(0))
                EndAttack();
        }
        else if (attackStateEntered || attackTimer >= ATTACK_ENTER_TIMEOUT)
        {
            EndAttack();
        }
    }

    void EndAttack()
    {
        isAttacking = false;
        attackStateEntered = false;
        attackTimer = 0f;
    }

    void OnAttack(InputValue value)
    {
        if (animator == null || !value.isPressed || isAttacking) return;
        isAttacking = true;
        attackStateEntered = false;
        attackTimer = 0f;
        animator.SetTrigger(ATTACK);
    }

    public void OnAttackHit()
    {
        Debug.Log("Attack hit");
    }
}