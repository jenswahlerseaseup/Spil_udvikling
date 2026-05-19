using UnityEngine;

// Replaced by GameHud. Kept so existing scene references don't break on open.
[System.Obsolete("Use GameHud instead.")]
public sealed class PrototypeHud : MonoBehaviour
{
    private void Awake() =>
        Debug.LogWarning("PrototypeHud is deprecated. Replace it with GameHud in the scene.", this);
}
