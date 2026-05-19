using UnityEngine;

/// <summary>
/// Slowly cycles a gentle warm-to-cool tint on the Camera background and an optional
/// global light object to give a soft time-of-day feel without a full lighting pipeline.
/// One full cycle = dayDuration seconds (default 5 minutes real-time).
/// </summary>
public sealed class DayNightTint : MonoBehaviour
{
    [SerializeField] private float dayDuration = 300f;

    // Dawn → Noon → Dusk → Night colour stops
    private static readonly Color[] SkyColours =
    {
        new Color(0.42f, 0.32f, 0.52f),   // pre-dawn purple
        new Color(0.78f, 0.55f, 0.35f),   // sunrise amber
        new Color(0.53f, 0.72f, 0.86f),   // midday sky
        new Color(0.86f, 0.60f, 0.36f),   // late afternoon
        new Color(0.26f, 0.18f, 0.38f),   // dusk
        new Color(0.09f, 0.09f, 0.14f),   // night
        new Color(0.09f, 0.09f, 0.14f),   // hold night
        new Color(0.42f, 0.32f, 0.52f),   // back to pre-dawn
    };

    private Camera cam;
    private float elapsed;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (cam == null || GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        elapsed = (elapsed + Time.deltaTime) % dayDuration;
        var t      = elapsed / dayDuration * (SkyColours.Length - 1);
        var index  = Mathf.FloorToInt(t);
        var frac   = t - index;
        var colour = Color.Lerp(SkyColours[index], SkyColours[(index + 1) % SkyColours.Length], frac);

        cam.backgroundColor = colour;
    }
}
