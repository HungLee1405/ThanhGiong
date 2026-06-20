using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    [Header("References")]
    public QuestDatabase questDatabase;
    public PlayerHubUI playerHubUI;
    public GameDayManager gameDayManager;
    public GiongHunger giongHunger;

    [Header("Runtime")]
    public int currentDay = 1;
    public int currentStepIndex = 0;
    public bool isDayQuestCompleted = false;

    [Header("Multiplayer")]
    public bool scaleSharedObjectivesWithPlayers = true;
    public int maxSharedQuestPlayers = 6;

    private List<QuestStep> currentSteps = new List<QuestStep>();
    private bool applyingSharedState;
    private int lastScaledPlayerCount = -1;

    private void Start()
    {
        if (gameDayManager != null)
        {
            currentDay = gameDayManager.currentDay;
        }

        LoadDay(currentDay);
    }

    private void Update()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsServer)
            return;

        int playerCount = GetSharedQuestPlayerCount();

        if (playerCount != lastScaledPlayerCount)
        {
            ApplyMultiplayerRequirements();
            RefreshQuestUI();
            SharedQuestNetwork.PublishState(this);
        }
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
        InitializeBaseRequiredAmounts();
        ApplyMultiplayerRequirements();

        if (currentSteps == null || currentSteps.Count == 0)
        {
            Debug.LogWarning("Không có nhiệm vụ cho ngày " + day);
            return;
        }

        Debug.Log("Đã load nhiệm vụ cho ngày " + day);
        RefreshQuestUI();
        SharedQuestNetwork.PublishState(this);
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

        AddProgress(QuestStepType.TalkToNPC, npcId, 1);
    }

    public void AddProgress(QuestStepType type, string targetId, int amount)
    {
        if (!applyingSharedState && SharedQuestNetwork.RequestProgress(type, targetId, amount))
        {
            return;
        }

        ApplySharedProgress(type, targetId, amount);
    }

    public void ApplySharedProgress(QuestStepType type, string targetId, int amount)
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

        if (!string.IsNullOrEmpty(step.targetNPCId) && step.targetNPCId != targetId)
        {
            Debug.Log("Sai target NPC. Cần: " + step.targetNPCId + ", nhưng nhận: " + targetId);
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
            SharedQuestNetwork.PublishState(this);
            return;
        }

        SharedQuestNetwork.PublishState(this);
    }

    public void CompleteSurviveStep()
    {
        AddProgress(QuestStepType.SurviveUntilDayEnd, "survive", 1);
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
        CheckStartDayCountdown();
        SharedQuestNetwork.PublishState(this);
    }

    public void ApplySharedState(int day, int stepIndex, int currentAmount, int requiredAmount, bool completed)
    {
        applyingSharedState = true;

        if (currentDay != day || currentSteps == null || currentSteps.Count == 0)
        {
            LoadDay(day);
        }

        currentDay = day;
        currentStepIndex = Mathf.Clamp(stepIndex, 0, Mathf.Max(0, currentSteps.Count));
        isDayQuestCompleted = completed;

        QuestStep step = GetCurrentStep();

        if (step != null)
        {
            step.requiredAmount = Mathf.Max(1, requiredAmount);
            step.currentAmount = Mathf.Clamp(currentAmount, 0, step.requiredAmount);
        }

        RefreshQuestUI();
        applyingSharedState = false;
    }

    private void InitializeBaseRequiredAmounts()
    {
        if (currentSteps == null)
            return;

        for (int i = 0; i < currentSteps.Count; i++)
        {
            if (currentSteps[i] != null)
            {
                currentSteps[i].baseRequiredAmount = Mathf.Max(1, currentSteps[i].requiredAmount);
            }
        }
    }

    private void ApplyMultiplayerRequirements()
    {
        if (currentSteps == null || !scaleSharedObjectivesWithPlayers)
            return;

        int playerCount = GetSharedQuestPlayerCount();
        lastScaledPlayerCount = playerCount;

        for (int i = 0; i < currentSteps.Count; i++)
        {
            QuestStep step = currentSteps[i];

            if (step == null)
                continue;

            if (step.baseRequiredAmount <= 0)
            {
                step.baseRequiredAmount = Mathf.Max(1, step.requiredAmount);
            }

            step.requiredAmount = ShouldScaleAsSharedObjective(step)
                ? Mathf.Max(1, step.baseRequiredAmount * playerCount)
                : step.baseRequiredAmount;

            step.currentAmount = Mathf.Clamp(step.currentAmount, 0, step.requiredAmount);
        }
    }

    private int GetSharedQuestPlayerCount()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
            return 1;

        return Mathf.Clamp(networkManager.ConnectedClientsIds.Count, 1, maxSharedQuestPlayers);
    }

    private bool ShouldScaleAsSharedObjective(QuestStep step)
    {
        switch (step.stepType)
        {
            case QuestStepType.CollectWater:
            case QuestStepType.CollectRice:
            case QuestStepType.CookRice:
            case QuestStepType.FeedGiong:
            case QuestStepType.CatchChicken:
            case QuestStepType.CollectIron:
            case QuestStepType.CollectBamboo:
                return true;

            default:
                return false;
        }
    }

    private void CheckStartDayCountdown()
    {
        QuestStep step = GetCurrentStep();

        if (step == null) return;

        if (step.stepType == QuestStepType.SurviveUntilDayEnd)
        {
            if (gameDayManager != null)
            {
                gameDayManager.StartDayCountdown();
            }

            if (giongHunger != null)
            {
                giongHunger.StartHungerDrain();
            }

            Debug.Log("Đã vào giai đoạn sinh tồn. Đồng hồ và thanh đói bắt đầu chạy.");
        }
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
