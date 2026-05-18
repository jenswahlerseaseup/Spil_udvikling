using UnityEngine;

public sealed class ContactDamage : MonoBehaviour
{
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField, Min(0f)] private float cooldown = 0.9f;
    [SerializeField] private LayerMask targetMask = ~0;

    private float nextDamageTime;

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (Time.time < nextDamageTime || (targetMask.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (other.TryGetComponent<HealthSystem>(out var health))
        {
            health.TakeDamage(damage);
            nextDamageTime = Time.time + cooldown;
        }
    }
}
