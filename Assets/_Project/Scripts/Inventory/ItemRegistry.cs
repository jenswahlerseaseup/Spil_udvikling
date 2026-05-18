using UnityEngine;

[CreateAssetMenu(menuName = "Nyt Spil/Inventory/Item Registry")]
public sealed class ItemRegistry : ScriptableObject
{
    [SerializeField] private ItemDefinition[] items = {};

    public ItemDefinition Find(string id)
    {
        foreach (var item in items)
        {
            if (item != null && item.Id == id)
            {
                return item;
            }
        }

        return null;
    }
}
