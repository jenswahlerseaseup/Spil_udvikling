using UnityEngine;

public sealed class BucketInteractable : MonoBehaviour, IInteractable
{
    [SerializeField, Min(0)] private int mischiefAmount = 1;
    [SerializeField] private string mischiefReason = "Knocked over a bucket";
    [SerializeField] private float tippedAngle = 90f;

    private bool tipped;

    public string InteractionPrompt => tipped ? string.Empty : "Vaelt";

    public void Interact(PlayerInteractor interactor)
    {
        if (tipped)
        {
            return;
        }

        tipped = true;
        transform.rotation = Quaternion.Euler(0f, 0f, tippedAngle);
        MischiefSystem.Instance?.AddMischief(mischiefAmount, mischiefReason);
        interactor.ShowMessage(string.Empty, $"Plask! (+{mischiefAmount} ballade)");
    }
}
