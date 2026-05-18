using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerInventory : MonoBehaviour
{
    public event Action Changed;

    [SerializeField, Min(0)] private int coins;
    [SerializeField] private List<InventoryItemStack> items = new();

    public int Coins => coins;
    public IReadOnlyList<InventoryItemStack> Items => items;

    public void AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        coins += amount;
        Changed?.Invoke();
    }

    public bool TrySpendCoins(int amount)
    {
        if (amount < 0 || coins < amount)
        {
            return false;
        }

        coins -= amount;
        Changed?.Invoke();
        return true;
    }

    public void AddItem(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            return;
        }

        var stack = items.Find(entry => entry.Item == item);
        if (stack == null)
        {
            items.Add(new InventoryItemStack(item, quantity));
        }
        else
        {
            stack.Add(quantity);
        }

        Changed?.Invoke();
    }

    public int CountItem(ItemDefinition item)
    {
        var stack = items.Find(entry => entry.Item == item);
        return stack != null ? stack.Quantity : 0;
    }

    public bool TryRemoveItem(ItemDefinition item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            return false;
        }

        var stack = items.Find(entry => entry.Item == item);
        if (stack == null || stack.Quantity < quantity)
        {
            return false;
        }

        stack.TryRemove(quantity);
        if (stack.Quantity == 0)
        {
            items.Remove(stack);
        }

        Changed?.Invoke();
        return true;
    }

    public void LoadState(int loadedCoins, List<InventoryItemStack> loadedItems)
    {
        coins = Mathf.Max(0, loadedCoins);
        items = loadedItems ?? new List<InventoryItemStack>();
        Changed?.Invoke();
    }
}
