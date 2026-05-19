using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building pause overlay. Listens to PlayerInputReader.PausePressed and GameManager.PauseChanged.
/// Builds its own Canvas so no prefab is needed.
/// </summary>
[RequireComponent(typeof(PlayerInputReader))]
public sealed class PauseMenu : MonoBehaviour
{
    private PlayerInputReader inputReader;
    private GameObject        overlayRoot;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        BuildOverlay();
    }

    private void OnEnable()
    {
        inputReader.PausePressed += OnPausePressed;
        if (GameManager.Instance != null)
            GameManager.Instance.PauseChanged += OnPauseChanged;
    }

    private void OnDisable()
    {
        inputReader.PausePressed -= OnPausePressed;
        if (GameManager.Instance != null)
            GameManager.Instance.PauseChanged -= OnPauseChanged;
    }

    private void OnPausePressed()  => GameManager.Instance?.TogglePause();
    private void OnPauseChanged(bool paused) => overlayRoot?.SetActive(paused);

    // ── Canvas builder ─────────────────────────────────────────────────────────

    private void BuildOverlay()
    {
        var canvasGo = new GameObject("Pause Canvas");

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var rootRt = canvasGo.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;

        // Dark fullscreen backdrop
        var backdrop = MakeStretchChild(canvasGo.transform, "Backdrop");
        var backdropImg = backdrop.gameObject.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.72f);

        // Centre card
        var card = MakeAnchoredChild(canvasGo.transform, "Card",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(380, 340));
        var cardImg = card.gameObject.AddComponent<Image>();
        cardImg.color = new Color(0.10f, 0.11f, 0.13f, 0.96f);

        // Title
        MakeTMP(card, "Title", "PAUSE", new Vector2(0, -24), new Vector2(360, 48), 36,
            new Color(0.95f, 0.88f, 0.62f), TextAlignmentOptions.Center);

        // Separator line
        var sep = MakeAnchoredChild(card, "Sep",
            new Vector2(0.1f, 1f), new Vector2(0.1f, 1f),
            new Vector2(0, -80), new Vector2(304, 2));
        sep.gameObject.AddComponent<Image>().color = new Color(0.4f, 0.38f, 0.32f, 0.8f);

        // Buttons
        MakeButton(card, "Resume",        "Forsaet spil",     new Vector2(0, -128), () => GameManager.Instance?.SetPaused(false));
        MakeButton(card, "Save",          "Gem (F5)",         new Vector2(0, -184), OnSave);
        MakeButton(card, "MainMenu",      "Til startmenu",    new Vector2(0, -240), () => GameManager.Instance?.GoToMainMenu());
        MakeButton(card, "Quit",          "Afslut spil",      new Vector2(0, -296), () => GameManager.Instance?.QuitGame());

        // Hint at bottom
        MakeTMP(card, "Hint", "ESC — Forsaet", new Vector2(0, -328), new Vector2(360, 24), 15,
            new Color(0.55f, 0.55f, 0.55f), TextAlignmentOptions.Center);

        overlayRoot = canvasGo;
        overlayRoot.SetActive(false);
    }

    private static void OnSave()
    {
        var sm = SaveManager.Instance;
        if (sm == null) return;
        sm.SaveToSlot(sm.CurrentSlot >= 0 ? sm.CurrentSlot : 0);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static RectTransform MakeStretchChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static RectTransform MakeAnchoredChild(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
        return rt;
    }

    private static void MakeTMP(RectTransform parent, string name, string text,
        Vector2 anchoredPos, Vector2 size, int fontSize, Color color, TextAlignmentOptions align)
    {
        var rt  = MakeAnchoredChild(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPos, size);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static void MakeButton(RectTransform parent, string name, string label,
        Vector2 anchoredPos, UnityEngine.Events.UnityAction action)
    {
        var rt  = MakeAnchoredChild(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPos, new Vector2(300, 44));
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.18f, 0.20f, 0.22f, 1f);

        var btn = rt.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(0.18f, 0.20f, 0.22f);
        colors.highlightedColor = new Color(0.28f, 0.30f, 0.34f);
        colors.pressedColor     = new Color(0.12f, 0.14f, 0.16f);
        btn.colors = colors;
        btn.onClick.AddListener(action);

        var labelRt = MakeStretchChild(rt);
        var tmp     = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 22;
        tmp.color     = new Color(0.90f, 0.92f, 0.88f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static RectTransform MakeStretchChild(RectTransform parent)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8, 4);
        rt.offsetMax = new Vector2(-8, -4);
        return rt;
    }
}
