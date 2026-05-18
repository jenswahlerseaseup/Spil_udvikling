using System.Collections;
using UnityEngine;

// Top-down visual hop: the sprite child rises and falls while the physics collider
// stays on the ground. Not a platform jump — used to clear small obstacles visually.
[RequireComponent(typeof(TopDownPlayerMotor))]
public sealed class PlayerJump : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField, Min(0f)] private float jumpDuration = 0.35f;
    [SerializeField, Min(0f)] private float jumpHeight   = 0.3f;

    private PlayerInputReader inputReader;

    public bool IsJumping { get; private set; }

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        if (visual == null) visual = transform.Find("Visuals") ?? transform;
    }

    private void OnEnable()  { if (inputReader != null) inputReader.JumpPressed += OnJumpPressed; }
    private void OnDisable() { if (inputReader != null) inputReader.JumpPressed -= OnJumpPressed; }

    private void OnJumpPressed()
    {
        if (IsJumping || !GameManager.CanPlayerAct) return;
        StartCoroutine(JumpRoutine());
    }

    private IEnumerator JumpRoutine()
    {
        IsJumping = true;
        var baseY  = visual.localPosition.y;
        var elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            var t       = elapsed / jumpDuration;
            var yOffset = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            var pos     = visual.localPosition;
            pos.y       = baseY + yOffset;
            visual.localPosition = pos;
            yield return null;
        }

        var finalPos = visual.localPosition;
        finalPos.y   = baseY;
        visual.localPosition = finalPos;
        IsJumping = false;
    }
}
