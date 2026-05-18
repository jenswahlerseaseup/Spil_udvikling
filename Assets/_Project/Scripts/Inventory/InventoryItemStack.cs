using System;
using UnityEngine;

[Serializable]
public sealed class InventoryItemStack
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int quantity;

    public ItemDefinition Item => item;
    public int Quantity => quantity;

    public InventoryItemStack(ItemDefinition item, int quantity)
    {
        this.item = item;
        this.quantity = Mathf.Max(0, quantity);
    }

    public void Add(int amount)
    {
        if (amount > 0) quantity += amount;
    }

    public bool TryRemove(int amount)
    {
        if (amount <= 0 || amount > quantity) return false;
        quantity -= amount;
        return true;
    }
}
