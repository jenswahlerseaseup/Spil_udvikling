using UnityEngine;

public sealed class ChickenWander : MonoBehaviour
{
    [SerializeField, Min(0f)] private float roamRadius = 1.2f;
    [SerializeField, Min(0f)] private float moveSpeed = 0.55f;
    [SerializeField, Min(0f)] private float fleeDistance = 1.25f;
    [SerializeField, Min(0f)] private float fleeSpeed = 1.8f;
    [SerializeField, Min(0.1f)] private float retargetInterval = 1.3f;

    private Transform player;
    private Vector3 homePosition;
    private Vector3 targetPosition;
    private float nextRetargetTime;

    private void Awake()
    {
        homePosition = transform.position;
        targetPosition = homePosition;
    }

    private void Start()
    {
        var playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
    }

    private void Update()
    {
        if (player != null)
        {
            var awayFromPlayer = transform.position - player.position;
            if (awayFromPlayer.sqrMagnitude <= fleeDistance * fleeDistance)
            {
                MoveToward(ClampToRoamArea(transform.position + awayFromPlayer.normalized * roamRadius), fleeSpeed);
                return;
            }
        }

        if (Time.time >= nextRetargetTime || Vector3.Distance(transform.position, targetPosition) < 0.08f)
        {
            PickNewTarget();
        }

        MoveToward(targetPosition, moveSpeed);
    }

    private void PickNewTarget()
    {
        nextRetargetTime = Time.time + retargetInterval;
        var offset = Random.insideUnitCircle * roamRadius;
        targetPosition = homePosition + new Vector3(offset.x, offset.y, 0f);
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
