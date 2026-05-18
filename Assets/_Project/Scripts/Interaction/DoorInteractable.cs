using UnityEngine;

public sealed class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Collider2D doorBlocker;

    private SpriteRenderer sr;
    private bool isOpen;

    public string InteractionPrompt => isOpen ? "Luk" : "Åbn";

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (doorBlocker == null) doorBlocker = GetComponent<Collider2D>();
        Refresh();
    }

    public void Interact(PlayerInteractor interactor)
    {
        isOpen = !isOpen;
        Refresh();
    }

    private void Refresh()
    {
        if (sr != null)          sr.sprite          = isOpen ? openSprite : closedSprite;
        if (doorBlocker != null) doorBlocker.enabled = !isOpen;
    }
}
