using UnityEngine;

public sealed class PrototypeHud : MonoBehaviour
{
    private GUIStyle labelStyle;
    private HealthSystem playerHealth;
    private PlayerInventory inventory;
    private PlayerQuestLog questLog;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        playerHealth = player.GetComponent<HealthSystem>();
        inventory = player.GetComponent<PlayerInventory>();
        questLog = player.GetComponent<PlayerQuestLog>();
    }

    private void OnGUI()
    {
        labelStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            normal = { textColor = new Color(0.86f, 0.95f, 0.88f) }
        };

        GUI.Label(new Rect(18, 14, 720, 28), "Nyt Spil - prototype", labelStyle);
        GUI.Label(new Rect(18, 42, 720, 28), $"HP: {ReadHealth()}   Coins: {ReadCoins()}", labelStyle);
        GUI.Label(new Rect(18, 70, 720, 28), "Move: WASD/arrows   Attack: Space/Mouse   Talk: E   Save: F5   Load: F9", labelStyle);
        GUI.Label(new Rect(18, 98, 920, 28), "Quest: " + ReadQuestText(), labelStyle);
    }

    private string ReadHealth()
    {
        return playerHealth != null ? $"{playerHealth.CurrentHealth}/{playerHealth.MaxHealth}" : "-";
    }

    private int ReadCoins()
    {
        return inventory != null ? inventory.Coins : 0;
    }

    private string ReadQuestText()
    {
        if (questLog == null || questLog.States.Count == 0)
        {
            return "Talk to Mira near the lantern.";
        }

        foreach (var state in questLog.States.Values)
        {
            return state == QuestState.Completed ? "Lantern Errand complete" : "Defeat a shade and bring Mira its Echo Shard";
        }

        return "Talk to Mira near the lantern.";
    }
}
