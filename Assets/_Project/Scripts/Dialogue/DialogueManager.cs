using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class DialogueManager : Singleton<DialogueManager>
{
    public event Action DialogueOpened;
    public event Action DialogueClosed;

    // Static shortcut used by GameManager.CanPlayerAct without requiring an Instance check.
    public static bool IsDialogueOpen => Instance != null && Instance.IsOpen;

    public bool IsOpen { get; private set; }

    private DialogueUI    dialogueUI;
    private DialogueDefinition currentDialogue;
    private int           currentLineIndex;
    private Action        onComplete;

    protected override void Awake()
    {
        base.Awake();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        dialogueUI = FindAnyObjectByType<DialogueUI>();
        // If dialogue was open during a scene transition, close it cleanly.
        if (IsOpen) ForceClose();
    }

    public void Open(DialogueDefinition dialogue, Action onComplete = null)
    {
        if (dialogue == null || dialogue.IsEmpty)
        {
            onComplete?.Invoke();
            return;
        }

        currentDialogue   = dialogue;
        currentLineIndex  = 0;
        this.onComplete   = onComplete;
        IsOpen            = true;

        ShowCurrentLine();
        DialogueOpened?.Invoke();
    }

    public void Advance()
    {
        if (!IsOpen) return;
        currentLineIndex++;
        if (currentLineIndex >= currentDialogue.Lines.Length)
        {
            Close();
            return;
        }
        ShowCurrentLine();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        dialogueUI?.Hide();
        var callback = onComplete;
        onComplete       = null;
        currentDialogue  = null;
        callback?.Invoke();
        DialogueClosed?.Invoke();
    }

    private void ShowCurrentLine()
    {
        var line = currentDialogue.Lines[currentLineIndex];
        dialogueUI?.Show(currentDialogue.SpeakerName, line.text);
    }

    private void ForceClose()
    {
        IsOpen = false;
        onComplete      = null;
        currentDialogue = null;
        dialogueUI?.Hide();
    }
}
