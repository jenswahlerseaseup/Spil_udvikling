using System;
using UnityEngine;

[Serializable]
public sealed class QuestStep
{
    [TextArea(1, 2)] public string description;
    [Min(1)] public int requiredCount = 1;
}
