using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building inventory overlay. Tab toggles open/closed.
/// Sits on the player alongside PlayerInputReader.
/// </summary>
[RequireComponent(typeof(PlayerInputReader))]
public sealed class InventoryPanel : MonoBehaviour
{
    private PlayerInputReader inputReader;
    private PlayerInventory   inventory;

    private GameObject      panelRoot;
    private Transform       itemListRoot;
    private TextMeshProUGUI coinsLabel;
    private TMP_FontAsset   tmpFont;
    private bool            isOpen;
    private bool            pausedGame;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        inventory   = GetComponent<PlayerInventory>();
        BuildPanel();
    }

    private void OnEnable()
    {
        inputReader.InventoryPressed += Toggle;
        if (inventory != null) inventory.Changed += RefreshList;
    }

    private void OnDisable()
    {
        inputReader.InventoryPressed -= Toggle;
        if (inventory != null) inventory.Changed -= RefreshList;
    }

    private void Toggle()
    {
        isOpen = !isOpen;
        panelRoot.SetActive(isOpen);
        if (isOpen)
        {
            RefreshList();
            if (GameManager.Instance != null && !GameManager.Instance.IsPaused)
            {
                pausedGame = true;
                GameManager.Instance.SetPaused(true);
            }
        }
        else if (pausedGame)
        {
            pausedGame = false;
            GameManager.Instance?.SetPaused(false);
        }
    }

    // ── Data refresh ───────────────────────────────────────────────────────────

    private void RefreshList()
    {
        if (!isOpen || inventory == null) return;

        // Clear old rows
        for (var i = itemListRoot.childCount - 1; i >= 0; i--)
            Destroy(itemListRoot.GetChild(i).gameObject);

        if (coinsLabel != null)
            coinsLabel.text = "● Moenter:  " + inventory.Coins;

        if (inventory.Items.Count == 0)
        {
            MakeRow("(ingen genstande endnu)");
            return;
        }

        foreach (var stack in inventory.Items)
            MakeRow(stack.Item.DisplayName + "  x" + stack.Quantity);
    }

    private void MakeRow(string text)
    {
        var go = new GameObject("Row");
        go.transform.SetParent(itemListRoot, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(320, 28);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 18;
        tmp.color     = new Color(0.88f, 0.90f, 0.86f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        if (tmpFont != null) tmp.font = tmpFont;
    }

    // ── Canvas builder ─────────────────────────────────────────────────────────

    private void BuildPanel()
    {
        tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var canvasGo = new GameObject("Inventory Canvas");

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var rootRt = canvasGo.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        // Card — right side, vertically centred
        var card = MakeAnchored(canvasGo.transform, "Card",
            new Vector2(1, 0.5f), new Vector2(1, 0.5f),
            new Vector2(-16, 0), new Vector2(380, 460));
        card.pivot = new Vector2(1, 0.5f);

        var cardImg = card.gameObject.AddComponent<Image>();
        cardImg.color = new Color(0.10f, 0.11f, 0.13f, 0.96f);

        // Title
        MakeTMP(card, "Title", "TASKE", new Vector2(0, -20), new Vector2(360, 40), 28,
            new Color(0.95f, 0.88f, 0.62f), TextAlignmentOptions.Center, tmpFont);

        // Coins line
        var coinsRt = MakeTMP(card, "Coins", "", new Vector2(0, -70), new Vector2(360, 28), 20,
            new Color(0.97f, 0.87f, 0.40f), TextAlignmentOptions.Left, tmpFont);
        coinsLabel = coinsRt;

        // Divider
        var sep = MakeAnchored(card, "Sep",
            new Vector2(0.05f, 1f), new Vector2(0.05f, 1f),
            new Vector2(0, -106), new Vector2(342, 2));
        sep.gameObject.AddComponent<Image>().color = new Color(0.4f, 0.38f, 0.32f, 0.7f);

        // Scrollable item list area
        var listContainer = MakeAnchored(card, "ItemList",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0, -116), new Vector2(354, 300));
        listContainer.pivot = new Vector2(0.5f, 1f);

        var layout = listContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding            = new RectOffset(16, 8, 4, 4);
        layout.spacing            = 6f;
        layout.childAlignment     = TextAnchor.UpperLeft;
        layout.childControlHeight = true;
        layout.childControlWidth  = true;
        layout.childForceExpandWidth = true;

        itemListRoot = listContainer;

        // Hint at bottom
        MakeTMP(card, "Hint", "TAB — Luk taske", new Vector2(0, -428), new Vector2(360, 24), 14,
            new Color(0.50f, 0.50f, 0.50f), TextAlignmentOptions.Center, tmpFont);

        panelRoot = canvasGo;
        panelRoot.SetActive(false);
    }

    private static RectTransform MakeAnchored(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        return rt;
    }

    private static TextMeshProUGUI MakeTMP(RectTransform parent, string name, string text,
        Vector2 anchoredPos, Vector2 size, int fontSize, Color color, TextAlignmentOptions align,
        TMP_FontAsset font = null)
    {
        var rt  = MakeAnchored(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPos, size);
        rt.pivot = new Vector2(0.5f, 1f);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        if (font != null) tmp.font = font;
        return tmp;
    }
}
