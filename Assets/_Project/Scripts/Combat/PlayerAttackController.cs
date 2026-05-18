using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(TopDownPlayerMotor))]
public sealed class PlayerAttackController : MonoBehaviour
{
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField, Min(0f)] private float range = 0.85f;
    [SerializeField, Min(0f)] private float radius = 0.45f;
    [SerializeField, Min(0f)] private float cooldown = 0.28f;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private SpriteRenderer attackFlash;

    private PlayerInputReader inputReader;
    private TopDownPlayerMotor motor;
    private float nextAttackTime;
    private Coroutine flashRoutine;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        motor = GetComponent<TopDownPlayerMotor>();
    }

    private void OnEnable()
    {
        inputReader.AttackPressed += Attack;
    }

    private void OnDisable()
    {
        inputReader.AttackPressed -= Attack;
    }

    private void Attack()
    {
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + cooldown;

        var facing = motor.FacingDirection.sqrMagnitude > 0.01f ? motor.FacingDirection.normalized : Vector2.down;
        var center = (Vector2)transform.position + facing * range;
        var hits = Physics2D.OverlapCircleAll(center, radius, targetMask);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<HealthSystem>(out var health))
            {
                health.TakeDamage(damage);
            }
        }

        ShowAttackFlash(facing);
    }

    private void ShowAttackFlash(Vector2 facing)
    {
        if (attackFlash == null)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine(facing));
    }

    private IEnumerator FlashRoutine(Vector2 facing)
    {
        attackFlash.transform.localPosition = facing * range;
        attackFlash.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
        attackFlash.enabled = true;
        yield return new WaitForSeconds(0.09f);
        attackFlash.enabled = false;
    }
}
