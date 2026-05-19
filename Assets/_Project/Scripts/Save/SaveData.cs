using System;
using System.Collections.Generic;

[Serializable]
public sealed class SaveData
{
    public int    slotId;
    public string playerName    = "Emil";
    public string currentScene  = SceneNames.Farm;
    public float  playerX;
    public float  playerY;
    public int    coins;
    public int    mischiefPoints;
    public float  soapboxBestDistance;
    public int    soapboxRunCount;
    public float  playTimeSeconds;
    public string timestamp;

    public List<ItemSaveEntry>  items  = new();
    public List<QuestSaveEntry> quests = new();
}

[Serializable]
public sealed class ItemSaveEntry
{
    public string itemId;
    public int    quantity;
}

[Serializable]
public sealed class QuestSaveEntry
{
    public string questId;
    public int    status;       // cast to QuestState
    public int    currentStep;
    public int    stepProgress;
}
