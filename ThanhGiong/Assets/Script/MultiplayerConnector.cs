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

    [Header("Optional")]
    public bool showDebugGui = true;

    private Rect sessionWindowRect = new Rect(16f, 16f, 300f, 180f);
    private string currentRoomCode = "";
    private string statusMessage = "";
    private bool isStartingOnline;
    private MenuPage menuPage = MenuPage.ModeSelect;

    private GUIStyle overlayStyle;
    private GUIStyle panelStyle;
    private GUIStyle titleStyle;
    private GUIStyle headingStyle;
    private GUIStyle labelStyle;
    private GUIStyle primaryButtonStyle;
    private GUIStyle secondaryButtonStyle;
    private GUIStyle inputStyle;
    private GUIStyle sessionWindowStyle;
    private Texture2D overlayTexture;
    private Texture2D panelTexture;
    private Texture2D goldTexture;
    private Texture2D greenTexture;
    private Texture2D inputTexture;

    private void Start()
    {
        EnsureNetworkManagerExists();
    }

    private void EnsureNetworkManagerExists()
    {
        if (NetworkManager.Singleton != null)
        {
            EnsureTransport(NetworkManager.Singleton.gameObject);
            SharedQuestNetwork.EnsureExists(NetworkManager.Singleton.gameObject);
            ConfigureNetworkPrefabs(NetworkManager.Singleton);
            return;
        }

        GameObject networkObject = new GameObject("Network Manager");
        NetworkManager manager = networkObject.AddComponent<NetworkManager>();
        EnsureTransport(networkObject);
        SharedQuestNetwork.EnsureExists(networkObject);
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
                manager.IsHost ? "ONLINE ROOM" : "ONLINE",
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

        float width = Mathf.Min(540f, Screen.width - 32f);
        float height = menuPage == MenuPage.ModeSelect ? 390f : 500f;
        height = Mathf.Min(height, Screen.height - 32f);
        Rect panelRect = new Rect(
            (Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f,
            width,
            height);

        GUILayout.BeginArea(panelRect, panelStyle);
        GUILayout.Space(20f);
        GUILayout.Label("THANH GIONG", titleStyle);
        GUILayout.Space(4f);

        if (menuPage == MenuPage.ModeSelect)
        {
            GUILayout.Label("CHOOSE GAME MODE", headingStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("OFFLINE", primaryButtonStyle, GUILayout.Height(64f)))
            {
                menuPage = MenuPage.InGame;
                statusMessage = "";
                CloseRoomMenu();
            }

            GUILayout.Space(14f);

            if (GUILayout.Button("ONLINE", secondaryButtonStyle, GUILayout.Height(64f)))
            {
                menuPage = MenuPage.Online;
                statusMessage = "";
            }

            GUILayout.FlexibleSpace();
        }
        else
        {
            GUILayout.Label("ONLINE", headingStyle);
            GUILayout.Space(18f);

            GUI.enabled = !isStartingOnline;

            if (GUILayout.Button("CREATE ROOM", primaryButtonStyle, GUILayout.Height(60f)))
            {
                if (manager != null)
                {
                    StartHost(manager);
                }
            }

            GUILayout.Space(22f);
            GUILayout.Label("ROOM CODE", labelStyle);
            joinCode = GUILayout.TextField(CleanJoinCode(joinCode), inputStyle, GUILayout.Height(56f));
            GUILayout.Space(12f);

            if (GUILayout.Button("JOIN ROOM", secondaryButtonStyle, GUILayout.Height(60f)))
            {
                if (manager != null)
                {
                    StartClient(manager);
                }
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("BACK", GUI.skin.button, GUILayout.Height(42f)))
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

        GUILayout.Label(GetNetworkStatus(manager), labelStyle);

        if (manager.IsHost)
        {
            GUILayout.Label("ROOM CODE", labelStyle);
            GUILayout.TextField(RoomCodeUtility.FormatCode(GetCurrentRoomCode()), inputStyle, GUILayout.Height(38f));
        }
        else
        {
            GUILayout.Label("Connected to " + joinCode, labelStyle);
        }

        if (GUILayout.Button("LEAVE", secondaryButtonStyle, GUILayout.Height(38f)))
        {
            LeaveSession(manager);
        }

        GUI.DragWindow();
    }

    private void Update()
    {
        UpdateRoomMenuCursor();
    }

    private void OnDisable()
    {
        IsRoomMenuOpen = false;
    }

    private void OnDestroy()
    {
        DestroyGuiTexture(overlayTexture);
        DestroyGuiTexture(panelTexture);
        DestroyGuiTexture(goldTexture);
        DestroyGuiTexture(greenTexture);
        DestroyGuiTexture(inputTexture);
    }

    private async void StartHost(NetworkManager manager)
    {
        if (isStartingOnline)
            return;

        isStartingOnline = true;
        statusMessage = "Creating online room...";

        try
        {
            await EnsureUnityServicesReady();
            ConfigureNetworkPrefabs(manager);

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, maxPlayers - 1));
            currentRoomCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            joinCode = currentRoomCode;

            UnityTransport transport = manager.GetComponent<UnityTransport>();

            if (transport != null)
            {
                transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            }

            bool started = manager.StartHost();
            menuPage = started ? MenuPage.InGame : MenuPage.Online;
            if (started)
            {
                CloseRoomMenu();
            }
            statusMessage = started ? "Hosting online room " + GetCurrentRoomCode() : "Host failed.";
            Debug.Log(started
                ? "Multiplayer: Online host started. Join code: " + GetCurrentRoomCode()
                : "Multiplayer: Online host failed to start.");
        }
        catch (RelayServiceException exception)
        {
            statusMessage = "Relay failed: " + exception.Reason;
            Debug.LogWarning("Multiplayer: Relay host failed. " + exception);
        }
        catch (RequestFailedException exception)
        {
            statusMessage = "Unity Services failed: " + exception.Message;
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
            statusMessage = "Enter the online code first.";
            Debug.LogWarning("Multiplayer: enter an online join code before joining.");
            return;
        }

        isStartingOnline = true;
        statusMessage = "Joining online room " + joinCode + "...";

        try
        {
            await EnsureUnityServicesReady();
            ConfigureNetworkPrefabs(manager);

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            UnityTransport transport = manager.GetComponent<UnityTransport>();

            if (transport != null)
            {
                transport.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            }

            bool started = manager.StartClient();
            menuPage = started ? MenuPage.InGame : MenuPage.Online;
            if (started)
            {
                CloseRoomMenu();
            }
            statusMessage = started ? "Joining online room " + joinCode + "..." : "Join failed.";
            Debug.Log(started
                ? "Multiplayer: Online client started with join code " + joinCode
                : "Multiplayer: Online client failed to start.");
        }
        catch (RelayServiceException exception)
        {
            statusMessage = "Relay failed: " + exception.Reason;
            Debug.LogWarning("Multiplayer: Relay join failed. " + exception);
        }
        catch (RequestFailedException exception)
        {
            statusMessage = "Unity Services failed: " + exception.Message;
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

        response.Approved = connectedCount < maxPlayers;
        response.CreatePlayerObject = response.Approved;
        response.Position = GetSpawnPosition(connectedCount);
        response.Rotation = Quaternion.identity;
        response.Reason = response.Approved ? "" : "Room is full.";
    }

    private Vector3 GetSpawnPosition(int playerIndex)
    {
        float angle = playerIndex * Mathf.PI * 2f / Mathf.Max(1, maxPlayers);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 2f;
        Vector3 spawnPosition = offset;

        if (playerPrefab != null)
        {
            spawnPosition = playerPrefab.transform.position + offset;
        }

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

    private string GetNetworkStatus(NetworkManager manager)
    {
        if (manager.IsHost) return "Host";
        if (manager.IsServer) return "Server";
        if (manager.IsClient) return "Client";

        return "Online";
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
        manager.Shutdown();
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private void EnsureGuiStyles()
    {
        if (panelStyle != null)
            return;

        overlayTexture = CreateColorTexture(new Color(0.05f, 0.07f, 0.04f, 0.68f));
        panelTexture = CreateColorTexture(new Color(0.18f, 0.23f, 0.14f, 0.98f));
        goldTexture = CreateColorTexture(new Color(0.82f, 0.61f, 0.23f, 1f));
        greenTexture = CreateColorTexture(new Color(0.29f, 0.48f, 0.24f, 1f));
        inputTexture = CreateColorTexture(new Color(0.96f, 0.90f, 0.73f, 1f));

        overlayStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = overlayTexture }
        };

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(38, 38, 24, 28),
            normal = { background = panelTexture }
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 38,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.84f, 0.42f) }
        };

        headingStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.95f, 0.91f, 0.78f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 16,
            wordWrap = true,
            normal = { textColor = new Color(0.95f, 0.91f, 0.78f) }
        };

        primaryButtonStyle = CreateButtonStyle(goldTexture, new Color(0.16f, 0.12f, 0.07f));
        secondaryButtonStyle = CreateButtonStyle(greenTexture, Color.white);

        inputStyle = new GUIStyle(GUI.skin.textField)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            fontStyle = FontStyle.Bold,
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
            padding = new RectOffset(16, 16, 28, 14),
            fontStyle = FontStyle.Bold,
            normal = { background = panelTexture, textColor = new Color(1f, 0.84f, 0.42f) }
        };
    }

    private GUIStyle CreateButtonStyle(Texture2D background, Color textColor)
    {
        return new GUIStyle(GUI.skin.button)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { background = background, textColor = textColor },
            hover = { background = background, textColor = textColor },
            active = { background = background, textColor = textColor }
        };
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
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
