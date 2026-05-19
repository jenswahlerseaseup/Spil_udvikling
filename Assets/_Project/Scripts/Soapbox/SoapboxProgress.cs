using UnityEngine;

/// <summary>
/// Runtime state for the soapbox car. For now upgrades are derived directly from
/// collected parts so the prototype stays transparent and easy to rebalance.
/// </summary>
public sealed class SoapboxProgress : Singleton<SoapboxProgress>
{
    [SerializeField] private ItemDefinition plankItem;
    [SerializeField] private ItemDefinition wheelItem;
    [SerializeField] private ItemDefinition axleItem;
    [SerializeField] private ItemDefinition bearingsItem;

    public float BestDistance { get; private set; }
    public int RunCount { get; private set; }

    public SoapboxStats GetStats(PlayerInventory inventory)
    {
        var planks = Count(inventory, plankItem);
        var wheels = Count(inventory, wheelItem);
        var axles = Count(inventory, axleItem);
        var bearings = Count(inventory, bearingsItem);

        return new SoapboxStats
        {
            acceleration = 9f + wheels * 1.5f + bearings * 2f,
            topSpeed = 8f + wheels * 1.3f + bearings * 2.2f,
            stability = 0.8f + axles * 0.25f,
            weight = Mathf.Max(0.7f, 1.15f - planks * 0.05f),
            partScore = planks + wheels + axles + bearings,
        };
    }

    public void RegisterRun(float distance)
    {
        RunCount++;
        BestDistance = Mathf.Max(BestDistance, distance);
    }

    public string GetBuildSummary(PlayerInventory inventory)
    {
        var stats = GetStats(inventory);
        if (stats.partScore <= 0)
        {
            return "Du har kun en ide endnu. Find trae, hjul, aksel og lejer rundt paa gaarden.";
        }

        return "Bil klar: fart " + Mathf.RoundToInt(stats.topSpeed) +
               ", acceleration " + Mathf.RoundToInt(stats.acceleration) +
               ", stabilitet " + Mathf.RoundToInt(stats.stability * 10f) + ".";
    }

    private static int Count(PlayerInventory inventory, ItemDefinition item)
    {
        return inventory != null && item != null ? inventory.CountItem(item) : 0;
    }
}

public struct SoapboxStats
{
    public float acceleration;
    public float topSpeed;
    public float stability;
    public float weight;
    public int partScore;
}
