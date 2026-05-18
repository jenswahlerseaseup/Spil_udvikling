using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public sealed class LootDropper : MonoBehaviour
{
    [SerializeField] private ItemDefinition item;
    [SerializeField] private Sprite lootSprite;
    [SerializeField, Min(0)] private int coins = 1;
    [SerializeField, Min(1)] private int quantity = 1;
    [SerializeField] private int lootSortingOrder = 12;
    [SerializeField, Min(0f)] private float colliderRadius = 0.35f;

    private HealthSystem health;

    private void Awake()
    {
        health = GetComponent<HealthSystem>();
    }

    private void OnEnable()
    {
        health.Died += DropLoot;
    }

    private void OnDisable()
    {
        health.Died -= DropLoot;
    }

    private void DropLoot()
    {
        var pickup = new GameObject("Loot Pickup");
        pickup.transform.position = transform.position;

        var renderer = pickup.AddComponent<SpriteRenderer>();
        renderer.sprite = lootSprite;
        renderer.sortingOrder = lootSortingOrder;

        var collider = pickup.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = colliderRadius;

        pickup.AddComponent<PickupItem>().Configure(item, quantity, coins);

        Destroy(gameObject);
    }
}
