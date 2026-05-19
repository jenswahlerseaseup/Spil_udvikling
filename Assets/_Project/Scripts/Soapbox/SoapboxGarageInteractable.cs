using UnityEngine;

public sealed class SoapboxGarageInteractable : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => "Byg saebekassebil";

    public void Interact(PlayerInteractor interactor)
    {
        var progress = SoapboxProgress.Instance;
        if (progress == null)
        {
            interactor.ShowMessage("Garage", "Garage-systemet mangler.");
            return;
        }

        interactor.ShowMessage("Garage", progress.GetBuildSummary(interactor.Inventory));
    }
}
