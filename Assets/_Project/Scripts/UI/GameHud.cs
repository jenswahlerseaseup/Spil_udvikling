using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building Canvas HUD. Place on any scene GameObject.
/// Draws: stats (top-left), quest tracker (top-right), interaction prompt (bottom-centre),
/// toast notifications (centre screen).
/// </summary>
public sealed class GameHud : MonoBehaviour
{
    public static GameHud Instance { get; private set; }

    private TextMeshProUGUI hpLabel;
    private TextMeshProUGUI coinsLabel;
    private TextMeshProUGUI mischiefLabel;
    private TextMeshProUGUI questTitleLabel;
    private TextMeshProUGUI questProgressLabel;
    private TextMeshProUGUI interactionPromptLabel;
    private CanvasGroup     notificationGroup;
    private TextMeshProUGUI notificationLabel;

    private HealthSystem    playerHealth;
    private PlayerInventory playerInventory;
    private Coroutine       notificationRoutine;

    private string lastPrompt = string.Empty;

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildCanvas();
    }

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth    = player.GetComponent<HealthSystem>();
            playerInventory = player.GetComponent<PlayerInventory>();
        }

        if (QuestManager.Instance   != null) QuestManager.Instance.QuestUpdated    += RefreshQuestPanel;
        if (QuestManager.Instance   != null) QuestManager.Instance.QuestCompleted  += OnQuestCompleted;
        if (MischiefSystem.Instance != null) MischiefSystem.Instance.MischiefAdded += OnMischiefAdded;

        RefreshQuestPanel();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (QuestManager.Instance   != null) QuestManager.Instance.QuestUpdated    -= RefreshQuestPanel;
        if (QuestManager.Instance   != null) QuestManager.Instance.QuestCompleted  -= OnQuestCompleted;
        if (MischiefSystem.Instance != null) MischiefSystem.Instance.MischiefAdded -= OnMischiefAdded;
    }

    private void LateUpdate()
    {
        if (hpLabel != null && playerHealth != null)
        {
            var hearts = new string('♥', playerHealth.CurrentHealth)
                       + new string('♡', playerHealth.MaxHealth - playerHealth.CurrentHealth);
            hpLabel.text = "HP  " + hearts;
        }
        else if (hpLabel != null && SoapboxProgress.Instance != null)
        {
            hpLabel.text = "Bil  rekord " + Mathf.RoundToInt(SoapboxProgress.Instance.BestDistance) + " m";
        }

        if (coinsLabel != null && playerInventory != null)
            coinsLabel.text = "●  " + playerInventory.Coins + " moenter";

        if (mischiefLabel != null && MischiefSystem.Instance != null)
            mischiefLabel.text = "★  " + MischiefSystem.Instance.Points + " ballade";
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void ShowInteractionPrompt(string text)
    {
        if (interactionPromptLabel == null || text == lastPrompt) return;
        lastPrompt = text;
        var hasText = !string.IsNullOrEmpty(text);
        interactionPromptLabel.text = text;
        interactionPromptLabel.transform.parent.gameObject.SetActive(hasText);
    }

    public void ShowNotification(string text, float displayDuration = 1.8f)
    {
        if (notificationLabel == null) return;
        if (notificationRoutine != null) StopCoroutine(notificationRoutine);
        notificationRoutine = StartCoroutine(FadeNotification(text, displayDuration));
    }

    // ── Events ─────────────────────────────────────────────────────────────────

    private void RefreshQuestPanel()
    {
        if (questTitleLabel == null) return;
        var qm = QuestManager.Instance;
        if (qm == null) return;

        var (title, progress) = GetBestActiveQuest(qm);
        questTitleLabel.text    = title;
        questProgressLabel.text = progress;
    }

    private static (string title, string progress) GetBestActiveQuest(QuestManager qm)
    {
        var found = TryQuestDisplay(qm, "collect_chickens", "Fang hoensene",
            d => "Find hoens rundt paa gaarden:  " + d.StepProgress + " / 3");
        if (found.HasValue) return found.Value;

        found = TryQuestDisplay(qm, "apple_harvest", "Aebletrae-hoest",
            d => "Ryst markerede trae i frugtskoven:  " + d.StepProgress + " / 3");
        if (found.HasValue) return found.Value;

        return ("Emil paa gaarden", "Foelg markoeren og tal med gaardejeren");
    }

    private static (string, string)? TryQuestDisplay(QuestManager qm, string id,
        string displayTitle, Func<QuestRuntimeData, string> progressFmt)
    {
        if (qm.GetStatus(id) != QuestState.Active) return null;
        var data     = qm.GetData(id);
        var progress = qm.IsReadyToComplete(id)
            ? "Vend tilbage til gaardejeren!"
            : (data != null ? progressFmt(data) : "...");
        return (displayTitle, progress);
    }

    private void OnMischiefAdded(int total, string reason)
    {
        ShowNotification("Ballade! +1   (ialt: " + total + ")");
    }

    private void OnQuestCompleted(QuestDefinition quest)
    {
        if (quest == null) return;
        var sb = new System.Text.StringBuilder();
        sb.Append("Quest fuldfoert:  ").Append(quest.Title).Append("!");
        if (quest.RewardCoins > 0)
            sb.Append("  +").Append(quest.RewardCoins).Append(" moenter");
        if (quest.RewardItems != null)
            foreach (var item in quest.RewardItems)
                if (item != null) sb.Append("  +").Append(item.DisplayName);
        ShowNotification(sb.ToString(), 3.5f);
    }

    // ── Coroutine ──────────────────────────────────────────────────────────────

    private IEnumerator FadeNotification(string text, float displayDuration)
    {
        notificationLabel.text  = text;
        notificationGroup.alpha = 1f;
        notificationGroup.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(displayDuration);

        var elapsed = 0f;
        const float fadeDuration = 0.5f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            notificationGroup.alpha = 1f - elapsed / fadeDuration;
            yield return null;
        }

        notificationGroup.gameObject.SetActive(false);
        notificationRoutine = null;
    }

    // ── Canvas builder ─────────────────────────────────────────────────────────

    private void BuildCanvas()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var root = MakeCanvasRoot();

        // ── Stats — top-left ──────────────────────────────────────────────────
        var statsBg = MakePanel(root, "Stats",
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(12, -12), new Vector2(252, 86),
            new Color(0f, 0f, 0f, 0.48f));

        hpLabel       = MakeLabel(statsBg, "HP",       new Vector2(10, -8),  20, new Color(0.95f, 0.42f, 0.42f), font: font);
        coinsLabel    = MakeLabel(statsBg, "Coins",    new Vector2(10, -34), 20, new Color(0.97f, 0.87f, 0.40f), font: font);
        mischiefLabel = MakeLabel(statsBg, "Mischief", new Vector2(10, -60), 20, new Color(0.80f, 0.52f, 0.95f), font: font);

        // ── Quest — top-right ─────────────────────────────────────────────────
        var questBg = MakePanel(root, "Quest",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-12, -12), new Vector2(300, 64),
            new Color(0f, 0f, 0f, 0.48f));

        questTitleLabel    = MakeLabel(questBg, "QTitle",    new Vector2(10, -8),  18, new Color(0.96f, 0.88f, 0.60f), TextAlignmentOptions.Right, font);
        questProgressLabel = MakeLabel(questBg, "QProgress", new Vector2(10, -34), 16, new Color(0.70f, 0.88f, 0.68f), TextAlignmentOptions.Right, font);

        // Width fills parent minus side padding
        SetLabelWidth(questTitleLabel,    280);
        SetLabelWidth(questProgressLabel, 280);

        // ── Interaction prompt — bottom-centre ────────────────────────────────
        var promptBg = MakePanel(root, "Prompt",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 68), new Vector2(540, 40),
            new Color(0f, 0f, 0f, 0.52f));

        interactionPromptLabel = MakeLabel(promptBg, "PromptText", new Vector2(0, -8), 22,
            new Color(0.92f, 0.96f, 0.80f), TextAlignmentOptions.Center, font);
        SetLabelWidth(interactionPromptLabel, 520);
        promptBg.gameObject.SetActive(false);

        // ── Notification toast — centre ────────────────────────────────────────
        var notifBg = MakePanel(root, "Notification",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 110), new Vector2(640, 52),
            new Color(0f, 0f, 0f, 0.62f));

        notificationGroup = notifBg.gameObject.AddComponent<CanvasGroup>();
        notificationLabel = MakeLabel(notifBg, "NotifText", new Vector2(0, -10), 24,
            new Color(1f, 1f, 1f, 1f), TextAlignmentOptions.Center, font);
        SetLabelWidth(notificationLabel, 620);
        notifBg.gameObject.SetActive(false);
    }

    private static RectTransform MakeCanvasRoot()
    {
        var go = new GameObject("HUD Canvas");

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static RectTransform MakePanel(RectTransform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.pivot           = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta       = size;

        if (bgColor.a > 0.001f)
        {
            var img = go.AddComponent<Image>();
            img.color = bgColor;
        }

        return rt;
    }

    private static TextMeshProUGUI MakeLabel(RectTransform parent, string name, Vector2 topLeftOffset,
        int fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Left,
        TMP_FontAsset font = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin       = new Vector2(0, 1);
        rt.anchorMax       = new Vector2(0, 1);
        rt.pivot           = new Vector2(0, 1);
        rt.anchoredPosition = topLeftOffset;
        rt.sizeDelta       = new Vector2(parent.sizeDelta.x - Mathf.Abs(topLeftOffset.x) * 2f, fontSize + 6);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize         = fontSize;
        tmp.alignment        = alignment;
        tmp.color            = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode     = TextOverflowModes.Ellipsis;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private static void SetLabelWidth(TextMeshProUGUI label, float width)
    {
        var rt = label.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
    }
}
