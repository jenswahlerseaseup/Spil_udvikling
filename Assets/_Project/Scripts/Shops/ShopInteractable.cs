using UnityEngine;

public sealed class ShopInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string speakerName = "Pip";
    [SerializeField] private ItemDefinition itemForSale;
    [SerializeField, Min(0)] private int price = 3;

    public string InteractionPrompt => "Buy";

    public void Interact(PlayerInteractor interactor)
    {
        var inventory = interactor.GetComponent<PlayerInventory>();
        if (inventory == null || itemForSale == null)
        {
            interactor.ShowMessage(speakerName, "Shop is closed.");
            return;
        }

        if (!inventory.TrySpendCoins(price))
        {
            interactor.ShowMessage(speakerName, $"Need {price} coins. Come back after some exploring.");
            return;
        }

        inventory.AddItem(itemForSale, 1);
        interactor.ShowMessage(speakerName, $"Sold! One {itemForSale.DisplayName} for you.");
    }
}
