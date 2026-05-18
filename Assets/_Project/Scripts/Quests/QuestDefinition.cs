using UnityEngine;

[CreateAssetMenu(menuName = "Nyt Spil/Quests/Quest")]
public sealed class QuestDefinition : ScriptableObject
{
    [SerializeField] private string id = "quest";
    [SerializeField] private string title = "Quest";
    [SerializeField, TextArea] private string description = "Quest description.";
    [SerializeField] private ItemDefinition requiredItem;
    [SerializeField, Min(1)] private int requiredQuantity = 1;
    [SerializeField, Min(0)] private int coinReward = 5;

    public string Id => id;
    public string Title => title;
    public string Description => description;
    public ItemDefinition RequiredItem => requiredItem;
    public int RequiredQuantity => requiredQuantity;
    public int CoinReward => coinReward;
}
