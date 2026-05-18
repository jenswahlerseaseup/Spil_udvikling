using UnityEngine;

public sealed class DialogueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string speakerName = "Mira";
    [SerializeField] private string prompt = "Talk";
    [SerializeField, TextArea] private string[] lines =
    {
        "The old lanterns woke again last night.",
        "If you head north, keep your blade close and your eyes open."
    };

    private int lineIndex;

    public string InteractionPrompt => prompt;

    public void Interact(PlayerInteractor interactor)
    {
        if (lines == null || lines.Length == 0)
        {
            interactor.ShowMessage(speakerName, "...");
            return;
        }

        interactor.ShowMessage(speakerName, lines[lineIndex]);
        lineIndex = (lineIndex + 1) % lines.Length;
    }
}
