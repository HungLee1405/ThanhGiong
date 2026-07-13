using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkLobbyCoordinator : MonoBehaviour
{
    private const string StartMatchMessage = "ThanhGiongLobbyStart";
    private const string MatchStateRequestMessage = "ThanhGiongLobbyStateRequest";
    private const string MatchStateResponseMessage = "ThanhGiongLobbyStateResponse";
    private const float StartBroadcastDuration = 3f;
    private const float StartBroadcastInterval = 0.25f;
    private const float MatchStateRequestInterval = 0.5f;

    public static bool MatchStarted { get; private set; }
    public static bool IsOnlineLobbyActive
    {
        get
        {
            NetworkManager manager = NetworkManager.Singleton;
            return manager != null && manager.IsListening && !MatchStarted;
        }
    }

    public static event Action MatchStartedEvent;

    private NetworkManager manager;
    private bool messageRegistered;
    private Coroutine startBroadcastCoroutine;
    private float nextMatchStateRequestTime;

    public static void EnsureExists(GameObject target)
    {
        if (target != null && target.GetComponent<NetworkLobbyCoordinator>() == null)
        {
            target.AddComponent<NetworkLobbyCoordinator>();
        }
    }

    private void Awake()
    {
        manager = GetComponent<NetworkManager>();
        MatchStarted = false;
    }

    private void Update()
    {
        if (manager == null)
        {
            manager = NetworkManager.Singleton;
        }

        if (!messageRegistered && manager != null && manager.IsListening)
        {
            RegisterMessage();
        }

        RequestMatchStateIfNeeded();

        if (manager != null && !manager.IsListening && MatchStarted)
        {
            MatchStarted = false;
        }

        OnlineUIFont.ApplyToCurrentOnlineText();
    }

    private void OnDisable()
    {
        if (startBroadcastCoroutine != null)
        {
            StopCoroutine(startBroadcastCoroutine);
            startBroadcastCoroutine = null;
        }

        if (messageRegistered && manager != null && manager.CustomMessagingManager != null)
        {
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(StartMatchMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(MatchStateRequestMessage);
            manager.CustomMessagingManager.UnregisterNamedMessageHandler(MatchStateResponseMessage);
            manager.OnClientConnectedCallback -= OnClientConnected;
        }

        messageRegistered = false;
        MatchStarted = false;
    }

    public static bool CanHostStart(out string reason)
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsHost)
        {
            reason = "Chỉ chủ phòng mới được bắt đầu.";
            return false;
        }

        NetworkPlayerAppearance[] players = FindObjectsByType<NetworkPlayerAppearance>(FindObjectsSortMode.None);
        int connectedPlayers = networkManager.ConnectedClientsIds.Count;

        if (players.Length < connectedPlayers || connectedPlayers == 0)
        {
            reason = "Đang chờ người chơi...";
            return false;
        }

        for (int i = 0; i < players.Length; i++)
        {
            if (!players[i].IsSpawned)
                continue;

            if (players[i].playerName.Value.IsEmpty)
            {
                reason = "Mỗi người chơi cần nhập tên.";
                return false;
            }

            if (!players[i].ready.Value)
            {
                reason = "Đang chờ tất cả sẵn sàng.";
                return false;
            }
        }

        reason = "Tất cả đã sẵn sàng.";
        return true;
    }

    public static bool TryStartMatch()
    {
        if (!CanHostStart(out _))
            return false;

        NetworkLobbyCoordinator coordinator = NetworkManager.Singleton.GetComponent<NetworkLobbyCoordinator>();

        if (coordinator == null)
            return false;

        coordinator.StartMatchForEveryone();
        return true;
    }

    private void StartMatchForEveryone()
    {
        if (manager == null || !manager.IsHost)
            return;

        SetMatchStarted();
        StartStartBroadcast();
    }

    private void StartStartBroadcast()
    {
        if (startBroadcastCoroutine != null)
        {
            StopCoroutine(startBroadcastCoroutine);
        }

        startBroadcastCoroutine = StartCoroutine(BroadcastStartMatchForAWhile());
    }

    private IEnumerator BroadcastStartMatchForAWhile()
    {
        float stopTime = Time.unscaledTime + StartBroadcastDuration;

        while (Time.unscaledTime <= stopTime)
        {
            BroadcastStartMatchOnce();
            yield return new WaitForSecondsRealtime(StartBroadcastInterval);
        }

        startBroadcastCoroutine = null;
    }

    private void BroadcastStartMatchOnce()
    {
        if (manager == null || !manager.IsHost || manager.CustomMessagingManager == null)
            return;

        foreach (ulong clientId in manager.ConnectedClientsIds)
        {
            if (clientId == manager.LocalClientId)
                continue;

            using FastBufferWriter writer = new FastBufferWriter(sizeof(bool), Allocator.Temp);
            writer.WriteValueSafe(true);
            manager.CustomMessagingManager.SendNamedMessage(StartMatchMessage, clientId, writer);
        }
    }

    private void RegisterMessage()
    {
        if (manager == null || manager.CustomMessagingManager == null)
            return;

        manager.CustomMessagingManager.UnregisterNamedMessageHandler(StartMatchMessage);
        manager.CustomMessagingManager.UnregisterNamedMessageHandler(MatchStateRequestMessage);
        manager.CustomMessagingManager.UnregisterNamedMessageHandler(MatchStateResponseMessage);
        manager.CustomMessagingManager.RegisterNamedMessageHandler(StartMatchMessage, OnStartMatchMessage);
        manager.CustomMessagingManager.RegisterNamedMessageHandler(MatchStateRequestMessage, OnMatchStateRequestMessage);
        manager.CustomMessagingManager.RegisterNamedMessageHandler(MatchStateResponseMessage, OnMatchStateResponseMessage);
        manager.OnClientConnectedCallback -= OnClientConnected;
        manager.OnClientConnectedCallback += OnClientConnected;
        messageRegistered = true;
    }

    private void OnStartMatchMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out bool shouldStart);

        if (shouldStart)
        {
            SetMatchStarted();
        }
    }

    private void OnMatchStateRequestMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (manager == null || !manager.IsServer)
            return;

        reader.ReadValueSafe(out bool requestState);

        if (requestState)
        {
            SendMatchState(senderClientId);
        }
    }

    private void OnMatchStateResponseMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (senderClientId != NetworkManager.ServerClientId)
            return;

        reader.ReadValueSafe(out bool started);

        if (started)
        {
            SetMatchStarted();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (MatchStarted)
        {
            SendMatchState(clientId);
        }
    }

    private void RequestMatchStateIfNeeded()
    {
        if (MatchStarted ||
            manager == null ||
            !manager.IsListening ||
            manager.IsServer ||
            manager.CustomMessagingManager == null ||
            Time.unscaledTime < nextMatchStateRequestTime)
        {
            return;
        }

        nextMatchStateRequestTime = Time.unscaledTime + MatchStateRequestInterval;

        using FastBufferWriter writer = new FastBufferWriter(sizeof(bool), Allocator.Temp);
        writer.WriteValueSafe(true);
        manager.CustomMessagingManager.SendNamedMessage(MatchStateRequestMessage, NetworkManager.ServerClientId, writer);
    }

    private void SendMatchState(ulong clientId)
    {
        if (manager == null || !manager.IsServer || manager.CustomMessagingManager == null)
            return;

        if (clientId == manager.LocalClientId)
            return;

        using FastBufferWriter writer = new FastBufferWriter(sizeof(bool), Allocator.Temp);
        writer.WriteValueSafe(MatchStarted);
        manager.CustomMessagingManager.SendNamedMessage(MatchStateResponseMessage, clientId, writer);
    }

    private static void SetMatchStarted()
    {
        if (MatchStarted)
            return;

        MatchStarted = true;
        OnlineUIFont.ApplyToCurrentOnlineText(true);
        PersistentBGM.instance?.FadeInMusic(0.25f, 0.5f);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        MatchStartedEvent?.Invoke();
        Debug.Log("Multiplayer: Lobby finished. Match started.");
    }
}
