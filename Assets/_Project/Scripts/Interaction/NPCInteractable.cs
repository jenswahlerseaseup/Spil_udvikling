using UnityEngine;

// Handles idle chat, quest start, quest progress, and quest hand-in for one NPC.
// Dialogue assets are optional; fallback messages keep prototype NPCs functional.
public sealed class NPCInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string speakerName = "Gaardejer";

    [Header("Idle")]
    [SerializeField] private DialogueDefinition idleDialogue;

    [Header("Quest")]
    [SerializeField] private QuestDefinition questToGive;
    [SerializeField] private DialogueDefinition questOfferDialogue;
    [SerializeField] private DialogueDefinition questActiveDialogue;
    [SerializeField] private DialogueDefinition questCompleteDialogue;
    [SerializeField] private DialogueDefinition questDoneDialogue;

    public string InteractionPrompt => "Tal";

    public void Interact(PlayerInteractor interactor)
    {
        if (questToGive == null)
        {
            OpenOrFallback(interactor, idleDialogue, "Sikke en dag paa gaarden.");
            return;
        }

        var questManager = QuestManager.Instance;
        var status = questManager != null ? questManager.GetStatus(questToGive.QuestId) : QuestState.NotStarted;

        switch (status)
        {
            case QuestState.NotStarted:
                questManager?.StartQuest(questToGive.QuestId);
                OpenOrFallback(interactor, questOfferDialogue, questToGive.Description);
                break;

            case QuestState.Active:
                if (questManager?.IsReadyToComplete(questToGive.QuestId) == true)
                {
                    questManager.CompleteQuest(questToGive.QuestId, interactor.Inventory);
                    OpenOrFallback(interactor, questCompleteDialogue, "Flot klaret. Her er din beloenning.");
                }
                else
                {
                    OpenOrFallback(interactor, questActiveDialogue, "Der mangler stadig hoens.");
                }
                break;

            case QuestState.Completed:
                OpenOrFallback(interactor, questDoneDialogue, "Tak for hjaelpen, Emil.");
                break;
        }
    }

    private void OpenOrFallback(PlayerInteractor interactor, DialogueDefinition definition, string fallback)
    {
        if (definition != null && !definition.IsEmpty && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.Open(definition);
        }
        else
        {
            interactor.ShowMessage(speakerName, fallback);
        }
    }
}
