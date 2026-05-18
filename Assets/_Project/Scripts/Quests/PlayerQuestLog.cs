using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerQuestLog : MonoBehaviour
{
    public event Action Changed;

    private readonly Dictionary<string, QuestState> states = new();

    public IReadOnlyDictionary<string, QuestState> States => states;

    public QuestState GetState(QuestDefinition quest)
    {
        if (quest == null || !states.TryGetValue(quest.Id, out var state))
        {
            return QuestState.NotStarted;
        }

        return state;
    }

    public void StartQuest(QuestDefinition quest)
    {
        if (quest == null || GetState(quest) != QuestState.NotStarted)
        {
            return;
        }

        states[quest.Id] = QuestState.Active;
        Changed?.Invoke();
    }

    public void CompleteQuest(QuestDefinition quest)
    {
        if (quest == null || GetState(quest) != QuestState.Active)
        {
            return;
        }

        states[quest.Id] = QuestState.Completed;
        Changed?.Invoke();
    }

    public void LoadState(Dictionary<string, QuestState> loadedStates)
    {
        states.Clear();
        if (loadedStates != null)
        {
            foreach (var pair in loadedStates)
            {
                states[pair.Key] = pair.Value;
            }
        }

        Changed?.Invoke();
    }
}
