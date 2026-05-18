using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One panel per save slot. The MainMenuController calls Refresh() to populate it.
public sealed class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private int slotIndex;

    [Header("State panels")]
    [SerializeField] private GameObject emptyPanel;
    [SerializeField] private GameObject occupiedPanel;

    [Header("Occupied labels")]
    [SerializeField] private TMP_Text sceneText;
    [SerializeField] private TMP_Text timestampText;

    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;

    private MainMenuController menu;

    private void Awake() => menu = GetComponentInParent<MainMenuController>();

    public void Refresh(int index, SaveData data)
    {
        slotIndex = index;
        var hasData = data != null;

        emptyPanel?.SetActive(!hasData);
        occupiedPanel?.SetActive(hasData);
        newGameButton?.gameObject.SetActive(!hasData);
        loadButton?.gameObject.SetActive(hasData);
        deleteButton?.gameObject.SetActive(hasData);

        if (!hasData) return;
        if (sceneText     != null) sceneText.text     = data.currentScene;
        if (timestampText != null) timestampText.text = data.timestamp;
    }

    public void OnClickNewGame() => menu?.OnNewGame(slotIndex);
    public void OnClickLoad()    => menu?.OnLoadSlot(slotIndex);
    public void OnClickDelete()  => menu?.RequestDeleteSlot(slotIndex);
}
