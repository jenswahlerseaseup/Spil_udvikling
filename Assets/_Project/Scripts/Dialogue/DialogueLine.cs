using System;
using UnityEngine;

[Serializable]
public sealed class DialogueLine
{
    [TextArea(2, 5)] public string text;
}
