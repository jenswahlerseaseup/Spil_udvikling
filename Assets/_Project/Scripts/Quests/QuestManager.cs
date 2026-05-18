using System;
using System.Collections.Generic;
using UnityEngine;

// Singleton that tracks runtime quest progress. Assign all QuestDefinition assets
// in the Inspector so the manager can look them up by ID.
public sealed class QuestManager : Singleton<QuestManager>
{
    public event Action QuestUpdated;
    public event Action<QuestDefinition> QuestCompleted;

    [SerializeField] private QuestDefinition[] allQuests;

    private readonly Dictionary<string, QuestRuntimeData> runtimeData = new();

    // ── Queries ──────────────────────────────────────────────────────────────

    public QuestState GetStatus(string questId) =>
        runtimeData.TryGetValue(questId, out var d) ? d.Status : QuestState.NotStarted;

    public QuestRuntimeData GetData(string questId) =>
        runtimeData.TryGetValue(questId, out var d) ? d : null;

    public bool IsReadyToComplete(string questId)
    {
        if (!runtimeData.TryGetValue(questId, out var d) || d.Status != QuestState.Active)
            return false;
        var def = FindQuest(questId);
        var steps = def != null ? def.Steps : null;
        return steps != null && d.CurrentStep >= steps.Length;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public void StartQuest(string questId)
    {
        if (GetStatus(questId) != QuestState.NotStarted) return;
        runtimeData[questId] = new QuestRuntimeData
        {
            QuestId      = questId,
            Status       = QuestState.Active,
            CurrentStep  = 0,
            StepProgress = 0,
        };
        QuestUpdated?.Invoke();
    }

    // Call once per collectible/event that advances the current quest step.
    public void RecordProgress(string questId, int amount = 1)
    {
        if (!runtimeData.TryGetValue(questId, out var d) || d.Status != QuestState.Active) return;
        var def = FindQuest(questId);
        var steps = def != null ? def.Steps : null;
        if (steps == null || d.CurrentStep >= steps.Length) return;

        var step = steps[d.CurrentStep];
        d.StepProgress += amount;
        if (d.StepProgress >= step.requiredCount)
        {
            d.CurrentStep++;
            d.StepProgress = 0;
        }
        QuestUpdated?.Invoke();
    }

    public void CompleteQuest(string questId, PlayerInventory inventory)
    {
        if (!IsReadyToComplete(questId)) return;
        runtimeData[questId].Status = QuestState.Completed;

        var def = FindQuest(questId);
        if (def != null && inventory != null)
        {
            if (def.RewardCoins > 0) inventory.AddCoins(def.RewardCoins);
            if (def.RewardItems != null)
                foreach (var item in def.RewardItems)
                    if (item != null) inventory.AddItem(item, 1);
        }

        QuestUpdated?.Invoke();
        QuestCompleted?.Invoke(def);
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public List<QuestSaveEntry> GetSaveEntries()
    {
        var list = new List<QuestSaveEntry>();
        foreach (var pair in runtimeData)
            list.Add(new QuestSaveEntry
            {
                questId      = pair.Key,
                status       = (int)pair.Value.Status,
                currentStep  = pair.Value.CurrentStep,
                stepProgress = pair.Value.StepProgress,
            });
        return list;
    }

    public void LoadSaveEntries(List<QuestSaveEntry> entries)
    {
        runtimeData.Clear();
        if (entries == null) return;
        foreach (var e in entries)
            runtimeData[e.questId] = new QuestRuntimeData
            {
                QuestId      = e.questId,
                Status       = (QuestState)e.status,
                CurrentStep  = e.currentStep,
                StepProgress = e.stepProgress,
            };
        QuestUpdated?.Invoke();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private QuestDefinition FindQuest(string questId)
    {
        if (allQuests == null) return null;
        foreach (var q in allQuests)
            if (q != null && q.QuestId == questId) return q;
        return null;
    }
}
