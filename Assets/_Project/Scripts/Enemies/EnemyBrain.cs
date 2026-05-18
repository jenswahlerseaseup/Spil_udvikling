using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class EnemyBrain : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 1.7f;
    [SerializeField, Min(0f)] private float chaseRange = 4f;
    [SerializeField] private Transform target;

    private Rigidbody2D body;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            return;
        }

        var toTarget = (Vector2)(target.position - transform.position);
        if (toTarget.sqrMagnitude > chaseRange * chaseRange)
        {
            return;
        }

        var direction = toTarget.normalized;
        body.MovePosition(body.position + direction * moveSpeed * Time.fixedDeltaTime);

        if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.1f)
        {
            spriteRenderer.flipX = direction.x < 0f;
        }
    }
}
