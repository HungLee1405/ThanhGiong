using System;
using UnityEngine;

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
    public int currentAmount = 0;

    [TextArea(2, 5)]
    public string[] dialogueLines;

    public bool IsCompleted()
    {
        return currentAmount >= requiredAmount;
    }
}