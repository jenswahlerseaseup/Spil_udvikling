using System;

[Serializable]
public sealed class InventoryItemStack
{
    public ItemDefinition item;
    public int quantity;

    public InventoryItemStack(ItemDefinition item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}
