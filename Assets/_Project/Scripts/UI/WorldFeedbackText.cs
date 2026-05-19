using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Lightweight world-space text burst for pickups and short gameplay feedback.
/// It is intentionally self-contained so interactables can add juice without
/// depending on scene prefabs or a larger UI effects framework.
/// </summary>
public sealed class WorldFeedbackText : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.75f;
    [SerializeField] private float riseDistance = 0.55f;

    private TextMeshPro textMesh;
    private Vector3 startPosition;

    public static void Spawn(Vector3 position, string text, Color color)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var go = new GameObject("World Feedback");
        go.transform.position = position;

        var mesh = go.AddComponent<TextMeshPro>();
        mesh.text = text;
        mesh.fontSize = 3.2f;
        mesh.alignment = TextAlignmentOptions.Center;
        mesh.color = color;
        mesh.sortingOrder = 9500;

        go.AddComponent<WorldFeedbackText>();
    }

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        startPosition = transform.position;
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        var elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / lifetime);
            transform.position = startPosition + Vector3.up * (riseDistance * EaseOut(t));

            if (textMesh != null)
            {
                var color = textMesh.color;
                color.a = 1f - t;
                textMesh.color = color;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
}
