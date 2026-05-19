using UnityEngine;

public sealed class FollowCamera2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new(0f, 0f, -10f);
    [SerializeField, Min(0f)] private float smoothTime = 0.12f;
    [SerializeField] private bool useBounds;
    [SerializeField] private Bounds worldBounds = new(Vector3.zero, new Vector3(48f, 32f, 0f));

    private Vector3 velocity;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    public void SetWorldBounds(Bounds bounds)
    {
        worldBounds = bounds;
        useBounds = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        var desiredPosition = target.position + offset;

        if (useBounds && cam != null)
        {
            var halfH = cam.orthographicSize;
            var halfW = halfH * cam.aspect;
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, worldBounds.min.x + halfW, worldBounds.max.x - halfW);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, worldBounds.min.y + halfH, worldBounds.max.y - halfH);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
    }
}
