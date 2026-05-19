using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public sealed class PlayerInteractor : MonoBehaviour
{
    private readonly List<IInteractable> nearbyInteractables = new();
    private PlayerInputReader inputReader;
    private IInteractable lastPrompted;

    public PlayerInventory Inventory { get; private set; }

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        Inventory   = GetComponent<PlayerInventory>();
    }

    private void OnEnable()  => inputReader.InteractPressed += Interact;
    private void OnDisable() => inputReader.InteractPressed -= Interact;

    private void OnTriggerEnter2D(Collider2D other) => AddInteractable(other);
    private void OnTriggerExit2D(Collider2D other)  => RemoveInteractable(other);

    private void Update()
    {
        CleanupMissingInteractables();
        var current = CurrentInteractable;
        if (current == lastPrompted) return;
        lastPrompted = current;

        var prompt = current != null && !string.IsNullOrEmpty(current.InteractionPrompt)
            ? "[ E ]  " + current.InteractionPrompt
            : null;
        GameHud.Instance?.ShowInteractionPrompt(prompt);
    }

    // Called by NPCInteractable, ChickenInteractable, BucketInteractable, etc. for quick feedback.
    public void ShowMessage(string speaker, string message)
    {
        var text = string.IsNullOrEmpty(speaker) ? message : speaker + ":  " + message;
        GameHud.Instance?.ShowNotification(text);
    }

    private IInteractable CurrentInteractable
    {
        get
        {
            IInteractable best = null;
            var bestScore = float.MaxValue;

            foreach (var interactable in nearbyInteractables)
            {
                if (interactable == null || string.IsNullOrEmpty(interactable.InteractionPrompt))
                {
                    continue;
                }

                if (interactable is not Component component)
                {
                    continue;
                }

                var score = (component.transform.position - transform.position).sqrMagnitude;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = interactable;
                }
            }

            return best;
        }
    }

    private void Interact()
    {
        if (!GameManager.CanPlayerAct && !DialogueManager.IsDialogueOpen)
        {
            return;
        }

        if (DialogueManager.IsDialogueOpen)
        {
            DialogueManager.Instance.Advance();
            return;
        }
        CurrentInteractable?.Interact(this);
    }

    private void AddInteractable(Collider2D other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable) && !nearbyInteractables.Contains(interactable))
            nearbyInteractables.Add(interactable);
    }

    private void RemoveInteractable(Collider2D other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            nearbyInteractables.Remove(interactable);
            if (lastPrompted == interactable)
            {
                lastPrompted = null;
                GameHud.Instance?.ShowInteractionPrompt(null);
            }
        }
    }

    private void CleanupMissingInteractables()
    {
        for (var i = nearbyInteractables.Count - 1; i >= 0; i--)
        {
            if (nearbyInteractables[i] == null ||
                nearbyInteractables[i] is Component component && component == null)
            {
                nearbyInteractables.RemoveAt(i);
            }
        }
    }
}
