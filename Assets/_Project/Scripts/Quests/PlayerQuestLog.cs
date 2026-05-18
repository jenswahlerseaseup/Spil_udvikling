using UnityEngine;

// Legacy shim kept so older prefabs/scenes fail gracefully while we migrate to QuestManager.
public sealed class PlayerQuestLog : MonoBehaviour
{
    private void Awake()
    {
        Debug.LogWarning("PlayerQuestLog is deprecated. Use QuestManager.Instance instead.", this);
    }
}
