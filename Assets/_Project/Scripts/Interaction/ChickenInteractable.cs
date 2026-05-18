using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ChickenInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string questId = "collect_chickens";
    [SerializeField, Min(0)] private int mischiefOnCatch;

    private bool caught;

    public string InteractionPrompt => caught ? string.Empty : "Fang";

    public void Interact(PlayerInteractor interactor)
    {
        if (caught)
        {
            return;
        }

        var questManager = QuestManager.Instance;
        if (questManager == null)
        {
            interactor.ShowMessage(string.Empty, "Quest-systemet mangler.");
            return;
        }

        var status = questManager.GetStatus(questId);
        if (status == QuestState.NotStarted)
        {
            interactor.ShowMessage(string.Empty, "Hoenen smutter vaek. Tal med gaardejeren foerst.");
            return;
        }

        if (status == QuestState.Completed)
        {
            interactor.ShowMessage(string.Empty, "Du har allerede samlet hoensene.");
            return;
        }

        caught = true;
        questManager.RecordProgress(questId);

        if (mischiefOnCatch > 0)
        {
            MischiefSystem.Instance?.AddMischief(mischiefOnCatch, "Caught a chicken in a chaotic way");
        }

        interactor.ShowMessage(string.Empty, "Fanget!");
        gameObject.SetActive(false);
    }
}
