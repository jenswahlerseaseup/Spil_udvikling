using UnityEngine;

public sealed class PrototypeHud : MonoBehaviour
{
    private const string ChickenQuestId = "collect_chickens";
    private const int ChickenQuestTarget = 3;

    private GUIStyle labelStyle;
    private HealthSystem playerHealth;
    private PlayerInventory inventory;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        playerHealth = player.GetComponent<HealthSystem>();
        inventory = player.GetComponent<PlayerInventory>();
    }

    private void OnGUI()
    {
        labelStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            normal = { textColor = new Color(0.86f, 0.95f, 0.88f) }
        };

        GUI.Label(new Rect(18, 14, 720, 28), "Emil paa gaarden - prototype", labelStyle);
        GUI.Label(new Rect(18, 42, 720, 28), $"HP: {ReadHealth()}   Moenter: {ReadCoins()}   Ballade: {ReadMischief()}", labelStyle);
        GUI.Label(new Rect(18, 70, 920, 28), "Bevaeg: WASD   Hop: Space   Tal: E   Loeb: Shift   Gem: F5   Indlaes: F9", labelStyle);
        GUI.Label(new Rect(18, 98, 920, 28), "Quest: " + ReadQuestText(), labelStyle);
    }

    private string ReadHealth() =>
        playerHealth != null ? $"{playerHealth.CurrentHealth}/{playerHealth.MaxHealth}" : "-";

    private int ReadCoins() =>
        inventory != null ? inventory.Coins : 0;

    private string ReadMischief() =>
        MischiefSystem.Instance != null ? MischiefSystem.Instance.Points.ToString() : "0";

    private static string ReadQuestText()
    {
        var questManager = QuestManager.Instance;
        if (questManager == null)
        {
            return "QuestManager mangler i scenen.";
        }

        var status = questManager.GetStatus(ChickenQuestId);
        var data = questManager.GetData(ChickenQuestId);

        return status switch
        {
            QuestState.NotStarted => "Tal med gaardejeren.",
            QuestState.Active => BuildActiveText(data),
            QuestState.Completed => "Fang hoensene - afleveret!",
            _ => string.Empty,
        };
    }

    private static string BuildActiveText(QuestRuntimeData data)
    {
        if (data == null)
        {
            return "Quest aktiv...";
        }

        return QuestManager.Instance.IsReadyToComplete(ChickenQuestId)
            ? "Vend tilbage til gaardejeren!"
            : $"Fang hoensene: {data.StepProgress}/{ChickenQuestTarget}";
    }
}
