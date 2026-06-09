using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [Header("References")]
    public QuestDatabase questDatabase;
    public PlayerHubUI playerHubUI;
    public GameDayManager gameDayManager;

    [Header("Runtime")]
    public int currentDay = 1;
    public int currentStepIndex = 0;
    public bool isDayQuestCompleted = false;

    private List<QuestStep> currentSteps = new List<QuestStep>();

    private void Start()
    {
        if (gameDayManager != null)
        {
            currentDay = gameDayManager.currentDay;
        }

        LoadDay(currentDay);
    }

    public void LoadDay(int day)
    {
        currentDay = day;
        currentStepIndex = 0;
        isDayQuestCompleted = false;

        if (questDatabase == null)
        {
            Debug.LogError("QuestDatabase chưa được gắn vào QuestManager.");
            return;
        }

        currentSteps = questDatabase.GetQuestStepsForDay(day);

        if (currentSteps == null || currentSteps.Count == 0)
        {
            Debug.LogWarning("Không có nhiệm vụ cho ngày " + day);
            return;
        }

        Debug.Log("Đã load nhiệm vụ cho ngày " + day);
        RefreshQuestUI();
    }

    public QuestStep GetCurrentStep()
    {
        if (currentSteps == null || currentSteps.Count == 0)
        {
            return null;
        }

        if (currentStepIndex < 0 || currentStepIndex >= currentSteps.Count)
        {
            return null;
        }

        return currentSteps[currentStepIndex];
    }

    public bool HasActiveStep()
    {
        return GetCurrentStep() != null && !isDayQuestCompleted;
    }

    public bool CanTalkToNPC(string npcId)
    {
        QuestStep step = GetCurrentStep();

        if (step == null)
        {
            return false;
        }

        if (step.stepType != QuestStepType.TalkToNPC)
        {
            return false;
        }

        return step.targetNPCId == npcId;
    }

    public string[] GetCurrentDialogueForNPC(string npcId)
    {
        if (!CanTalkToNPC(npcId))
        {
            return null;
        }

        QuestStep step = GetCurrentStep();

        if (step == null)
        {
            return null;
        }

        return step.dialogueLines;
    }

    public void CompleteTalkToNPC(string npcId)
    {
        if (!CanTalkToNPC(npcId))
        {
            Debug.Log("NPC này không phải mục tiêu hiện tại: " + npcId);
            return;
        }

        CompleteCurrentStep();
    }

    public void AddProgress(QuestStepType type, string targetId, int amount)
    {
        QuestStep step = GetCurrentStep();

        if (step == null)
        {
            Debug.LogWarning("Không có nhiệm vụ hiện tại.");
            return;
        }

        if (step.stepType != type)
        {
            Debug.Log("Hành động không khớp nhiệm vụ hiện tại. Hiện tại cần: " + step.stepType);
            return;
        }

        if (!string.IsNullOrEmpty(step.targetItemId) && step.targetItemId != targetId)
        {
            Debug.Log("Sai target item. Cần: " + step.targetItemId + ", nhưng nhận: " + targetId);
            return;
        }

        step.currentAmount += amount;

        if (step.currentAmount > step.requiredAmount)
        {
            step.currentAmount = step.requiredAmount;
        }

        Debug.Log("Tiến độ nhiệm vụ: " + step.currentAmount + "/" + step.requiredAmount);

        RefreshQuestUI();

        if (step.IsCompleted())
        {
            CompleteCurrentStep();
        }
    }

    public void CompleteSurviveStep()
    {
        QuestStep step = GetCurrentStep();

        if (step == null)
        {
            return;
        }

        if (step.stepType == QuestStepType.SurviveUntilDayEnd)
        {
            step.currentAmount = step.requiredAmount;
            CompleteCurrentStep();
        }
    }

    public void CompleteForgeProgress(int amount)
    {
        AddProgress(QuestStepType.ForgeWeapon, "forge_weapon", amount);
    }

    public void CompleteCurrentStep()
    {
        QuestStep step = GetCurrentStep();

        if (step != null)
        {
            Debug.Log("Hoàn thành bước nhiệm vụ: " + step.questDescription);
        }

        currentStepIndex++;

        if (currentStepIndex >= currentSteps.Count)
        {
            CompleteDayQuest();
            return;
        }

        RefreshQuestUI();
    }

    private void CompleteDayQuest()
    {
        isDayQuestCompleted = true;

        Debug.Log("Hoàn thành toàn bộ nhiệm vụ ngày " + currentDay);

        if (playerHubUI != null)
        {
            playerHubUI.UpdateQuestUI(
                "Hoàn thành nhiệm vụ ngày " + currentDay,
                "Hãy chờ ngày kết thúc hoặc tiếp tục chuẩn bị tài nguyên."
            );
        }
    }

    private void RefreshQuestUI()
    {
        QuestStep step = GetCurrentStep();

        if (step == null)
        {
            if (playerHubUI != null)
            {
                playerHubUI.UpdateQuestUI(
                    "Không có nhiệm vụ",
                    "Hiện tại không có nhiệm vụ nào."
                );
            }

            return;
        }

        if (playerHubUI == null)
        {
            Debug.LogWarning("PlayerHubUI chưa được gắn vào QuestManager.");
            return;
        }

        string progressText = "";

        if (step.requiredAmount > 1)
        {
            progressText = "\nTiến độ: " + step.currentAmount + "/" + step.requiredAmount;
        }

        playerHubUI.UpdateQuestUI(
            step.questName,
            "- " + step.questDescription + progressText
        );
    }
}