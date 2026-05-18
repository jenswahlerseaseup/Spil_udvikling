using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class TopDownPlayerMotor : MonoBehaviour
{
    // Rigidbody2D movement keeps collision resolution in Unity's 2D physics instead of manually pushing transforms.
    [SerializeField] private PlayerMovementSettings movementSettings;
    [SerializeField] private PlayerInputReader inputReader;

    private Rigidbody2D body;
    private Vector2 moveInput;
    private Vector2 facingDirection = Vector2.down;

    public Vector2 MoveInput => moveInput;
    public Vector2 FacingDirection => facingDirection;
    public bool IsMoving => moveInput.sqrMagnitude > 0.0001f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (inputReader == null)
        {
            inputReader = GetComponent<PlayerInputReader>();
        }
    }

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.MoveChanged += SetMoveInput;
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.MoveChanged -= SetMoveInput;
        }
    }

    private void FixedUpdate()
    {
        if (!GameManager.CanPlayerAct)
        {
            body.MovePosition(body.position);
            return;
        }

        var speed = movementSettings != null ? movementSettings.moveSpeed : 4.5f;
        if (inputReader != null && inputReader.RunHeld && movementSettings != null)
            speed *= movementSettings.runSpeedMultiplier;

        body.MovePosition(body.position + moveInput * speed * Time.fixedDeltaTime);
    }

    private void Reset()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void SetMoveInput(Vector2 value)
    {
        var deadZone = movementSettings != null ? movementSettings.inputDeadZone : 0.12f;
        moveInput = value.sqrMagnitude < deadZone * deadZone ? Vector2.zero : Vector2.ClampMagnitude(value, 1f);

        if (moveInput.sqrMagnitude > 0.0001f)
        {
            facingDirection = moveInput.normalized;
        }
    }
}
