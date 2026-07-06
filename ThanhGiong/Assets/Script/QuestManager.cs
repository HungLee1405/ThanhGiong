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

    public event System.Action OnQuestStepChanged;
    public event System.Action OnActiveQuestsChanged;
    public event System.Action<QuestStepType> OnQuestProgressChanged;

    public HashSet<QuestStepType> completedStepTypes = new HashSet<QuestStepType>();

    private List<QuestStep> currentSteps = new List<QuestStep>();
    private List<QuestStep> sideQuestSteps = new List<QuestStep>();
    private int currentSideStepIndex = -1;
    private bool applyingSharedState;
    private int lastScaledPlayerCount = -1;

    private void Start()
    {
        if (gameDayManager != null)
        {
            currentDay = gameDayManager.currentDay;
        }

        VillageStorage storage = FindFirstObjectByType<VillageStorage>();
        if (storage != null)
        {
            storage.OnStorageChanged += RefreshQuestUI;
        }

        LoadDay(currentDay);

        if (!SharedQuestNetwork.TryApplyPendingQuestState(this))
        {
            SharedQuestNetwork.RequestQuestState();
        }
    }

    private void OnDestroy()
    {
        VillageStorage storage = FindFirstObjectByType<VillageStorage>();
        if (storage != null)
        {
            storage.OnStorageChanged -= RefreshQuestUI;
        }
    }

    private void Update()
    {
        if (NetworkLobbyCoordinator.IsOnlineLobbyActive)
            return;

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

        List<QuestStep> allSteps = questDatabase.GetQuestStepsForDay(day);
        
        currentSteps = new List<QuestStep>();
        sideQuestSteps = new List<QuestStep>();

        if (allSteps != null)
        {
            foreach (var step in allSteps)
            {
                if (step.isSideQuest)
                {
                    sideQuestSteps.Add(step);
                }
                else
                {
                    currentSteps.Add(step);
                }
            }
        }

        currentSideStepIndex = sideQuestSteps.Count > 0 ? 0 : -1;

        InitializeBaseRequiredAmounts();
        ApplyMultiplayerRequirements();

        if (currentSteps.Count == 0)
        {
            Debug.LogWarning("Không có nhiệm vụ cho ngày " + day);
            return;
        }

        Debug.Log("Đã load nhiệm vụ cho ngày " + day);
        
        QuestStep firstStep = GetCurrentStep();
        if (firstStep != null)
        {
            TryGrantReward(firstStep, RewardTiming.StartOfStep);
        }

        QuestStep firstSideStep = GetCurrentSideStep();
        if (firstSideStep != null)
        {
            TryGrantReward(firstSideStep, RewardTiming.StartOfStep);
        }

        RefreshQuestUI();
        OnQuestStepChanged?.Invoke();
        OnActiveQuestsChanged?.Invoke();
        CheckStartDayCountdown();
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

    public QuestStep GetCurrentSideStep()
    {
        if (sideQuestSteps == null || sideQuestSteps.Count == 0)
        {
            return null;
        }

        if (currentSideStepIndex < 0 || currentSideStepIndex >= sideQuestSteps.Count)
        {
            return null;
        }

        QuestStep sideStep = sideQuestSteps[currentSideStepIndex];

        // Block side quest until main quest reaches the required step
        if (currentStepIndex < sideStep.unlockAtMainStepIndex)
        {
            return null;
        }

        return sideStep;
    }

    public int CurrentSideStepIndex => currentSideStepIndex;

    public ulong GetCompletedStepMask()
    {
        ulong mask = 0;
        foreach (QuestStepType type in completedStepTypes)
        {
            int bit = (int)type;
            if (bit >= 0 && bit < 64) mask |= 1UL << bit;
        }
        return mask;
    }

    public List<QuestStep> GetActiveSteps()
    {
        List<QuestStep> active = new List<QuestStep>();
        
        QuestStep mainStep = GetCurrentStep();
        if (mainStep != null && !isDayQuestCompleted)
        {
            active.Add(mainStep);
        }

        QuestStep sideStep = GetCurrentSideStep();
        if (sideStep != null)
        {
            active.Add(sideStep);
        }

        return active;
    }

    public bool HasActiveStep()
    {
        return GetActiveSteps().Count > 0;
    }

    public bool IsStepActive(QuestStepType stepType)
    {
        foreach (var step in GetActiveSteps())
        {
            if (step.stepType == stepType)
                return true;
        }
        return false;
    }

    public bool IsStepCompleted(QuestStepType stepType)
    {
        if (completedStepTypes.Contains(stepType))
            return true;
        
        if (sideQuestSteps != null)
        {
            foreach (var step in sideQuestSteps)
            {
                if (step.stepType == stepType && step.IsCompleted())
                    return true;
            }
        }
        if (currentSteps != null)
        {
            foreach (var step in currentSteps)
            {
                if (step.stepType == stepType && step.IsCompleted())
                    return true;
            }
        }
        return false;
    }

    public QuestStep GetActiveStep(QuestStepType stepType)
    {
        foreach (var step in GetActiveSteps())
        {
            if (step.stepType == stepType)
                return step;
        }
        return null;
    }

    public bool CanTalkToNPC(string npcId)
    {
        foreach (var step in GetActiveSteps())
        {
            if (step.stepType == QuestStepType.TalkToNPC && step.targetNPCId == npcId)
            {
                return true;
            }
        }
        return false;
    }

    public string[] GetCurrentDialogueForNPC(string npcId)
    {
        foreach (var step in GetActiveSteps())
        {
            if (step.stepType == QuestStepType.TalkToNPC && step.targetNPCId == npcId)
            {
                return VietnameseText.Fix(step.dialogueLines);
            }
        }
        return null;
    }

    /// <summary>Trả về mảng AudioClip tương ứng với hội thoại hiện tại của NPC.</summary>
    public AudioClip[] GetCurrentVoiceClipsForNPC(string npcId)
    {
        foreach (var step in GetActiveSteps())
        {
            if (step.stepType == QuestStepType.TalkToNPC && step.targetNPCId == npcId)
            {
                return step.voiceClips;
            }
        }
        return null;
    }

    public void CompleteTalkToNPC(string npcId)
    {
        if (!CanTalkToNPC(npcId))
        {
            Debug.Log("NPC này không phải mục tiêu hiện tại: " + npcId);
            return;
        }

        QuestStep targetStep = null;
        foreach (var step in GetActiveSteps())
        {
            if (step.stepType == QuestStepType.TalkToNPC && step.targetNPCId == npcId)
            {
                targetStep = step;
                break;
            }
        }

        if (targetStep == null) return;

        if (!TryGrantReward(targetStep, RewardTiming.TalkToNPC))
        {
            return; // Túi đầy, không cho nhận quest
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
        List<QuestStep> matchingSteps = new List<QuestStep>();
        foreach (var step in GetActiveSteps())
        {
            if (step.stepType != type)
                continue;

            if (!string.IsNullOrEmpty(step.targetItemId) && step.targetItemId != targetId)
                continue;

            if (!string.IsNullOrEmpty(step.targetNPCId) && step.targetNPCId != targetId)
                continue;

            matchingSteps.Add(step);
        }

        if (matchingSteps.Count == 0)
        {
            Debug.Log($"Hành động không khớp nhiệm vụ hiện tại hoặc sai target. Type: {type}, Target: {targetId}");
            return;
        }

        bool stateChanged = false;
        List<QuestStep> stepsToProcess = new List<QuestStep>(matchingSteps);

        foreach (var step in stepsToProcess)
        {
            step.currentAmount += amount;

            if (step.currentAmount > step.requiredAmount)
            {
                step.currentAmount = step.requiredAmount;
            }

            Debug.Log("Tiến độ nhiệm vụ: " + step.currentAmount + "/" + step.requiredAmount);
            stateChanged = true;

            if (step.IsCompleted())
            {
                if (step.isSideQuest)
                {
                    CompleteSideStep(step);
                }
                else
                {
                    CompleteCurrentStep();
                }
            }
        }

        if (stateChanged)
        {
            RefreshQuestUI();
            OnQuestProgressChanged?.Invoke(type);
            OnQuestStepChanged?.Invoke();
            OnActiveQuestsChanged?.Invoke();
            SharedQuestNetwork.PublishState(this);
        }
    }

    public bool CompleteSurviveStep()
    {
        if (sideQuestSteps != null)
        {
            foreach (var sideStep in sideQuestSteps)
            {
                if (sideStep.isRequiredForDayCompletion && !sideStep.IsCompleted())
                {
                    Debug.Log($"Nhiệm vụ phụ bắt buộc chưa hoàn thành: {sideStep.questDescription}");
                    return false;
                }
            }
        }

        QuestStep step = GetCurrentStep();
        if (step != null && step.storageRequirements != null && step.storageRequirements.Count > 0)
        {
            VillageStorage storage = FindFirstObjectByType<VillageStorage>();
            if (storage != null)
            {
                foreach (var req in step.storageRequirements)
                {
                    if (storage.GetAmount(req.targetItemId) < req.requiredAmount)
                    {
                        Debug.Log("Chưa đủ tài nguyên trong kho! Ngày thất bại.");
                        return false; // Chưa đạt yêu cầu kho
                    }
                }
            }
        }
        AddProgress(QuestStepType.SurviveUntilDayEnd, "survive", 1);
        return true;
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
            if (!TryGrantReward(step, RewardTiming.CompletionOfStep))
            {
                return; // Túi đầy, không cho qua bước
            }
            completedStepTypes.Add(step.stepType);
            Debug.Log("Hoàn thành bước nhiệm vụ: " + step.questDescription);
        }

        currentStepIndex++;

        if (currentStepIndex >= currentSteps.Count)
        {
            CompleteDayQuest();
            return;
        }

        QuestStep nextStep = GetCurrentStep();
        if (nextStep != null)
        {
            TryGrantReward(nextStep, RewardTiming.StartOfStep);
        }

        // Check if a side quest just unlocked
        QuestStep activeSideStep = GetCurrentSideStep();
        if (activeSideStep != null && activeSideStep.unlockAtMainStepIndex == currentStepIndex)
        {
            TryGrantReward(activeSideStep, RewardTiming.StartOfStep);
        }

        RefreshQuestUI();
        OnQuestStepChanged?.Invoke();
        CheckStartDayCountdown();
        SharedQuestNetwork.PublishState(this);
    }

    public void CompleteSideStep(QuestStep step)
    {
        if (step != null)
        {
            if (!TryGrantReward(step, RewardTiming.CompletionOfStep))
            {
                return; // Túi đầy, không cho qua bước
            }
            completedStepTypes.Add(step.stepType);
            Debug.Log("Hoàn thành bước nhiệm vụ phụ: " + step.questDescription);
        }

        currentSideStepIndex++;
        OnActiveQuestsChanged?.Invoke();

        if (currentSideStepIndex < sideQuestSteps.Count)
        {
            QuestStep nextSideStep = GetCurrentSideStep();
            if (nextSideStep != null)
            {
                TryGrantReward(nextSideStep, RewardTiming.StartOfStep);
            }
        }
        else
        {
            Debug.Log("Đã hoàn thành toàn bộ chuỗi nhiệm vụ phụ!");
        }
    }

    public void ApplySharedState(
        int day,
        int stepIndex,
        int currentAmount,
        int requiredAmount,
        bool completed,
        int sideStepIndex,
        int sideCurrentAmount,
        int sideRequiredAmount,
        ulong completedMask)
    {
        applyingSharedState = true;

        if (currentDay != day || currentSteps == null || currentSteps.Count == 0)
        {
            LoadDay(day);
        }

        currentDay = day;
        currentStepIndex = Mathf.Clamp(stepIndex, 0, Mathf.Max(0, currentSteps.Count));
        currentSideStepIndex = Mathf.Clamp(sideStepIndex, -1, Mathf.Max(-1, sideQuestSteps.Count));
        isDayQuestCompleted = completed;

        QuestStep step = GetCurrentStep();

        if (step != null)
        {
            step.requiredAmount = Mathf.Max(1, requiredAmount);
            step.currentAmount = Mathf.Clamp(currentAmount, 0, step.requiredAmount);
        }


        QuestStep sideStep = GetCurrentSideStep();
        if (sideStep != null)
        {
            sideStep.requiredAmount = Mathf.Max(1, sideRequiredAmount);
            sideStep.currentAmount = Mathf.Clamp(sideCurrentAmount, 0, sideStep.requiredAmount);
        }

        completedStepTypes.Clear();
        foreach (QuestStepType type in System.Enum.GetValues(typeof(QuestStepType)))
        {
            int bit = (int)type;
            if (bit >= 0 && bit < 64 && (completedMask & (1UL << bit)) != 0)
            {
                completedStepTypes.Add(type);
            }
        }

        RefreshQuestUI();
        applyingSharedState = false;
    }

    private void InitializeBaseRequiredAmounts()
    {
        if (currentSteps != null)
        {
            for (int i = 0; i < currentSteps.Count; i++)
            {
                if (currentSteps[i] != null && currentSteps[i].baseRequiredAmount <= 0)
                {
                    currentSteps[i].baseRequiredAmount = Mathf.Max(1, currentSteps[i].requiredAmount);
                }
            }
        }

        if (sideQuestSteps != null)
        {
            for (int i = 0; i < sideQuestSteps.Count; i++)
            {
                if (sideQuestSteps[i] != null && sideQuestSteps[i].baseRequiredAmount <= 0)
                {
                    sideQuestSteps[i].baseRequiredAmount = Mathf.Max(1, sideQuestSteps[i].requiredAmount);
                }
            }
        }
    }

    private void ApplyMultiplayerRequirements()
    {
        if (!scaleSharedObjectivesWithPlayers)
            return;

        int playerCount = GetSharedQuestPlayerCount();
        lastScaledPlayerCount = playerCount;

        if (currentSteps != null)
        {
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

        if (sideQuestSteps != null)
        {
            for (int i = 0; i < sideQuestSteps.Count; i++)
            {
                QuestStep step = sideQuestSteps[i];

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
        if (playerHubUI == null)
        {
            Debug.LogWarning("PlayerHubUI chưa được gắn vào QuestManager.");
            return;
        }

        QuestStep mainStep = GetCurrentStep();
        QuestStep sideStep = GetCurrentSideStep();

        if (mainStep == null)
        {
            playerHubUI.UpdateQuestUI("Không có nhiệm vụ", "Hiện tại không có nhiệm vụ nào.");
            return;
        }

        string mainProgress = "";
        if (mainStep.requiredAmount > 1)
        {
            mainProgress = "\nTiến độ: " + mainStep.currentAmount + "/" + mainStep.requiredAmount;
        }

        if (mainStep.storageRequirements != null && mainStep.storageRequirements.Count > 0)
        {
            VillageStorage storage = FindFirstObjectByType<VillageStorage>();
            if (storage != null)
            {
                foreach (var req in mainStep.storageRequirements)
                {
                    int has = storage.GetAmount(req.targetItemId);
                    string itemName = req.targetItemId;
                    if (itemName == "iron_ore") itemName = "Sắt";
                    else if (itemName == "bamboo") itemName = "Tre";
                    else if (itemName == "rice") itemName = "Gạo";
                    else if (itemName == "water") itemName = "Nước";

                    mainProgress += $"\nKho - {itemName}: {has}/{req.requiredAmount}";
                }
            }
        }

        string mainDesc = "- " + VietnameseText.Fix(mainStep.questDescription) + VietnameseText.Fix(mainProgress);

        if (sideStep != null)
        {
            string sideProgress = "";
            if (sideStep.requiredAmount > 1)
            {
                sideProgress = "\nTiến độ: " + sideStep.currentAmount + "/" + sideStep.requiredAmount;
            }

            string sideDesc = "- " + VietnameseText.Fix(sideStep.questDescription) + VietnameseText.Fix(sideProgress);

            if (playerHubUI.mainQuestText != null && playerHubUI.sideQuestText != null)
            {
                playerHubUI.mainQuestText.text = $"<b>{VietnameseText.Fix(mainStep.questName)}</b>\n{mainDesc}";
                playerHubUI.sideQuestText.text = $"<b>{VietnameseText.Fix(sideStep.questName)}</b>\n{sideDesc}";
            }

            if (playerHubUI.questNameText != null)
            {
                playerHubUI.questNameText.text = VietnameseText.Fix(mainStep.questName);
            }

            if (playerHubUI.questText != null)
            {
                playerHubUI.questText.text = $"<b>NHIỆM VỤ CHÍNH</b>\n{mainDesc}\n\n<b>NHIỆM VỤ PHỤ</b>\n{sideDesc}";
            }
            playerHubUI.UpdateQuestUI(VietnameseText.Fix(mainStep.questName), mainDesc, VietnameseText.Fix(sideStep.questName), sideDesc);
        }
        else
        {
            if (playerHubUI.mainQuestText != null)
            {
                playerHubUI.mainQuestText.text = $"<b>{VietnameseText.Fix(mainStep.questName)}</b>\n{mainDesc}";
            }
            if (playerHubUI.sideQuestText != null)
            {
                playerHubUI.sideQuestText.text = "";
            }

            playerHubUI.UpdateQuestUI(VietnameseText.Fix(mainStep.questName), mainDesc);
        }
    }

    private bool TryGrantReward(QuestStep step, RewardTiming timing)
    {
        if (step == null || step.rewardItem == null || step.rewardReceived || step.rewardTiming != timing)
            return true;

        NetworkManager networkManager = NetworkManager.Singleton;
        int amountToGrant = step.rewardAmount > 0 ? step.rewardAmount : 1;

        if (networkManager != null && networkManager.IsListening)
        {
            // The server grants shared quest rewards to every player's local inventory.
            // Clients mark their local quest copy and wait for the authoritative reward message.
            if (networkManager.IsServer)
            {
                SharedQuestNetwork.GrantRewardToAll(step.rewardItem.itemId, amountToGrant);
            }

            step.rewardReceived = true;
            return true;
        }

        PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
        if (inventory == null) return false;

        if (step.requireInventorySpace && !inventory.CanAddItem(step.rewardItem, amountToGrant))
        {
            if (playerHubUI != null)
            {
                playerHubUI.UpdateQuestUI("Túi đồ đầy!", "Vui lòng dọn trống ít nhất " + amountToGrant + " ô trong túi để nhận thưởng nhiệm vụ.");
            }
            return false;
        }

        bool added = inventory.AddItem(step.rewardItem, amountToGrant);
        if (added)
        {
            step.rewardReceived = true;
            if (playerHubUI != null && !string.IsNullOrEmpty(step.rewardMessage))
            {
                playerHubUI.UpdateQuestUI("Nhận thưởng!", VietnameseText.Fix(step.rewardMessage));
            }
            return true;
        }
        
        return false;
    }

    public void PrepareSharedActionReward(QuestStepType type, string targetId)
    {
        if (type != QuestStepType.TalkToNPC) return;

        foreach (QuestStep step in GetActiveSteps())
        {
            if (step.stepType != type) continue;
            if (!string.IsNullOrEmpty(step.targetNPCId) && step.targetNPCId != targetId) continue;
            TryGrantReward(step, RewardTiming.TalkToNPC);
        }
    }

    public bool HasCompletedStepType(QuestStepType type)
    {
        return completedStepTypes.Contains(type);
    }

    public bool HasCompletedQuestType(string typeString)
    {
        if (System.Enum.TryParse(typeString, out QuestStepType type))
        {
            return completedStepTypes.Contains(type);
        }
        return false;
    }
}
