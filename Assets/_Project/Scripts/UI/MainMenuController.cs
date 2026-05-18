using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach to a GameObject in the MainMenu scene.
// Wire up buttons via Inspector — all OnClick calls point to this controller.
public sealed class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject confirmDeletePanel;

    [Header("Confirm delete dialog")]
    [SerializeField] private TMP_Text confirmDeleteText;

    [Header("Save slots")]
    [SerializeField] private SaveSlotUI[] saveSlots;

    private int pendingDeleteSlot = -1;

    private void Start() => RefreshSlots();

    private void RefreshSlots()
    {
        for (var i = 0; i < saveSlots.Length && i < SaveManager.MaxSlots; i++)
            saveSlots[i].Refresh(i, SaveManager.Instance?.LoadSlotData(i));
    }

    // Called by the slot's New Game button.
    public void OnNewGame(int slot)
    {
        SaveManager.Instance?.StartNewGame(slot, SceneNames.Farm);
    }

    // Called by the slot's Continue/Load button.
    public void OnLoadSlot(int slot)
    {
        var data = SaveManager.Instance?.LoadSlotData(slot);
        if (data != null)
        {
            SaveManager.Instance?.LoadFromSlot(slot);
        }
    }

    // Called by the slot's Delete button — shows the confirmation dialog.
    public void RequestDeleteSlot(int slot)
    {
        pendingDeleteSlot = slot;
        var data = SaveManager.Instance?.LoadSlotData(slot);
        if (confirmDeleteText != null)
            confirmDeleteText.text =
                $"Er du sikker på, at du vil slette dette gemte spil?\n({data?.timestamp ?? "Ukendt dato"})";

        mainPanel?.SetActive(false);
        confirmDeletePanel?.SetActive(true);
    }

    // Called by the "Ja, slet" button in the confirmation dialog.
    public void ConfirmDelete()
    {
        if (pendingDeleteSlot >= 0)
        {
            SaveManager.Instance?.DeleteSlot(pendingDeleteSlot);
            pendingDeleteSlot = -1;
        }
        CancelDelete();
    }

    // Called by the "Fortryd" button in the confirmation dialog.
    public void CancelDelete()
    {
        confirmDeletePanel?.SetActive(false);
        mainPanel?.SetActive(true);
        RefreshSlots();
    }

    public void OnQuit() => GameManager.Instance?.QuitGame();
}
