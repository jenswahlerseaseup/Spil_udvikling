using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class PickupItem : MonoBehaviour
{
    [SerializeField] private ItemDefinition item;
    [SerializeField, Min(1)] private int quantity = 1;
    [SerializeField, Min(0)] private int coins;

    public void Configure(ItemDefinition itemDefinition, int itemQuantity, int coinAmount)
    {
        item = itemDefinition;
        quantity = Mathf.Max(1, itemQuantity);
        coins = Mathf.Max(0, coinAmount);
    }

    private void Reset()
    {
        var pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !other.TryGetComponent<PlayerInventory>(out var inventory))
        {
            return;
        }

        if (coins > 0)
        {
            inventory.AddCoins(coins);
        }

        if (item != null)
        {
            inventory.AddItem(item, quantity);
        }

        Destroy(gameObject);
    }
}
