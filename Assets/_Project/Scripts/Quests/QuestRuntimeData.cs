// Runtime state for one active quest. QuestState (NotStarted/Active/Completed) lives in QuestState.cs.
public sealed class QuestRuntimeData
{
    public string     QuestId;
    public QuestState Status;
    public int        CurrentStep;
    public int        StepProgress;
}
