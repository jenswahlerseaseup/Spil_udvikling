using UnityEngine;

// Legacy shim kept so old scenes show a clear message instead of silently failing.
public sealed class QuestGiverInteractable : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => "Talk";

    public void Interact(PlayerInteractor interactor)
    {
        interactor.ShowMessage("Old NPC", "This NPC uses the old quest system. Replace it with NPCInteractable.");
    }

    private void Awake()
    {
        Debug.LogWarning("QuestGiverInteractable is deprecated. Use NPCInteractable instead.", this);
    }
}
