using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestRequirement
{
    public string targetItemId;
    public int requiredAmount;
}

[Serializable]
public class QuestStep
{
    public int day;
    public string questName;

    [TextArea(2, 5)]
    public string questDescription;

    public QuestStepType stepType;

    public string targetNPCId;
    public string targetItemId;

    public int requiredAmount = 1;
    [HideInInspector] public int baseRequiredAmount = 0;
    public int currentAmount = 0;

    [Header("Parallel / Side Quest")]
    public bool isSideQuest = false;
    public bool isRequiredForDayCompletion = false;
    public int unlockAtMainStepIndex = 0;

    public List<QuestRequirement> storageRequirements = new List<QuestRequirement>();

    [TextArea(2, 5)]
    public string[] dialogueLines;

    [Header("Rewards")]
    public ItemData rewardItem;
    public int rewardAmount = 1;
    public RewardTiming rewardTiming;
    public bool rewardReceived;
    [TextArea(2, 5)]
    public string rewardMessage;
    public bool requireInventorySpace = true;

    public bool IsCompleted()
    {
        return currentAmount >= requiredAmount;
    }
}
