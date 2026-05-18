using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class SaveGameController : MonoBehaviour
{
    private const string SaveFileName = "savegame.json";

    [SerializeField] private Transform player;
    [SerializeField] private ItemRegistry itemRegistry;

    private PlayerInputReader inputReader;
    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    private void Start()
    {
        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            player = playerObject != null ? playerObject.transform : null;
        }

        if (player != null)
        {
            inputReader = player.GetComponent<PlayerInputReader>();
            if (inputReader != null)
            {
                inputReader.SavePressed += Save;
                inputReader.LoadPressed += Load;
            }
        }

        Load();
    }

    private void OnDestroy()
    {
        if (inputReader != null)
        {
            inputReader.SavePressed -= Save;
            inputReader.LoadPressed -= Load;
        }
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    public void Save()
    {
        if (player == null)
        {
            return;
        }

        var data = new SaveData
        {
            playerX = player.position.x,
            playerY = player.position.y,
            coins = player.GetComponent<PlayerInventory>()?.Coins ?? 0,
            health = player.GetComponent<HealthSystem>()?.CurrentHealth ?? 1,
            quests = CaptureQuestStates(player.GetComponent<PlayerQuestLog>()),
            items = CaptureItemStacks(player.GetComponent<PlayerInventory>())
        };

        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        Debug.Log("Saved game to " + SavePath);
    }

    public void Load()
    {
        if (player == null || !File.Exists(SavePath))
        {
            return;
        }

        var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        player.position = new Vector3(data.playerX, data.playerY, player.position.z);

        if (player.TryGetComponent<PlayerInventory>(out var inventory))
        {
            inventory.LoadState(data.coins, RestoreItemStacks(data.items));
        }

        if (player.TryGetComponent<HealthSystem>(out var health))
        {
            health.LoadState(data.health);
        }

        if (player.TryGetComponent<PlayerQuestLog>(out var questLog))
        {
            questLog.LoadState(RestoreQuestStates(data.quests));
        }
    }

    private static List<ItemSaveEntry> CaptureItemStacks(PlayerInventory inventory)
    {
        var entries = new List<ItemSaveEntry>();
        if (inventory == null)
        {
            return entries;
        }

        foreach (var stack in inventory.Items)
        {
            if (stack.Item != null)
            {
                entries.Add(new ItemSaveEntry { id = stack.Item.Id, quantity = stack.Quantity });
            }
        }

        return entries;
    }

    private List<InventoryItemStack> RestoreItemStacks(List<ItemSaveEntry> entries)
    {
        var stacks = new List<InventoryItemStack>();
        if (entries == null || itemRegistry == null)
        {
            return stacks;
        }

        foreach (var entry in entries)
        {
            var item = itemRegistry.Find(entry.id);
            if (item != null)
            {
                stacks.Add(new InventoryItemStack(item, entry.quantity));
            }
        }

        return stacks;
    }

    private static List<QuestSaveEntry> CaptureQuestStates(PlayerQuestLog questLog)
    {
        var entries = new List<QuestSaveEntry>();
        if (questLog == null)
        {
            return entries;
        }

        foreach (var pair in questLog.States)
        {
            entries.Add(new QuestSaveEntry { id = pair.Key, state = pair.Value });
        }

        return entries;
    }

    private static Dictionary<string, QuestState> RestoreQuestStates(List<QuestSaveEntry> entries)
    {
        var states = new Dictionary<string, QuestState>();
        if (entries == null)
        {
            return states;
        }

        foreach (var entry in entries)
        {
            states[entry.id] = entry.state;
        }

        return states;
    }

    [Serializable]
    private sealed class SaveData
    {
        public float playerX;
        public float playerY;
        public int coins;
        public int health;
        public List<QuestSaveEntry> quests = new();
        public List<ItemSaveEntry> items = new();
    }

    [Serializable]
    private sealed class QuestSaveEntry
    {
        public string id;
        public QuestState state;
    }

    [Serializable]
    private sealed class ItemSaveEntry
    {
        public string id;
        public int quantity;
    }
}
