using UnityEngine;

public sealed class FollowCamera2D : MonoBehaviour
{
    // This lightweight fallback keeps the project playable before Cinemachine is configured.
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
    [SerializeField, Min(0f)] private float smoothTime = 0.12f;

    private Vector3 velocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        var desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
