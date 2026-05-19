using UnityEngine;

/// <summary>
/// Sorts top-down sprites by their world Y position so objects lower on screen
/// render in front. Keep this local and small; it replaces hand-tuned sorting
/// numbers for characters, props, trees, and pickups.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class AutoYSort2D : MonoBehaviour
{
    [SerializeField] private int baseOrder = 5000;
    [SerializeField] private int unitsPerOrder = 100;
    [SerializeField] private float footOffsetY;
    [SerializeField] private bool updateEveryFrame = true;

    private SpriteRenderer spriteRenderer;
    private Vector3 lastPosition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplySortOrder();
    }

    private void LateUpdate()
    {
        if (!updateEveryFrame && transform.position == lastPosition)
        {
            return;
        }

        ApplySortOrder();
    }

    public void Configure(int newBaseOrder, int newUnitsPerOrder, float newFootOffsetY, bool dynamicSort)
    {
        baseOrder = newBaseOrder;
        unitsPerOrder = Mathf.Max(1, newUnitsPerOrder);
        footOffsetY = newFootOffsetY;
        updateEveryFrame = dynamicSort;
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        ApplySortOrder();
    }

    private void ApplySortOrder()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        lastPosition = transform.position;
        var sortY = transform.position.y + footOffsetY;
        spriteRenderer.sortingOrder = baseOrder - Mathf.RoundToInt(sortY * unitsPerOrder);
    }
}
