using UnityEngine;

public sealed class QuestGiverInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string speakerName = "Mira";
    [SerializeField] private QuestDefinition quest;

    public string InteractionPrompt => "Talk";

    public void Interact(PlayerInteractor interactor)
    {
        var player = interactor.gameObject;
        var log = player.GetComponent<PlayerQuestLog>();
        var inventory = player.GetComponent<PlayerInventory>();

        if (quest == null || log == null || inventory == null)
        {
            interactor.ShowMessage(speakerName, "Something is missing. Come back later.");
            return;
        }

        var state = log.GetState(quest);
        if (state == QuestState.NotStarted)
        {
            log.StartQuest(quest);
            interactor.ShowMessage(speakerName, quest.Description);
            return;
        }

        if (state == QuestState.Completed)
        {
            interactor.ShowMessage(speakerName, "The lantern burns a little brighter because of you.");
            return;
        }

        var count = inventory.CountItem(quest.RequiredItem);
        if (count >= quest.RequiredQuantity)
        {
            inventory.TryRemoveItem(quest.RequiredItem, quest.RequiredQuantity);
            inventory.AddCoins(quest.CoinReward);
            log.CompleteQuest(quest);
            interactor.ShowMessage(speakerName, "You found it. Take these coins for the road.");
        }
        else
        {
            interactor.ShowMessage(speakerName, $"Bring me {quest.RequiredQuantity} {quest.RequiredItem.DisplayName}. You have {count}.");
        }
    }
}
