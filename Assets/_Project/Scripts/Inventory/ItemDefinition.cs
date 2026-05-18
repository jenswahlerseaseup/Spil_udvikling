using UnityEngine;

[CreateAssetMenu(menuName = "Nyt Spil/Inventory/Item")]
public sealed class ItemDefinition : ScriptableObject
{
    [SerializeField] private string id = "item";
    [SerializeField] private string displayName = "Item";
    [SerializeField, Min(0)] private int value = 1;
    [SerializeField] private Sprite icon;

    public string Id => id;
    public string DisplayName => displayName;
    public int Value => value;
    public Sprite Icon => icon;
}
