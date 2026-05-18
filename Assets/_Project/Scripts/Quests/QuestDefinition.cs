using UnityEngine;

[CreateAssetMenu(menuName = "Nyt Spil/Quests/Quest")]
public sealed class QuestDefinition : ScriptableObject
{
    [SerializeField] private string questId = "quest";
    [SerializeField] private string title = "Quest";
    [SerializeField, TextArea] private string description;
    [SerializeField] private QuestStep[] steps;
    [SerializeField] private ItemDefinition[] rewardItems;
    [SerializeField, Min(0)] private int rewardCoins;

    public string         QuestId      => questId;
    public string         Id           => questId; // Backwards-compatible alias for older prototype systems.
    public string         Title        => title;
    public string         Description  => description;
    public QuestStep[]    Steps        => steps;
    public ItemDefinition[] RewardItems  => rewardItems;
    public int            RewardCoins  => rewardCoins;
}
