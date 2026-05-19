using UnityEngine;

public sealed class SoapboxRunStarter : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => "Start saebekasse-run";

    public void Interact(PlayerInteractor interactor)
    {
        if (SoapboxRunController.Instance == null)
        {
            interactor.ShowMessage("Rampe", "Run-systemet mangler.");
            return;
        }

        SoapboxRunController.Instance.StartRun(interactor);
    }
}
