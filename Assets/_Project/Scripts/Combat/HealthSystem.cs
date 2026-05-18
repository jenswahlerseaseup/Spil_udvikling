using System;
using UnityEngine;

public sealed class HealthSystem : MonoBehaviour
{
    public event Action<int, int> HealthChanged;
    public event Action Died;

    [SerializeField, Min(1)] private int maxHealth = 5;
    [SerializeField, Min(0f)] private float invincibilityDuration = 0f;

    private float invincibleUntil;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;
    public bool IsInvincible => Time.time < invincibleUntil;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0 || IsInvincible)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (invincibilityDuration > 0f)
        {
            invincibleUntil = Time.time + invincibilityDuration;
        }

        if (CurrentHealth == 0)
        {
            Died?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void LoadState(int currentHealth)
    {
        CurrentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
