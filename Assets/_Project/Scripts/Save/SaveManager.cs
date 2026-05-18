using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SaveManager : Singleton<SaveManager>
{
    public const int MaxSlots = 3;

    [SerializeField] private ItemRegistry itemRegistry;
    [SerializeField, Range(0, MaxSlots - 1)] private int defaultGameplaySlot;

    private PlayerInputReader subscribedInput;
    private SaveData pendingLoad;

    public int CurrentSlot { get; private set; } = -1;

    public static string SlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_slot_{Mathf.Clamp(slot, 0, MaxSlots - 1)}.json");

    public bool SlotExists(int slot) => File.Exists(SlotPath(slot));

    public SaveData LoadSlotData(int slot)
    {
        if (!SlotExists(slot))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(slot)));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not read save slot {slot}: {exception.Message}");
            return null;
        }
    }

    public void StartNewGame(int slot, string sceneName)
    {
        CurrentSlot = Mathf.Clamp(slot, 0, MaxSlots - 1);
        pendingLoad = null;
        SceneManager.LoadScene(sceneName);
    }

    public void SaveToSlot(int slot)
    {
        slot = Mathf.Clamp(slot, 0, MaxSlots - 1);
        CurrentSlot = slot;

        var player = FindPlayer();
        var data = new SaveData
        {
            slotId = slot,
            currentScene = SceneManager.GetActiveScene().name,
            playerX = player != null ? player.position.x : 0f,
            playerY = player != null ? player.position.y : 0f,
            coins = player?.GetComponent<PlayerInventory>()?.Coins ?? 0,
            mischiefPoints = MischiefSystem.Instance?.Points ?? 0,
            playTimeSeconds = Time.realtimeSinceStartup,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            items = CaptureItems(player?.GetComponent<PlayerInventory>()),
            quests = QuestManager.Instance?.GetSaveEntries() ?? new List<QuestSaveEntry>(),
        };

        File.WriteAllText(SlotPath(slot), JsonUtility.ToJson(data, true));
        Debug.Log($"Saved slot {slot}: {SlotPath(slot)}");
    }

    public bool LoadFromSlot(int slot)
    {
        var data = LoadSlotData(slot);
        if (data == null)
        {
            return false;
        }

        CurrentSlot = Mathf.Clamp(slot, 0, MaxSlots - 1);
        var targetScene = string.IsNullOrWhiteSpace(data.currentScene) ? SceneNames.Farm : data.currentScene;
        if (SceneManager.GetActiveScene().name != targetScene)
        {
            pendingLoad = data;
            SceneManager.LoadScene(targetScene);
            return true;
        }

        ApplyLoadedData(data);
        return true;
    }

    public void DeleteSlot(int slot)
    {
        var path = SlotPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        if (CurrentSlot == slot)
        {
            CurrentSlot = -1;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }

    protected override void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeInput();
        }

        base.OnDestroy();
    }

    private void Start()
    {
        RefreshPlayerInputSubscription();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshPlayerInputSubscription();

        if (pendingLoad != null)
        {
            var data = pendingLoad;
            pendingLoad = null;
            ApplyLoadedData(data);
        }
    }

    private void RefreshPlayerInputSubscription()
    {
        UnsubscribeInput();

        var player = FindPlayer();
        subscribedInput = player != null ? player.GetComponent<PlayerInputReader>() : null;
        if (subscribedInput == null)
        {
            return;
        }

        subscribedInput.SavePressed += SaveCurrentSlot;
        subscribedInput.LoadPressed += LoadCurrentSlot;
    }

    private void UnsubscribeInput()
    {
        if (subscribedInput == null)
        {
            return;
        }

        subscribedInput.SavePressed -= SaveCurrentSlot;
        subscribedInput.LoadPressed -= LoadCurrentSlot;
        subscribedInput = null;
    }

    private void SaveCurrentSlot()
    {
        SaveToSlot(CurrentSlot >= 0 ? CurrentSlot : defaultGameplaySlot);
    }

    private void LoadCurrentSlot()
    {
        LoadFromSlot(CurrentSlot >= 0 ? CurrentSlot : defaultGameplaySlot);
    }

    private void ApplyLoadedData(SaveData data)
    {
        var player = FindPlayer();
        if (player != null)
        {
            player.position = new Vector3(data.playerX, data.playerY, player.position.z);
            player.GetComponent<PlayerInventory>()?.LoadState(data.coins, RestoreItems(data.items));
        }

        MischiefSystem.Instance?.LoadState(data.mischiefPoints);
        QuestManager.Instance?.LoadSaveEntries(data.quests);
    }

    private static Transform FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        return go != null ? go.transform : null;
    }

    private static List<ItemSaveEntry> CaptureItems(PlayerInventory inventory)
    {
        var list = new List<ItemSaveEntry>();
        if (inventory == null)
        {
            return list;
        }

        foreach (var stack in inventory.Items)
        {
            if (stack.Item != null)
            {
                list.Add(new ItemSaveEntry { itemId = stack.Item.Id, quantity = stack.Quantity });
            }
        }

        return list;
    }

    private List<InventoryItemStack> RestoreItems(List<ItemSaveEntry> entries)
    {
        var stacks = new List<InventoryItemStack>();
        if (entries == null || itemRegistry == null)
        {
            return stacks;
        }

        foreach (var entry in entries)
        {
            var item = itemRegistry.Find(entry.itemId);
            if (item != null)
            {
                stacks.Add(new InventoryItemStack(item, entry.quantity));
            }
        }

        return stacks;
    }
}
