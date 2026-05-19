using UnityEngine;

/// <summary>
/// Applies a gentle sine-wave sway to a SpriteRenderer's transform.
/// Attach to grass, tall plants, or light foliage objects.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class GrassSwayer : MonoBehaviour
{
    [SerializeField, Min(0f)] private float amplitude  = 2.5f;  // degrees
    [SerializeField, Min(0f)] private float frequency  = 0.8f;  // cycles per second
    [SerializeField, Range(0f, 1f)] private float phaseOffset;  // randomised in Awake

    private float baseAngle;

    private void Awake()
    {
        baseAngle   = transform.eulerAngles.z;
        phaseOffset = Random.value;
    }

    private void Update()
    {
        var angle = Mathf.Sin((Time.time * frequency + phaseOffset) * Mathf.PI * 2f) * amplitude;
        transform.rotation = Quaternion.Euler(0f, 0f, baseAngle + angle);
    }
}
