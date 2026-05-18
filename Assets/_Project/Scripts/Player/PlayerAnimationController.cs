using UnityEngine;

[RequireComponent(typeof(TopDownPlayerMotor))]
public sealed class PlayerAnimationController : MonoBehaviour
{
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");
    private static readonly int FacingX = Animator.StringToHash("FacingX");
    private static readonly int FacingY = Animator.StringToHash("FacingY");
    private static readonly int Speed = Animator.StringToHash("Speed");

    [SerializeField] private Animator animator;

    private TopDownPlayerMotor motor;

    private void Awake()
    {
        motor = GetComponent<TopDownPlayerMotor>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        var input = motor.MoveInput;
        var facing = motor.FacingDirection;

        animator.SetFloat(MoveX, input.x);
        animator.SetFloat(MoveY, input.y);
        animator.SetFloat(FacingX, facing.x);
        animator.SetFloat(FacingY, facing.y);
        animator.SetFloat(Speed, input.magnitude);
    }
}
