using UnityEngine;

// Legacy shim kept so older scenes fail gracefully while we migrate to SaveManager.
public sealed class SaveGameController : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogWarning("SaveGameController is deprecated. Use SaveManager instead.", this);
        enabled = false;
    }
}
