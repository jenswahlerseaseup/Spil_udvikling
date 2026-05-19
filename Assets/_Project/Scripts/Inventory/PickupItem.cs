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

        var feedback = string.Empty;
        if (coins > 0)
        {
            inventory.AddCoins(coins);
            feedback = "+" + coins + " moenter";
        }

        if (item != null)
        {
            inventory.AddItem(item, quantity);
            feedback = "+" + quantity + " " + item.DisplayName;
        }

        if (!string.IsNullOrEmpty(feedback))
        {
            WorldFeedbackText.Spawn(transform.position + Vector3.up * 0.35f, feedback, new Color(1f, 0.9f, 0.45f));
            GameHud.Instance?.ShowNotification(feedback, 1.2f);
        }

        Destroy(gameObject);
    }
}
