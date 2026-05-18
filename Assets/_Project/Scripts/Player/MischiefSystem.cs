using System;
using UnityEngine;

public sealed class MischiefSystem : Singleton<MischiefSystem>
{
    public event Action<int, string> MischiefAdded;

    public int Points { get; private set; }

    public void AddMischief(int amount, string reason)
    {
        if (amount <= 0) return;
        Points += amount;
        Debug.Log($"[Ballade] {reason} (+{amount}) = {Points}");
        MischiefAdded?.Invoke(Points, reason);
    }

    public void Reset() => Points = 0;

    public void LoadState(int points) => Points = Mathf.Max(0, points);
}
