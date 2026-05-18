using UnityEngine;

[CreateAssetMenu(menuName = "Nyt Spil/Player/Movement Settings")]
public sealed class PlayerMovementSettings : ScriptableObject
{
    [Min(0f)] public float moveSpeed = 4.5f;
    [Min(1f)] public float runSpeedMultiplier = 1.6f;

    [Tooltip("Small inputs below this value are ignored to prevent controller drift.")]
    [Range(0f, 0.5f)] public float inputDeadZone = 0.12f;
}
