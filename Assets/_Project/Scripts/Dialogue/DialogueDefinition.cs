using UnityEngine;

[CreateAssetMenu(menuName = "Nyt Spil/Dialogue/Dialogue")]
public sealed class DialogueDefinition : ScriptableObject
{
    [SerializeField] private string speakerName;
    [SerializeField] private DialogueLine[] lines;

    public string       SpeakerName => speakerName;
    public DialogueLine[] Lines     => lines;

    public bool IsEmpty => lines == null || lines.Length == 0;
}
