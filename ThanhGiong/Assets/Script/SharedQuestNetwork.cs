using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SharedQuestNetwork : MonoBehaviour
{
    private const string ProgressMessage = "ThanhGiongQuestProgress";
    private const string StateMessage = "ThanhGiongQuestState";
    private const string RewardMessage = "ThanhGiongQuestReward";
    private const string StorageMessage = "ThanhGiongStorageDeposit";
    private const string FeedMessage = "ThanhGiongFeed";
    private const string WorldStateMessage = "ThanhGiongWorldState";
    private const string EndingSceneMessage = "ThanhGiongEndingScene";
    private const string ResourceCollectMessage = "ThanhGiongResourceCollect";
    private const string ResourceCollectResultMessage = "ThanhGiongResourceCollectResult";
    private const string ResourceStateMessage = "ThanhGiongResourceState";
    private const string GameOverMessage = "ThanhGiongGameOver";
    private const string ChickenCatchMessage = "ThanhGiongChickenCatch";
    private const string ChickenCatchResultMessage = "ThanhGiongChickenCatchResult";
    private const string ChickenStateMessage = "ThanhGiongChickenState";
    private const string ChickenTransformStateMessage = "ThanhGiongChickenTransformState";
    private const string DayTransitionMessage = "ThanhGiongDayTransition";

    private static SharedQuestNetwork instance;
    private NetworkManager manager;
    private bool messagesRegistered;
    private readonly List<PendingReward> pendingRewards = new List<PendingReward>();

    private struct PendingReward
    {
        public string itemId;
        public int amount;
    }

    public static void EnsureExists(GameObject target)
    {
        if (target == null || target.GetComponent<SharedQuestNetwork>() != null)
            return;

        target.AddComponent<SharedQuestNetwork>();
    }

    private void Awake()
    {
        instance = this;
        manager = GetComponent<NetworkManager>();
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            RegisterMessages(NetworkManager.Singleton);
        }
    }

    private void OnDisable()
    {
        if (manager != null && manager.CustomMessagingManager != null)
        {
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(ProgressMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(StateMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(RewardMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(StorageMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(FeedMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(WorldStateMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(EndingSceneMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(ResourceCollectMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(ResourceCollectResultMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(ResourceStateMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(GameOverMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(ChickenCatchMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(ChickenCatchResultMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(ChickenStateMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(ChickenTransformStateMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(DayTransitionMessage);
            manager.OnClientConnectedCallback -= OnClientConnected;
        }

        messagesRegistered = false;
    }

    private void Update()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (!messagesRegistered && networkManager != null && networkManager.IsListening)
        {
            RegisterMessages(networkManager);
        }

        ProcessPendingRewards();
    }

    public static bool RequestProgress(QuestStepType type, string targetId, int amount)
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
            return false;

        if (networkManager.IsServer)
            return false;

        instance?.RegisterMessages(networkManager);

        using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
        writer.WriteValueSafe((int)type);
        writer.WriteValueSafe(targetId ?? "");
        writer.WriteValueSafe(amount);
        networkManager.CustomMessagingManager.SendNamedMessage(ProgressMessage, NetworkManager.ServerClientId, writer);

        return true;
    }

    public static void PublishState(QuestManager questManager)
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (questManager == null || networkManager == null || !networkManager.IsServer)
            return;

        instance?.RegisterMessages(networkManager);

        QuestStep step = questManager.GetCurrentStep();
        QuestStep sideStep = questManager.GetCurrentSideStep();
        int currentAmount = step != null ? step.currentAmount : 0;
        int requiredAmount = step != null ? step.requiredAmount : 1;
        int sideCurrentAmount = sideStep != null ? sideStep.currentAmount : 0;
        int sideRequiredAmount = sideStep != null ? sideStep.requiredAmount : 1;

        using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
        writer.WriteValueSafe(questManager.currentDay);
        writer.WriteValueSafe(questManager.currentStepIndex);
        writer.WriteValueSafe(currentAmount);
        writer.WriteValueSafe(requiredAmount);
        writer.WriteValueSafe(questManager.isDayQuestCompleted);
        writer.WriteValueSafe(questManager.CurrentSideStepIndex);
        writer.WriteValueSafe(sideCurrentAmount);
        writer.WriteValueSafe(sideRequiredAmount);
        writer.WriteValueSafe(questManager.GetCompletedStepMask());

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == networkManager.LocalClientId)
                continue;

            networkManager.CustomMessagingManager.SendNamedMessage(StateMessage, clientId, writer);
        }
    }

    public static bool RequestStorageDeposit(string itemId, int amount)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening || networkManager.IsServer)
            return false;

        instance?.RegisterMessages(networkManager);
        using FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp);
        writer.WriteValueSafe(itemId ?? "");
        writer.WriteValueSafe(amount);
        networkManager.CustomMessagingManager.SendNamedMessage(StorageMessage, NetworkManager.ServerClientId, writer);
        return true;
    }

    public static bool RequestFeed(float amount)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening || networkManager.IsServer)
            return false;

        instance?.RegisterMessages(networkManager);
        using FastBufferWriter writer = new FastBufferWriter(sizeof(float), Allocator.Temp);
        writer.WriteValueSafe(amount);
        networkManager.CustomMessagingManager.SendNamedMessage(FeedMessage, NetworkManager.ServerClientId, writer);
        return true;
    }

    public static bool RequestResourceCollect(string resourceId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening || networkManager.IsServer)
            return false;

        instance?.RegisterMessages(networkManager);
        using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
        writer.WriteValueSafe(resourceId ?? "");
        networkManager.CustomMessagingManager.SendNamedMessage(ResourceCollectMessage, NetworkManager.ServerClientId, writer);
        return true;
    }

    public static bool RequestChickenCatch(string chickenId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening || networkManager.IsServer)
            return false;

        instance?.RegisterMessages(networkManager);
        using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
        writer.WriteValueSafe(chickenId ?? "");
        networkManager.CustomMessagingManager.SendNamedMessage(ChickenCatchMessage, NetworkManager.ServerClientId, writer);
        return true;
    }

    public static void PublishChickenState(string chickenId, bool caught)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        instance?.RegisterMessages(networkManager);

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == networkManager.LocalClientId)
                continue;

            SendChickenState(clientId, chickenId, caught);
        }
    }

    public static void SendChickenState(ulong clientId, string chickenId, bool caught)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        instance?.RegisterMessages(networkManager);
        using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
        writer.WriteValueSafe(chickenId ?? "");
        writer.WriteValueSafe(caught);
        networkManager.CustomMessagingManager.SendNamedMessage(ChickenStateMessage, clientId, writer);
    }

    public static void PublishChickenTransformState(string chickenId, Vector3 position, float rotationY, bool hidden)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        instance?.RegisterMessages(networkManager);

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == networkManager.LocalClientId)
                continue;

            SendChickenTransformState(clientId, chickenId, position, rotationY, hidden);
        }
    }

    public static void SendChickenTransformState(
        ulong clientId,
        string chickenId,
        Vector3 position,
        float rotationY,
        bool hidden)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        instance?.RegisterMessages(networkManager);
        using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
        writer.WriteValueSafe(chickenId ?? "");
        writer.WriteValueSafe(position);
        writer.WriteValueSafe(rotationY);
        writer.WriteValueSafe(hidden);
        networkManager.CustomMessagingManager.SendNamedMessage(ChickenTransformStateMessage, clientId, writer);
    }

    public static void PublishResourceState(string resourceId, bool hidden, float remainingSeconds)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        instance?.RegisterMessages(networkManager);

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == networkManager.LocalClientId)
                continue;

            SendResourceState(clientId, resourceId, hidden, remainingSeconds);
        }
    }

    public static void SendResourceState(ulong clientId, string resourceId, bool hidden, float remainingSeconds)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        instance?.RegisterMessages(networkManager);
        using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
        writer.WriteValueSafe(resourceId ?? "");
        writer.WriteValueSafe(hidden);
        writer.WriteValueSafe(Mathf.Max(0f, remainingSeconds));
        networkManager.CustomMessagingManager.SendNamedMessage(ResourceStateMessage, clientId, writer);
    }

    public static void PublishWorldState()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        VillageStorage storage = FindFirstObjectByType<VillageStorage>();
        GiongHunger hunger = FindFirstObjectByType<GiongHunger>();
        GameDayManager day = FindFirstObjectByType<GameDayManager>();

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == networkManager.LocalClientId) continue;

            using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
            writer.WriteValueSafe(storage != null ? storage.ironOreAmount : 0);
            writer.WriteValueSafe(storage != null ? storage.bambooAmount : 0);
            writer.WriteValueSafe(storage != null ? storage.waterAmount : 0);
            writer.WriteValueSafe(storage != null ? storage.riceAmount : 0);
            writer.WriteValueSafe(hunger != null ? hunger.currentHunger : 100f);
            writer.WriteValueSafe(hunger != null && hunger.isHungerRunning);
            writer.WriteValueSafe(day != null ? day.currentDay : 1);
            writer.WriteValueSafe(day != null ? day.remainingTime : 0f);
            writer.WriteValueSafe(day != null && day.isDayRunning);
            writer.WriteValueSafe(day != null && day.isTransitioningDay);
            networkManager.CustomMessagingManager.SendNamedMessage(WorldStateMessage, clientId, writer);
        }
    }

    public static void GrantRewardToAll(string itemId, int amount)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer || string.IsNullOrEmpty(itemId) || amount <= 0)
            return;

        instance?.QueueReward(itemId, amount);

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == networkManager.LocalClientId) continue;
            using FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp);
            writer.WriteValueSafe(itemId);
            writer.WriteValueSafe(amount);
            networkManager.CustomMessagingManager.SendNamedMessage(RewardMessage, clientId, writer);
        }
    }

    public static void ShowGameOverForAll(string title, string reason, bool isDeath)
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
        {
            ShowGameOverLocal(title, reason, isDeath);
            return;
        }

        if (!networkManager.IsServer)
            return;

        instance?.RegisterMessages(networkManager);

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == networkManager.LocalClientId)
                continue;

            using FastBufferWriter writer = new FastBufferWriter(512, Allocator.Temp);
            writer.WriteValueSafe(title ?? "");
            writer.WriteValueSafe(reason ?? "");
            writer.WriteValueSafe(isDeath);
            networkManager.CustomMessagingManager.SendNamedMessage(GameOverMessage, clientId, writer);
        }

        ShowGameOverLocal(title, reason, isDeath);
    }

    public static void PlayDayTransitionForAll(int nextDay)
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
            return;

        if (!networkManager.IsServer)
            return;

        instance?.RegisterMessages(networkManager);

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == networkManager.LocalClientId)
                continue;

            using FastBufferWriter writer = new FastBufferWriter(sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(nextDay);
            networkManager.CustomMessagingManager.SendNamedMessage(DayTransitionMessage, clientId, writer);
        }
    }

    public static void LoadEndingSceneForAll(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
        {
            LoadEndingSceneLocal(sceneName);
            return;
        }

        if (!networkManager.IsServer)
            return;

        instance?.RegisterMessages(networkManager);

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == networkManager.LocalClientId)
                continue;

            using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
            writer.WriteValueSafe(sceneName);
            networkManager.CustomMessagingManager.SendNamedMessage(EndingSceneMessage, clientId, writer);
        }

        LoadEndingSceneLocal(sceneName);
    }

    private void RegisterMessages(NetworkManager networkManager)
    {
        if (networkManager == null || networkManager.CustomMessagingManager == null)
            return;

        manager = networkManager;
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ProgressMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(StateMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(RewardMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(StorageMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(FeedMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(WorldStateMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(EndingSceneMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ResourceCollectMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ResourceCollectResultMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ResourceStateMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(GameOverMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ChickenCatchMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ChickenCatchResultMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ChickenStateMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ChickenTransformStateMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(DayTransitionMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ProgressMessage, OnProgressMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(StateMessage, OnStateMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(RewardMessage, OnRewardMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(StorageMessage, OnStorageMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(FeedMessage, OnFeedMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(WorldStateMessage, OnWorldStateMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(EndingSceneMessage, OnEndingSceneMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ResourceCollectMessage, OnResourceCollectMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ResourceCollectResultMessage, OnResourceCollectResultMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ResourceStateMessage, OnResourceStateMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(GameOverMessage, OnGameOverMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ChickenCatchMessage, OnChickenCatchMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ChickenCatchResultMessage, OnChickenCatchResultMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ChickenStateMessage, OnChickenStateMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ChickenTransformStateMessage, OnChickenTransformStateMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(DayTransitionMessage, OnDayTransitionMessage);
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientConnectedCallback += OnClientConnected;
        messagesRegistered = true;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (manager == null || !manager.IsServer)
            return;

        QuestManager questManager = FindFirstObjectByType<QuestManager>();

        if (questManager != null)
        {
            PublishState(questManager);
        }

        PublishWorldState();
        ResourcePickup.PublishKnownSharedStatesToClient(clientId);
        ChickenController.PublishKnownSharedStatesToClient(clientId);
    }

    private void OnProgressMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (manager == null || !manager.IsServer)
            return;

        reader.ReadValueSafe(out int stepTypeValue);
        reader.ReadValueSafe(out string targetId);
        reader.ReadValueSafe(out int amount);

        QuestManager questManager = FindFirstObjectByType<QuestManager>();

        if (questManager == null)
            return;

        QuestStepType stepType = (QuestStepType)stepTypeValue;
        questManager.PrepareSharedActionReward(stepType, targetId);
        questManager.ApplySharedProgress(stepType, targetId, amount);
        PublishState(questManager);
    }

    private void OnStateMessage(ulong senderClientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out int day);
        reader.ReadValueSafe(out int stepIndex);
        reader.ReadValueSafe(out int currentAmount);
        reader.ReadValueSafe(out int requiredAmount);
        reader.ReadValueSafe(out bool completed);
        reader.ReadValueSafe(out int sideStepIndex);
        reader.ReadValueSafe(out int sideCurrentAmount);
        reader.ReadValueSafe(out int sideRequiredAmount);
        reader.ReadValueSafe(out ulong completedMask);

        QuestManager questManager = FindFirstObjectByType<QuestManager>();

        if (questManager != null)
        {
            questManager.ApplySharedState(
                day, stepIndex, currentAmount, requiredAmount, completed,
                sideStepIndex, sideCurrentAmount, sideRequiredAmount, completedMask);
        }
    }

    private void OnRewardMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId) return;
        reader.ReadValueSafe(out string itemId);
        reader.ReadValueSafe(out int amount);
        QueueReward(itemId, amount);
    }

    private void OnStorageMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (manager == null || !manager.IsServer) return;
        reader.ReadValueSafe(out string itemId);
        reader.ReadValueSafe(out int amount);

        VillageStorage storage = FindFirstObjectByType<VillageStorage>();
        if (storage != null && storage.ApplySharedDeposit(itemId, Mathf.Clamp(amount, 1, 8)))
        {
            PublishWorldState();
        }
    }

    private void OnFeedMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (manager == null || !manager.IsServer) return;
        reader.ReadValueSafe(out float amount);
        GiongHunger hunger = FindFirstObjectByType<GiongHunger>();
        if (hunger != null)
        {
            hunger.ApplySharedFeed(Mathf.Clamp(amount, 0f, hunger.maxHunger));
            PublishWorldState();
        }
    }

    private void OnResourceCollectMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (manager == null || !manager.IsServer)
            return;

        reader.ReadValueSafe(out string resourceId);

        bool success = ResourcePickup.TryReserveSharedCollect(
            resourceId,
            out string itemId,
            out int amount,
            out _);

        using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
        writer.WriteValueSafe(resourceId ?? "");
        writer.WriteValueSafe(itemId ?? "");
        writer.WriteValueSafe(amount);
        writer.WriteValueSafe(success);
        manager.CustomMessagingManager.SendNamedMessage(ResourceCollectResultMessage, senderClientId, writer);
    }

    private void OnResourceCollectResultMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out string resourceId);
        reader.ReadValueSafe(out string itemId);
        reader.ReadValueSafe(out int amount);
        reader.ReadValueSafe(out bool success);
        ResourcePickup.ApplySharedCollectResult(resourceId, itemId, amount, success);
    }

    private void OnResourceStateMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out string resourceId);
        reader.ReadValueSafe(out bool hidden);
        reader.ReadValueSafe(out float remainingSeconds);
        ResourcePickup.ApplySharedResourceState(resourceId, hidden, remainingSeconds);
    }

    private void OnChickenCatchMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (manager == null || !manager.IsServer)
            return;

        reader.ReadValueSafe(out string chickenId);

        bool success = ChickenController.TryReserveSharedCatch(
            chickenId,
            out string itemId);

        using FastBufferWriter writer = new FastBufferWriter(256, Allocator.Temp);
        writer.WriteValueSafe(chickenId ?? "");
        writer.WriteValueSafe(itemId ?? "");
        writer.WriteValueSafe(success);
        manager.CustomMessagingManager.SendNamedMessage(ChickenCatchResultMessage, senderClientId, writer);
    }

    private void OnChickenCatchResultMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out string chickenId);
        reader.ReadValueSafe(out string itemId);
        reader.ReadValueSafe(out bool success);
        ChickenController.ApplySharedCatchResult(chickenId, itemId, success);
    }

    private void OnChickenStateMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out string chickenId);
        reader.ReadValueSafe(out bool caught);
        ChickenController.ApplySharedState(chickenId, caught);
    }

    private void OnChickenTransformStateMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out string chickenId);
        reader.ReadValueSafe(out Vector3 position);
        reader.ReadValueSafe(out float rotationY);
        reader.ReadValueSafe(out bool hidden);
        ChickenController.ApplySharedTransformState(chickenId, position, rotationY, hidden);
    }

    private void OnDayTransitionMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out int nextDay);
        GameDayManager.PlaySharedDayTransitionLocal(nextDay);
    }

    private void OnWorldStateMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId) return;

        reader.ReadValueSafe(out int iron);
        reader.ReadValueSafe(out int bamboo);
        reader.ReadValueSafe(out int water);
        reader.ReadValueSafe(out int rice);
        reader.ReadValueSafe(out float hungerValue);
        reader.ReadValueSafe(out bool hungerRunning);
        reader.ReadValueSafe(out int dayValue);
        reader.ReadValueSafe(out float remainingTime);
        reader.ReadValueSafe(out bool dayRunning);
        reader.ReadValueSafe(out bool dayTransitioning);

        FindFirstObjectByType<VillageStorage>()?.ApplySharedState(iron, bamboo, water, rice);
        FindFirstObjectByType<GiongHunger>()?.ApplySharedState(hungerValue, hungerRunning);
        FindFirstObjectByType<GameDayManager>()?.ApplySharedState(dayValue, remainingTime, dayRunning, dayTransitioning);
    }

    private void OnEndingSceneMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out string sceneName);
        LoadEndingSceneLocal(sceneName);
    }

    private void OnGameOverMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out string title);
        reader.ReadValueSafe(out string reason);
        reader.ReadValueSafe(out bool isDeath);
        ShowGameOverLocal(title, reason, isDeath);
    }

    private static void LoadEndingSceneLocal(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private static void ShowGameOverLocal(string title, string reason, bool isDeath)
    {
        GameOverManager gameOver = FindFirstObjectByType<GameOverManager>();
        if (gameOver != null)
        {
            gameOver.TriggerGameOver();
        }

        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.ShowGameOver(title, reason, isDeath);
        }
    }

    private void QueueReward(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
        pendingRewards.Add(new PendingReward { itemId = itemId, amount = amount });
        ProcessPendingRewards();
    }

    private void ProcessPendingRewards()
    {
        if (pendingRewards.Count == 0) return;

        PlayerInventory inventory = FindLocalInventory();
        if (inventory == null) return;

        for (int i = pendingRewards.Count - 1; i >= 0; i--)
        {
            PendingReward reward = pendingRewards[i];
            ItemData item = FindItemData(reward.itemId);
            if (item == null || !inventory.CanAddItem(item, reward.amount)) continue;

            if (inventory.AddItem(item, reward.amount))
            {
                pendingRewards.RemoveAt(i);
            }
        }
    }

    private static PlayerInventory FindLocalInventory()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (PlayerMovement movement in players)
        {
            if (movement == null || !movement.gameObject.activeInHierarchy) continue;
            if (networkManager != null && networkManager.IsListening && movement.IsSpawned && !movement.IsOwner) continue;
            return movement.GetComponent<PlayerInventory>();
        }
        return null;
    }

    private static ItemData FindItemData(string itemId)
    {
        ItemData[] items = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (ItemData item in items)
        {
            if (item != null && item.itemId == itemId) return item;
        }
        return null;
    }
}
