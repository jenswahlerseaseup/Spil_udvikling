using UnityEngine;

/// <summary>
/// Chickens wander within their home radius and flee from the player.
/// In panic mode (player very close) they sprint further away and zigzag unpredictably.
/// </summary>
public sealed class ChickenWander : MonoBehaviour
{
    [SerializeField, Min(0f)] private float roamRadius        = 3.5f;
    [SerializeField, Min(0f)] private float moveSpeed         = 1.1f;
    [SerializeField, Min(0f)] private float fleeDetectRadius  = 2.8f;
    [SerializeField, Min(0f)] private float fleeSpeed         = 3.2f;
    [SerializeField, Min(0f)] private float panicRadius       = 1.4f;
    [SerializeField, Min(0f)] private float panicSpeed        = 4.8f;
    [SerializeField, Min(0.1f)] private float retargetInterval = 1.4f;
    [SerializeField, Min(0.1f)] private float panicZigzagInterval = 0.25f;

    private Transform player;
    private Vector3   homePosition;
    private Vector3   targetPosition;
    private float     nextRetargetTime;
    private float     nextZigzagTime;
    private SpriteRenderer sr;

    private enum State { Wander, Flee, Panic }
    private State state;

    private void Awake()
    {
        homePosition   = transform.position;
        targetPosition = homePosition;
        sr             = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        player = p != null ? p.transform : null;
    }

    private void Update()
    {
        if (!GameManager.CanPlayerAct) return;

        var prevState = state;
        UpdateState();
        if (state != prevState) OnStateChanged();

        switch (state)
        {
            case State.Panic:
                HandlePanic();
                break;
            case State.Flee:
                HandleFlee();
                break;
            default:
                HandleWander();
                break;
        }

        if (sr != null)
            sr.flipX = (targetPosition.x - transform.position.x) < -0.05f;
    }

    private void UpdateState()
    {
        if (player == null) { state = State.Wander; return; }
        var dist = Vector2.Distance(transform.position, player.position);
        state = dist <= panicRadius  ? State.Panic :
                dist <= fleeDetectRadius ? State.Flee :
                                        State.Wander;
    }

    private void OnStateChanged()
    {
        if (state == State.Panic) nextZigzagTime = 0f;
    }

    private void HandlePanic()
    {
        if (Time.time >= nextZigzagTime)
        {
            nextZigzagTime = Time.time + panicZigzagInterval;
            var awayDir = (transform.position - player.position).normalized;
            // Zigzag: randomly bias left or right of the direct escape vector
            var perp    = new Vector3(-awayDir.y, awayDir.x, 0f);
            var bias    = Random.Range(-0.8f, 0.8f);
            var newDir  = (awayDir + perp * bias).normalized;
            targetPosition = ClampToRoamArea(transform.position + newDir * roamRadius);
        }
        MoveToward(targetPosition, panicSpeed);
    }

    private void HandleFlee()
    {
        var awayDir    = (transform.position - player.position).normalized;
        targetPosition = ClampToRoamArea(transform.position + awayDir * roamRadius);
        MoveToward(targetPosition, fleeSpeed);
    }

    private void HandleWander()
    {
        if (Time.time >= nextRetargetTime || Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            nextRetargetTime = Time.time + retargetInterval;
            var offset   = Random.insideUnitCircle * roamRadius;
            targetPosition = homePosition + new Vector3(offset.x, offset.y, 0f);
        }
        MoveToward(targetPosition, moveSpeed);
    }

    private void MoveToward(Vector3 target, float speed)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    private Vector3 ClampToRoamArea(Vector3 position)
    {
        var offset = position - homePosition;
        if (offset.sqrMagnitude > roamRadius * roamRadius)
        {
            offset = offset.normalized * roamRadius;
        }

        return homePosition + offset;
    }
}
