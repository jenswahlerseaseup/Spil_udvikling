using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class ChickenInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string questId = "collect_chickens";
    [SerializeField, Min(0)] private int mischiefOnCatch;

    private bool caught;
    private bool isSubscribedToQuestUpdates;

    public bool HasBeenCaught => caught;

    public string InteractionPrompt => caught ? string.Empty : "Fang";

    private void OnEnable()
    {
        TrySubscribeToQuestUpdates();
        RefreshQuestVisibility();
    }

    private void Start()
    {
        TrySubscribeToQuestUpdates();
        RefreshQuestVisibility();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null && isSubscribedToQuestUpdates)
        {
            QuestManager.Instance.QuestUpdated -= RefreshQuestVisibility;
        }

        isSubscribedToQuestUpdates = false;
    }

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

        var progress = questManager.IsReadyToComplete(questId)
            ? "Alle hoens er fanget. Tilbage til gaardejeren!"
            : "Hoene fanget!";
        WorldFeedbackText.Spawn(transform.position + Vector3.up * 0.65f, "Fanget!", new Color(1f, 0.95f, 0.55f));
        interactor.ShowMessage(string.Empty, progress);
        gameObject.SetActive(false);
    }

    private void RefreshQuestVisibility()
    {
        var questManager = QuestManager.Instance;
        if (questManager == null)
        {
            return;
        }

        var shouldHide =
            questManager.GetStatus(questId) == QuestState.Completed ||
            questManager.IsReadyToComplete(questId);

        if (shouldHide)
        {
            gameObject.SetActive(false);
        }
    }

    private void TrySubscribeToQuestUpdates()
    {
        if (isSubscribedToQuestUpdates || QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.QuestUpdated += RefreshQuestVisibility;
        isSubscribedToQuestUpdates = true;
    }
}
