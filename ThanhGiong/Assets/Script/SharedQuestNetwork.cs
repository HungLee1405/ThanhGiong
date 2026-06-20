using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class SharedQuestNetwork : MonoBehaviour
{
    private const string ProgressMessage = "ThanhGiongQuestProgress";
    private const string StateMessage = "ThanhGiongQuestState";

    private static SharedQuestNetwork instance;
    private NetworkManager manager;
    private bool messagesRegistered;

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
        int currentAmount = step != null ? step.currentAmount : 0;
        int requiredAmount = step != null ? step.requiredAmount : 1;

        using FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp);
        writer.WriteValueSafe(questManager.currentDay);
        writer.WriteValueSafe(questManager.currentStepIndex);
        writer.WriteValueSafe(currentAmount);
        writer.WriteValueSafe(requiredAmount);
        writer.WriteValueSafe(questManager.isDayQuestCompleted);

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == networkManager.LocalClientId)
                continue;

            networkManager.CustomMessagingManager.SendNamedMessage(StateMessage, clientId, writer);
        }
    }

    private void RegisterMessages(NetworkManager networkManager)
    {
        if (networkManager == null || networkManager.CustomMessagingManager == null)
            return;

        manager = networkManager;
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(ProgressMessage);
        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(StateMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(ProgressMessage, OnProgressMessage);
        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(StateMessage, OnStateMessage);
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

        questManager.ApplySharedProgress((QuestStepType)stepTypeValue, targetId, amount);
        PublishState(questManager);
    }

    private void OnStateMessage(ulong senderClientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out int day);
        reader.ReadValueSafe(out int stepIndex);
        reader.ReadValueSafe(out int currentAmount);
        reader.ReadValueSafe(out int requiredAmount);
        reader.ReadValueSafe(out bool completed);

        QuestManager questManager = FindFirstObjectByType<QuestManager>();

        if (questManager != null)
        {
            questManager.ApplySharedState(day, stepIndex, currentAmount, requiredAmount, completed);
        }
    }
}
