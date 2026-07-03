using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerConnector : MonoBehaviour
{
    public static bool IsRoomMenuOpen { get; private set; }
    public static string ActiveRoomCode { get; private set; } = "";
    private static string pendingMenuMessage = "";

    private enum MenuPage
    {
        ModeSelect,
        Online,
        InGame
    }

    [Header("Connection")]
    public string address = "127.0.0.1";
    public ushort port = 7777;
    public GameObject playerPrefab;
    public string joinCode = "";
    public int maxPlayers = 6;
    [SerializeField] private LayerMask spawnGroundMask = 1 << 3;
    [SerializeField] private Vector3 onlineSpawnOrigin = new Vector3(-77.876f, 11.23f, 171.307f);
    [SerializeField] private Vector3 onlineSpawnEulerAngles = new Vector3(0f, 138.947f, 0f);
    [SerializeField] private float onlineSpawnSpacing = 1.35f;

    [Header("Optional")]
    public bool showDebugGui = true;

    private Rect sessionWindowRect = new Rect(16f, 16f, 300f, 180f);
    private string currentRoomCode = "";
    private string statusMessage = "";
    private bool isStartingOnline;
    private bool connectedAsClient;
    private bool clientConnectionConfirmed;
    private bool intentionalLeave;
    private bool returningToMenu;
    private NetworkManager callbackManager;
    private MenuPage menuPage = MenuPage.ModeSelect;

    private GUIStyle overlayStyle;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle headingStyle;
    private GUIStyle labelStyle;
    private GUIStyle codeLabelStyle;
    private GUIStyle backButtonStyle;
    private GUIStyle primaryButtonStyle;
    private GUIStyle secondaryButtonStyle;
    private GUIStyle inputStyle;
    private GUIStyle sessionWindowStyle;
    private Font titleFont;
    private Font uiFont;
    private Color panelColor = new Color(0.10f, 0.17f, 0.16f, 0.98f);
    private Color sessionColor = new Color(0.10f, 0.18f, 0.15f, 0.94f);
    private Texture2D overlayTexture;
    private Texture2D panelTexture;
    private Texture2D sessionTexture;
    private Texture2D goldTexture;
    private Texture2D greenTexture;
    private Texture2D backTexture;
    private Texture2D inputTexture;

    private void Start()
    {
        EnsureNetworkManagerExists();
        RegisterNetworkCallbacks(NetworkManager.Singleton);

        if (!string.IsNullOrWhiteSpace(pendingMenuMessage))
        {
            statusMessage = pendingMenuMessage;
            pendingMenuMessage = "";
            menuPage = MenuPage.ModeSelect;
        }
    }

    private void EnsureNetworkManagerExists()
    {
        if (NetworkManager.Singleton != null)
        {
            EnsureTransport(NetworkManager.Singleton.gameObject);
            SharedQuestNetwork.EnsureExists(NetworkManager.Singleton.gameObject);
            NetworkLobbyCoordinator.EnsureExists(NetworkManager.Singleton.gameObject);
            ConfigureNetworkPrefabs(NetworkManager.Singleton);
            return;
        }

        GameObject networkObject = new GameObject("Network Manager");
        NetworkManager manager = networkObject.AddComponent<NetworkManager>();
        EnsureTransport(networkObject);
        SharedQuestNetwork.EnsureExists(networkObject);
        NetworkLobbyCoordinator.EnsureExists(networkObject);
        ConfigureNetworkPrefabs(manager);
        DontDestroyOnLoad(networkObject);
    }

    private void EnsureTransport(GameObject networkObject)
    {
        UnityTransport transport = networkObject.GetComponent<UnityTransport>();

        if (transport == null)
        {
            transport = networkObject.AddComponent<UnityTransport>();
        }

        transport.DisconnectTimeoutMS = 5000;

        NetworkManager manager = networkObject.GetComponent<NetworkManager>();

        if (manager != null)
        {
            if (manager.NetworkConfig == null)
            {
                manager.NetworkConfig = new NetworkConfig();
            }

            manager.NetworkConfig.NetworkTransport = transport;
        }
    }

    private void OnGUI()
    {
        if (!showDebugGui)
            return;

        UpdateRoomMenuCursor();

        NetworkManager manager = NetworkManager.Singleton;

        if (manager != null && manager.IsListening)
        {
            EnsureGuiStyles();
            sessionWindowRect = GUILayout.Window(
                GetInstanceID(),
                sessionWindowRect,
                DrawSessionWindow,
                "PHÒNG TRỰC TUYẾN",
                sessionWindowStyle);
            return;
        }

        if (menuPage != MenuPage.InGame)
        {
            DrawStartupMenu(manager);
        }
    }

    private void DrawStartupMenu(NetworkManager manager)
    {
        EnsureGuiStyles();
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, overlayStyle);

        float width = Mathf.Min(560f, Screen.width - 32f);
        float height = menuPage == MenuPage.ModeSelect ? 410f : 520f;
        height = Mathf.Min(height, Screen.height - 32f);
        Rect panelRect = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height);

        GUILayout.BeginArea(panelRect, panelStyle);
        GUILayout.Space(24f);
        GUILayout.Label("THÁNH GIÓNG", titleStyle);
        GUILayout.Space(8f);

        if (menuPage == MenuPage.ModeSelect)
        {
            GUILayout.Label("CHỌN CHẾ ĐỘ CHƠI", headingStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("CHƠI ĐƠN", primaryButtonStyle, GUILayout.Height(66f)))
            {
                menuPage = MenuPage.InGame;
                statusMessage = "";
                CloseRoomMenu();
            }

            GUILayout.Space(16f);

            if (GUILayout.Button("CHƠI TRỰC TUYẾN", secondaryButtonStyle, GUILayout.Height(66f)))
            {
                menuPage = MenuPage.Online;
                statusMessage = "";
            }

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                GUILayout.Space(16f);
                GUILayout.Label(statusMessage, labelStyle);
            }

            GUILayout.FlexibleSpace();
        }
        else
        {
            GUILayout.Label("TRỰC TUYẾN", headingStyle);
            GUILayout.Space(18f);

            GUI.enabled = !isStartingOnline;

            if (GUILayout.Button("TẠO PHÒNG", primaryButtonStyle, GUILayout.Height(62f)))
            {
                if (manager != null)
                {
                    StartHost(manager);
                }
            }

            GUILayout.Space(22f);
            GUILayout.Label("MÃ PHÒNG", codeLabelStyle);
            joinCode = GUILayout.TextField(CleanJoinCode(joinCode), inputStyle, GUILayout.Height(56f));
            GUILayout.Space(12f);

            if (GUILayout.Button("VÀO PHÒNG", secondaryButtonStyle, GUILayout.Height(62f)))
            {
                if (manager != null)
                {
                    StartClient(manager);
                }
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("QUAY LẠI", backButtonStyle, GUILayout.Height(44f)))
            {
                menuPage = MenuPage.ModeSelect;
                statusMessage = "";
            }

            GUI.enabled = true;

            if (!string.IsNullOrWhiteSpace(statusMessage))
            {
                GUILayout.Space(8f);
                GUILayout.Label(statusMessage, labelStyle);
            }
        }

        GUILayout.EndArea();
    }

    private void DrawSessionWindow(int windowId)
    {
        NetworkManager manager = NetworkManager.Singleton;

        if (manager == null)
            return;

        GUILayout.Label(GetNetworkStatus(manager), codeLabelStyle);

        if (manager.IsHost)
        {
            GUILayout.Label("MÃ PHÒNG", labelStyle);
            GUILayout.TextField(RoomCodeUtility.FormatCode(GetCurrentRoomCode()), inputStyle, GUILayout.Height(42f));
        }
        else
        {
            GUILayout.Label("Đã kết nối tới phòng " + joinCode, labelStyle);
        }

        if (GUILayout.Button("RỜI PHÒNG", secondaryButtonStyle, GUILayout.Height(42f)))
        {
            LeaveSession(manager);
        }

        GUI.DragWindow();
    }

    private void Update()
    {
        if (callbackManager != NetworkManager.Singleton)
        {
            RegisterNetworkCallbacks(NetworkManager.Singleton);
        }

        UpdateRoomMenuCursor();
    }

    private void OnDisable()
    {
        IsRoomMenuOpen = false;
    }

    private void OnDestroy()
    {
        UnregisterNetworkCallbacks();
        DestroyGuiTexture(overlayTexture);
        DestroyGuiTexture(panelTexture);
        DestroyGuiTexture(sessionTexture);
        DestroyGuiTexture(goldTexture);
        DestroyGuiTexture(greenTexture);
        DestroyGuiTexture(backTexture);
        DestroyGuiTexture(inputTexture);
        DestroyRuntimeFont(titleFont);
        DestroyRuntimeFont(uiFont);
    }

    private async void StartHost(NetworkManager manager)
    {
        if (isStartingOnline)
            return;

        isStartingOnline = true;
        statusMessage = "Đang tạo phòng trực tuyến...";

        try
        {
            await EnsureUnityServicesReady();
            ConfigureNetworkPrefabs(manager);

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, maxPlayers - 1));
            currentRoomCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            joinCode = currentRoomCode;
            ActiveRoomCode = currentRoomCode;

            UnityTransport transport = manager.GetComponent<UnityTransport>();

            if (transport != null)
            {
                transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            }

            bool started = manager.StartHost();
            connectedAsClient = false;
            clientConnectionConfirmed = false;
            intentionalLeave = false;
            returningToMenu = false;
            menuPage = started ? MenuPage.InGame : MenuPage.Online;
            if (started)
            {
                CloseRoomMenu();
            }
            statusMessage = started ? "Đang chủ phòng trực tuyến " + GetCurrentRoomCode() : "Tạo phòng thất bại.";
            Debug.Log(started
                ? "Multiplayer: Online host started. Join code: " + GetCurrentRoomCode()
                : "Multiplayer: Online host failed to start.");
        }
        catch (RelayServiceException exception)
        {
            statusMessage = "Kết nối Relay thất bại: " + exception.Reason;
            Debug.LogWarning("Multiplayer: Relay host failed. " + exception);
        }
        catch (RequestFailedException exception)
        {
            statusMessage = "Dịch vụ Unity bị lỗi: " + exception.Message;
            Debug.LogWarning("Multiplayer: Unity Services host failed. " + exception);
        }
        finally
        {
            isStartingOnline = false;
        }
    }

    private async void StartClient(NetworkManager manager)
    {
        if (isStartingOnline)
            return;

        joinCode = CleanJoinCode(joinCode);

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            statusMessage = "Hãy nhập mã phòng trước.";
            Debug.LogWarning("Multiplayer: enter an online join code before joining.");
            return;
        }

        isStartingOnline = true;
        statusMessage = "Đang vào phòng trực tuyến " + joinCode + "...";

        try
        {
            await EnsureUnityServicesReady();
            ConfigureNetworkPrefabs(manager);

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            ActiveRoomCode = joinCode;
            UnityTransport transport = manager.GetComponent<UnityTransport>();

            if (transport != null)
            {
                transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            }

            bool started = manager.StartClient();
            connectedAsClient = started;
            clientConnectionConfirmed = false;
            intentionalLeave = false;
            returningToMenu = false;
            menuPage = started ? MenuPage.InGame : MenuPage.Online;
            if (started)
            {
                CloseRoomMenu();
            }
            statusMessage = started ? "Đang vào phòng trực tuyến " + joinCode + "..." : "Vào phòng thất bại.";
            Debug.Log(started
                ? "Multiplayer: Online client started with join code " + joinCode
                : "Multiplayer: Online client failed to start.");
        }
        catch (RelayServiceException exception)
        {
            statusMessage = "Kết nối Relay thất bại: " + exception.Reason;
            Debug.LogWarning("Multiplayer: Relay join failed. " + exception);
        }
        catch (RequestFailedException exception)
        {
            statusMessage = "Dịch vụ Unity bị lỗi: " + exception.Message;
            Debug.LogWarning("Multiplayer: Unity Services join failed. " + exception);
        }
        finally
        {
            isStartingOnline = false;
        }
    }

    private void ConfigureTransport()
    {
        if (NetworkManager.Singleton == null)
            return;

        ConfigureNetworkPrefabs(NetworkManager.Singleton);

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport != null)
        {
            if (address == "0.0.0.0")
            {
                transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            }
            else
            {
                transport.SetConnectionData(address, port);
            }
        }
    }

    private void ConfigureNetworkPrefabs(NetworkManager manager)
    {
        if (manager == null)
            return;

        if (manager.NetworkConfig == null)
        {
            manager.NetworkConfig = new NetworkConfig();
        }

        manager.NetworkConfig.ConnectionApproval = true;
        manager.ConnectionApprovalCallback = ApproveConnection;

        if (playerPrefab == null)
        {
            playerPrefab = Resources.Load<GameObject>("NetworkPlayer");
        }

        if (playerPrefab == null)
            return;

        manager.NetworkConfig.PlayerPrefab = playerPrefab;

        if (!manager.NetworkConfig.Prefabs.Contains(playerPrefab))
        {
            manager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = playerPrefab });
        }
    }

    private void ApproveConnection(
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        NetworkManager manager = NetworkManager.Singleton;
        int connectedCount = manager != null ? manager.ConnectedClientsIds.Count : 0;

        response.Approved = connectedCount < maxPlayers && !NetworkLobbyCoordinator.MatchStarted;
        response.CreatePlayerObject = response.Approved;
        response.Position = GetSpawnPosition(connectedCount);
        response.Rotation = GetSpawnRotation();
        response.Reason = response.Approved
            ? ""
            : NetworkLobbyCoordinator.MatchStarted ? "Trận đấu đã bắt đầu." : "Phòng đã đầy.";
    }

    private Vector3 GetSpawnPosition(int playerIndex)
    {
        Vector3 offset = Vector3.zero;

        if (playerIndex > 0)
        {
            float angle = (playerIndex - 1) * Mathf.PI * 2f / Mathf.Max(1, maxPlayers - 1);
            offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * onlineSpawnSpacing;
        }

        Vector3 spawnPosition = onlineSpawnOrigin + offset;

        Vector3 probeOrigin = spawnPosition + Vector3.up * 50f;

        if (Physics.Raycast(
            probeOrigin,
            Vector3.down,
            out RaycastHit hit,
            150f,
            spawnGroundMask,
            QueryTriggerInteraction.Ignore))
        {
            spawnPosition.y = hit.point.y + 0.02f;
        }

        return spawnPosition;
    }

    private Quaternion GetSpawnRotation()
    {
        return Quaternion.Euler(onlineSpawnEulerAngles);
    }

    private string GetNetworkStatus(NetworkManager manager)
    {
        if (manager.IsHost) return "Chủ phòng";
        if (manager.IsServer) return "Máy chủ";
        if (manager.IsClient) return "Người chơi";

        return "Trực tuyến";
    }

    private string GetCurrentRoomCode()
    {
        return currentRoomCode;
    }

    private string CleanJoinCode(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        string cleanCode = "";

        for (int i = 0; i < value.Length && cleanCode.Length < 12; i++)
        {
            if (char.IsLetterOrDigit(value[i]))
            {
                cleanCode += char.ToUpperInvariant(value[i]);
            }
        }

        return cleanCode;
    }

    private void UpdateRoomMenuCursor()
    {
        NetworkManager manager = NetworkManager.Singleton;
        IsRoomMenuOpen = showDebugGui
            && (manager == null || !manager.IsListening)
            && menuPage != MenuPage.InGame;

        if (!IsRoomMenuOpen)
            return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseRoomMenu()
    {
        IsRoomMenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LeaveSession(NetworkManager manager)
    {
        Debug.Log("Multiplayer: leaving online room.");
        intentionalLeave = true;
        string message = manager.IsHost ? "Bạn đã đóng phòng." : "Bạn đã rời phòng.";
        BeginReturnToModeSelect(message, manager);
    }

    private void RegisterNetworkCallbacks(NetworkManager manager)
    {
        if (manager == null || callbackManager == manager)
            return;

        UnregisterNetworkCallbacks();
        callbackManager = manager;
        callbackManager.OnClientConnectedCallback += OnClientConnected;
        callbackManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void UnregisterNetworkCallbacks()
    {
        if (callbackManager != null)
        {
            callbackManager.OnClientConnectedCallback -= OnClientConnected;
            callbackManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        callbackManager = null;
    }

    private void OnClientConnected(ulong connectedClientId)
    {
        NetworkManager manager = callbackManager != null ? callbackManager : NetworkManager.Singleton;
        if (manager != null && manager.IsClient && !manager.IsHost
            && connectedClientId == manager.LocalClientId)
        {
            clientConnectionConfirmed = true;
            statusMessage = "Đã kết nối tới phòng trực tuyến " + joinCode + ".";
        }
    }

    private void OnClientDisconnected(ulong disconnectedClientId)
    {
        if (returningToMenu || intentionalLeave || !connectedAsClient)
            return;

        NetworkManager manager = callbackManager != null ? callbackManager : NetworkManager.Singleton;

        if (manager != null && manager.IsHost)
            return;

        if (manager != null && disconnectedClientId != manager.LocalClientId)
            return;

        string disconnectReason = manager != null ? manager.DisconnectReason : "";
        string message;

        if (!clientConnectionConfirmed)
        {
            message = string.IsNullOrWhiteSpace(disconnectReason)
                ? "Không thể kết nối tới phòng. Hãy kiểm tra host và client dùng cùng phiên bản."
                : "Bị từ chối vào phòng: " + disconnectReason;
        }
        else
        {
            message = string.IsNullOrWhiteSpace(disconnectReason)
                ? "Mất kết nối tới chủ phòng."
                : "Mất kết nối: " + disconnectReason;
        }

        Debug.LogWarning(
            "Multiplayer: client disconnected. ClientId=" + disconnectedClientId
            + ", confirmed=" + clientConnectionConfirmed
            + ", reason=" + disconnectReason);
        BeginReturnToModeSelect(message, manager);
    }

    private void BeginReturnToModeSelect(string message, NetworkManager manager)
    {
        if (returningToMenu)
            return;

        returningToMenu = true;
        connectedAsClient = false;
        clientConnectionConfirmed = false;
        ActiveRoomCode = "";
        currentRoomCode = "";
        joinCode = "";
        pendingMenuMessage = message;

        if (manager != null && manager.IsListening)
        {
            manager.Shutdown();
        }

        StartCoroutine(ReloadCurrentScene());
    }

    private IEnumerator ReloadCurrentScene()
    {
        yield return null;
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    public static void LeaveCurrentSession()
    {
        MultiplayerConnector connector = FindFirstObjectByType<MultiplayerConnector>();
        NetworkManager manager = NetworkManager.Singleton;

        if (connector != null && manager != null && manager.IsListening)
        {
            connector.LeaveSession(manager);
        }
    }

    private void EnsureGuiStyles()
    {
        if (panelStyle != null)
            return;

        overlayTexture = CreateColorTexture(new Color(0.02f, 0.04f, 0.05f, 0.72f));
        panelTexture = CreateRoundedRectTexture(96, 96, 14, panelColor);
        sessionTexture = CreateRoundedRectTexture(72, 72, 10, sessionColor);
        goldTexture = CreateRoundedRectTexture(64, 64, 8, new Color(0.88f, 0.63f, 0.24f, 1f));
        greenTexture = CreateRoundedRectTexture(64, 64, 8, new Color(0.20f, 0.52f, 0.43f, 1f));
        backTexture = CreateRoundedRectTexture(64, 64, 8, new Color(0.18f, 0.24f, 0.24f, 1f));
        inputTexture = CreateRoundedRectTexture(64, 64, 7, new Color(0.96f, 0.92f, 0.78f, 1f));
        titleFont = OnlineUIFont.CreateTitleFont();
        uiFont = OnlineUIFont.CreateUIFont();

        overlayStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = overlayTexture }
        };

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(14, 14, 14, 14),
            padding = new RectOffset(42, 42, 28, 30),
            normal = { background = panelTexture }
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            font = titleFont,
            fontSize = 40,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.82f, 0.36f) }
        };

        headingStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            font = uiFont,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.92f, 0.96f, 0.86f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            font = uiFont,
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = new Color(0.86f, 0.91f, 0.82f) }
        };

        codeLabelStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.78f, 0.30f) }
        };

        primaryButtonStyle = CreateButtonStyle(goldTexture, new Color(0.16f, 0.12f, 0.07f));
        secondaryButtonStyle = CreateButtonStyle(greenTexture, Color.white);
        backButtonStyle = CreateButtonStyle(backTexture, new Color(0.90f, 0.94f, 0.88f));

        inputStyle = new GUIStyle(GUI.skin.textField)
        {
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(7, 7, 7, 7),
            font = uiFont,
            fontSize = 25,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(12, 12, 6, 6),
            normal =
            {
                background = inputTexture,
                textColor = new Color(0.15f, 0.12f, 0.08f)
            },
            focused =
            {
                background = inputTexture,
                textColor = new Color(0.15f, 0.12f, 0.08f)
            }
        };

        sessionWindowStyle = new GUIStyle(GUI.skin.window)
        {
            border = new RectOffset(10, 10, 10, 10),
            padding = new RectOffset(18, 18, 30, 16),
            font = titleFont,
            fontStyle = FontStyle.Bold,
            normal = { background = sessionTexture, textColor = new Color(1f, 0.82f, 0.36f) }
        };
    }

    private GUIStyle CreateButtonStyle(Texture2D background, Color textColor)
    {
        return new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            font = uiFont,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(12, 12, 8, 8),
            normal = { background = background, textColor = textColor },
            hover = { background = background, textColor = Color.white },
            active = { background = background, textColor = new Color(1f, 0.92f, 0.70f) }
        };
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private Texture2D CreateRoundedRectTexture(int width, int height, int radius, Color color)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.wrapMode = TextureWrapMode.Clamp;

        float cornerRadius = radius - 0.5f;
        float maxDistance = cornerRadius * cornerRadius;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = 0f;
                float dy = 0f;

                if (x < radius)
                    dx = cornerRadius - x;
                else if (x >= width - radius)
                    dx = x - (width - radius) + 0.5f;

                if (y < radius)
                    dy = cornerRadius - y;
                else if (y >= height - radius)
                    dy = y - (height - radius) + 0.5f;

                bool inside = dx == 0f && dy == 0f || dx * dx + dy * dy <= maxDistance;
                texture.SetPixel(x, y, inside ? color : new Color(color.r, color.g, color.b, 0f));
            }
        }

        texture.Apply();
        return texture;
    }

    private void DestroyRuntimeFont(Font font)
    {
    }

    private void DestroyGuiTexture(Texture2D texture)
    {
        if (texture != null)
        {
            Destroy(texture);
        }
    }

    private async System.Threading.Tasks.Task EnsureUnityServicesReady()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
