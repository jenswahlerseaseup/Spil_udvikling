using TMPro;
using UnityEngine;

// Place this component on the dialogue Canvas panel in gameplay scenes.
// The panel should start disabled; this script opens and closes it.
public sealed class DialogueUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text continueHint;

    private void Awake()
    {
        Hide();
    }

    public void Show(string speaker, string text)
    {
        if (panel != null) panel.SetActive(true);
        if (speakerText != null) speakerText.text = speaker;
        if (bodyText != null) bodyText.text = text;
        if (continueHint != null) continueHint.text = "[E] Fortsaet";
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }
}
