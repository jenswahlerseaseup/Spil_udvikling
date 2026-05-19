using UnityEngine;

/// <summary>
/// Readable sign post. Shows a fixed message when interacted with.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class SignInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string signText = "Et skilt";

    public string InteractionPrompt => "Laes";

    public void Interact(PlayerInteractor interactor)
    {
        interactor.ShowMessage(string.Empty, signText);
    }
}
