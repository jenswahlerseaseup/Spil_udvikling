using UnityEngine;

/// <summary>
/// Handles idle chat, quest start, progress check, and hand-in for up to two sequential quests.
/// All dialogue assets are optional; fallback strings keep the NPC functional without them.
/// </summary>
public sealed class NPCInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string speakerName = "Gaardejer";

    [Header("Idle (no quest)")]
    [SerializeField] private DialogueDefinition idleDialogue;
    [SerializeField] private string idleFallback = "Sikke en dag paa gaarden.";

    [Header("First Quest")]
    [SerializeField] private QuestDefinition questToGive;
    [SerializeField] private DialogueDefinition questOfferDialogue;
    [SerializeField] private DialogueDefinition questActiveDialogue;
    [SerializeField] private DialogueDefinition questCompleteDialogue;
    [SerializeField] private DialogueDefinition questDoneDialogue;

    [Header("Second Quest (activates after first is complete)")]
    [SerializeField] private QuestDefinition secondQuestToGive;
    [SerializeField] private DialogueDefinition secondQuestOfferDialogue;
    [SerializeField] private DialogueDefinition secondQuestActiveDialogue;
    [SerializeField] private DialogueDefinition secondQuestCompleteDialogue;
    [SerializeField] private DialogueDefinition secondQuestDoneDialogue;

    public string InteractionPrompt => "Tal";

    public void Interact(PlayerInteractor interactor)
    {
        if (questToGive == null)
        {
            Speak(interactor, idleDialogue, idleFallback);
            return;
        }

        var qm          = QuestManager.Instance;
        var firstStatus = qm != null ? qm.GetStatus(questToGive.QuestId) : QuestState.NotStarted;

        // ── First quest still in progress ──────────────────────────────────────
        if (firstStatus != QuestState.Completed)
        {
            HandleQuest(interactor, qm, questToGive, firstStatus,
                questOfferDialogue, questActiveDialogue, questCompleteDialogue, questDoneDialogue);
            return;
        }

        // ── First quest done — check second quest ──────────────────────────────
        if (secondQuestToGive == null)
        {
            Speak(interactor, questDoneDialogue, "Tak for hjaelpen, Emil.");
            return;
        }

        var secondStatus = qm != null ? qm.GetStatus(secondQuestToGive.QuestId) : QuestState.NotStarted;
        HandleQuest(interactor, qm, secondQuestToGive, secondStatus,
            secondQuestOfferDialogue, secondQuestActiveDialogue,
            secondQuestCompleteDialogue, secondQuestDoneDialogue);
    }

    private void HandleQuest(PlayerInteractor interactor, QuestManager qm,
        QuestDefinition quest, QuestState status,
        DialogueDefinition offerDlg, DialogueDefinition activeDlg,
        DialogueDefinition completeDlg, DialogueDefinition doneDlg)
    {
        switch (status)
        {
            case QuestState.NotStarted:
                qm?.StartQuest(quest.QuestId);
                Speak(interactor, offerDlg, quest.Description);
                break;

            case QuestState.Active:
                if (qm?.IsReadyToComplete(quest.QuestId) == true)
                {
                    qm.CompleteQuest(quest.QuestId, interactor.Inventory);
                    Speak(interactor, completeDlg, "Flot klaret! Her er din beloenning.");
                }
                else
                {
                    Speak(interactor, activeDlg, "Der er stadig noget at goere.");
                }
                break;

            case QuestState.Completed:
                Speak(interactor, doneDlg, "Tak for hjaelpen, Emil.");
                break;
        }
    }

    private void Speak(PlayerInteractor interactor, DialogueDefinition definition, string fallback)
    {
        if (definition != null && !definition.IsEmpty && DialogueManager.Instance != null)
            DialogueManager.Instance.Open(definition);
        else
            interactor.ShowMessage(speakerName, fallback);
    }
}
