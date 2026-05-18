using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInputReader))]
public sealed class PlayerInteractor : MonoBehaviour
{
    [SerializeField, Min(0f)] private float messageDuration = 4f;

    private readonly List<IInteractable> nearbyInteractables = new();
    private PlayerInputReader inputReader;

    public PlayerInventory Inventory { get; private set; }

    private string activeSpeaker;
    private string activeMessage;
    private float messageHideTime;
    private GUIStyle promptStyle;
    private GUIStyle dialogueStyle;

    private IInteractable CurrentInteractable => nearbyInteractables.Count > 0 ? nearbyInteractables[0] : null;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
        Inventory = GetComponent<PlayerInventory>();
    }

    private void OnEnable()
    {
        inputReader.InteractPressed += Interact;
    }

    private void OnDisable()
    {
        inputReader.InteractPressed -= Interact;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        AddInteractable(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        RemoveInteractable(other);
    }

    public void ShowMessage(string speaker, string message)
    {
        activeSpeaker = speaker;
        activeMessage = message;
        messageHideTime = Time.time + messageDuration;
    }

    private void Interact()
    {
        // Forward to dialogue manager if a conversation is open.
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
        {
            nearbyInteractables.Add(interactable);
        }
    }

    private void RemoveInteractable(Collider2D other)
    {
        if (other.TryGetComponent<IInteractable>(out var interactable))
        {
            nearbyInteractables.Remove(interactable);
        }
    }

    private void OnGUI()
    {
        promptStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.92f, 0.95f, 0.82f) }
        };

        dialogueStyle ??= new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
            padding = new RectOffset(18, 18, 12, 12)
        };

        var current = CurrentInteractable;
        if (current != null)
        {
            GUI.Label(new Rect(Screen.width * 0.5f - 170f, Screen.height - 96f, 340f, 32f), "E - " + current.InteractionPrompt, promptStyle);
        }

        if (!string.IsNullOrEmpty(activeMessage) && Time.time < messageHideTime)
        {
            GUI.Box(
                new Rect(28f, Screen.height - 164f, Screen.width - 56f, 112f),
                activeSpeaker + "\n" + activeMessage,
                dialogueStyle);
        }
    }
}
