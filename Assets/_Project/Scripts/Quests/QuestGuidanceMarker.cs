using UnityEngine;

/// <summary>
/// Small world-space quest marker. It observes one quest and toggles itself for
/// the current objective, keeping guidance visible without adding a minimap yet.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class QuestGuidanceMarker : MonoBehaviour
{
    public enum VisibilityRule
    {
        WhenQuestNotStarted,
        WhenQuestActive,
        WhenQuestActiveAndIncomplete,
        WhenQuestReadyToComplete,
    }

    [SerializeField] private string questId;
    [SerializeField] private VisibilityRule visibilityRule = VisibilityRule.WhenQuestActive;
    [SerializeField] private string prerequisiteQuestId;
    [SerializeField] private QuestState prerequisiteStatus = QuestState.Completed;
    [SerializeField] private ShakeableTree linkedTree;
    [SerializeField] private ChickenInteractable linkedChicken;
    [SerializeField, Min(0f)] private float bobHeight = 0.12f;
    [SerializeField, Min(0f)] private float bobSpeed = 2.4f;

    private Vector3 localStart;
    private SpriteRenderer markerRenderer;

    private void Awake()
    {
        markerRenderer = GetComponent<SpriteRenderer>();
        localStart = transform.localPosition;
    }

    private void OnEnable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.QuestUpdated += RefreshVisibility;
        }

        RefreshVisibility();
    }

    private void OnDisable()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.QuestUpdated -= RefreshVisibility;
        }
    }

    private void Update()
    {
        transform.localPosition = localStart + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    public void Configure(string newQuestId, VisibilityRule newRule, ShakeableTree treeToTrack,
        string newPrerequisiteQuestId = null, QuestState newPrerequisiteStatus = QuestState.Completed)
    {
        questId = newQuestId;
        visibilityRule = newRule;
        linkedTree = treeToTrack;
        linkedChicken = null;
        prerequisiteQuestId = newPrerequisiteQuestId;
        prerequisiteStatus = newPrerequisiteStatus;
        RefreshVisibility();
    }

    public void ConfigureChicken(string newQuestId, VisibilityRule newRule, ChickenInteractable chickenToTrack)
    {
        questId = newQuestId;
        visibilityRule = newRule;
        linkedTree = null;
        linkedChicken = chickenToTrack;
        prerequisiteQuestId = null;
        prerequisiteStatus = QuestState.Completed;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        if (markerRenderer == null)
        {
            markerRenderer = GetComponent<SpriteRenderer>();
        }

        var visible = ShouldBeVisible();
        markerRenderer.enabled = visible;
    }

    private bool ShouldBeVisible()
    {
        if (string.IsNullOrWhiteSpace(questId) || QuestManager.Instance == null)
        {
            return false;
        }

        if (linkedTree != null && linkedTree.HasBeenShaken)
        {
            return false;
        }

        if (linkedChicken != null && linkedChicken.HasBeenCaught)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(prerequisiteQuestId) &&
            QuestManager.Instance.GetStatus(prerequisiteQuestId) != prerequisiteStatus)
        {
            return false;
        }

        var status = QuestManager.Instance.GetStatus(questId);
        var ready = QuestManager.Instance.IsReadyToComplete(questId);

        return visibilityRule switch
        {
            VisibilityRule.WhenQuestNotStarted => status == QuestState.NotStarted,
            VisibilityRule.WhenQuestActive => status == QuestState.Active,
            VisibilityRule.WhenQuestActiveAndIncomplete => status == QuestState.Active && !ready,
            VisibilityRule.WhenQuestReadyToComplete => status == QuestState.Active && ready,
            _ => false,
        };
    }
}
