using UnityEngine;

/// <summary>
/// Runtime state for the soapbox car. The garage installs parts into clear
/// upgrade levels, keeping progression readable while the prototype is small.
/// </summary>
public sealed class SoapboxProgress : Singleton<SoapboxProgress>
{
    private const int MaxUpgradeLevel = 3;

    [SerializeField] private ItemDefinition plankItem;
    [SerializeField] private ItemDefinition wheelItem;
    [SerializeField] private ItemDefinition axleItem;
    [SerializeField] private ItemDefinition bearingsItem;

    public float BestDistance { get; private set; }
    public int RunCount { get; private set; }
    public int FrameLevel { get; private set; }
    public int WheelLevel { get; private set; }
    public int BearingLevel { get; private set; }
    public int SteeringLevel { get; private set; }

    public SoapboxStats GetStats(PlayerInventory inventory)
    {
        var spareParts = Count(inventory, plankItem) + Count(inventory, wheelItem) +
                         Count(inventory, axleItem) + Count(inventory, bearingsItem);

        return new SoapboxStats
        {
            acceleration = 8.5f + WheelLevel * 1.4f + BearingLevel * 2.2f,
            topSpeed = 7.5f + WheelLevel * 1.6f + BearingLevel * 2.4f,
            stability = 0.8f + SteeringLevel * 0.35f + FrameLevel * 0.08f,
            weight = Mathf.Max(0.72f, 1.18f - FrameLevel * 0.08f),
            upgradeLevel = TotalUpgradeLevel,
            partScore = FrameLevel + WheelLevel + BearingLevel + SteeringLevel + spareParts,
        };
    }

    public void RegisterRun(float distance)
    {
        RunCount++;
        BestDistance = Mathf.Max(BestDistance, distance);
    }

    public void LoadState(float bestDistance, int runCount, int frameLevel, int wheelLevel, int bearingLevel, int steeringLevel)
    {
        BestDistance = Mathf.Max(0f, bestDistance);
        RunCount = Mathf.Max(0, runCount);
        FrameLevel = ClampLevel(frameLevel);
        WheelLevel = ClampLevel(wheelLevel);
        BearingLevel = ClampLevel(bearingLevel);
        SteeringLevel = ClampLevel(steeringLevel);
    }

    public bool TryInstallNextUpgrade(PlayerInventory inventory, out string message)
    {
        if (inventory == null)
        {
            message = "Inventory mangler, saa garagen kan ikke installere dele.";
            return false;
        }

        if (TryUpgradeFrame(inventory, out message)) return true;
        if (TryUpgradeWheels(inventory, out message)) return true;
        if (TryUpgradeBearings(inventory, out message)) return true;
        if (TryUpgradeSteering(inventory, out message)) return true;

        message = GetNextUpgradeHint(inventory);
        return false;
    }

    public string GetBuildSummary(PlayerInventory inventory)
    {
        var stats = GetStats(inventory);
        if (stats.partScore <= 0)
        {
            return "Du har kun en ide endnu. Find trae, hjul, aksel og lejer rundt paa gaarden.";
        }

        return "Bil L" + TotalUpgradeLevel + ": fart " + Mathf.RoundToInt(stats.topSpeed) +
               ", acceleration " + Mathf.RoundToInt(stats.acceleration) +
               ", stabilitet " + Mathf.RoundToInt(stats.stability * 10f) +
               ". Rekord: " + Mathf.RoundToInt(BestDistance) + " m. " + GetNextUpgradeHint(inventory);
    }

    private int TotalUpgradeLevel => FrameLevel + WheelLevel + BearingLevel + SteeringLevel;

    private bool TryUpgradeFrame(PlayerInventory inventory, out string message)
    {
        if (FrameLevel >= MaxUpgradeLevel || !inventory.TryRemoveItem(plankItem, 1))
        {
            message = string.Empty;
            return false;
        }

        FrameLevel++;
        message = "Lettere ramme installeret! Ramme L" + FrameLevel + ".";
        return true;
    }

    private bool TryUpgradeWheels(PlayerInventory inventory, out string message)
    {
        if (WheelLevel >= MaxUpgradeLevel || !inventory.TryRemoveItem(wheelItem, 1))
        {
            message = string.Empty;
            return false;
        }

        WheelLevel++;
        message = "Bedre hjul installeret! Hjul L" + WheelLevel + ".";
        return true;
    }

    private bool TryUpgradeBearings(PlayerInventory inventory, out string message)
    {
        if (BearingLevel >= MaxUpgradeLevel || !inventory.TryRemoveItem(bearingsItem, 1))
        {
            message = string.Empty;
            return false;
        }

        BearingLevel++;
        message = "Hurtigere lejer installeret! Lejer L" + BearingLevel + ".";
        return true;
    }

    private bool TryUpgradeSteering(PlayerInventory inventory, out string message)
    {
        if (SteeringLevel >= MaxUpgradeLevel || !inventory.TryRemoveItem(axleItem, 1))
        {
            message = string.Empty;
            return false;
        }

        SteeringLevel++;
        message = "Mere stabil styring installeret! Styring L" + SteeringLevel + ".";
        return true;
    }

    private string GetNextUpgradeHint(PlayerInventory inventory)
    {
        if (FrameLevel < MaxUpgradeLevel) return RequirementText("Naeste: ramme", plankItem, inventory);
        if (WheelLevel < MaxUpgradeLevel) return RequirementText("Naeste: hjul", wheelItem, inventory);
        if (BearingLevel < MaxUpgradeLevel) return RequirementText("Naeste: lejer", bearingsItem, inventory);
        if (SteeringLevel < MaxUpgradeLevel) return RequirementText("Naeste: styring", axleItem, inventory);
        return "Alle nuvaerende upgrades er installeret.";
    }

    private static string RequirementText(string prefix, ItemDefinition item, PlayerInventory inventory)
    {
        var owned = Count(inventory, item);
        var name = item != null ? item.DisplayName : "del";
        return prefix + " kraever 1 " + name + " (" + owned + "/1).";
    }

    private static int ClampLevel(int level)
    {
        return Mathf.Clamp(level, 0, MaxUpgradeLevel);
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
    public int upgradeLevel;
    public int partScore;
}
