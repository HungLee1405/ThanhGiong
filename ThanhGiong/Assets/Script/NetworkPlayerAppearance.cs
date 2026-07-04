using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerAppearance : NetworkBehaviour
{
    public static bool IsLocalSelectionOpen { get; private set; }

    private static readonly Color[] ShirtColors =
    {
        new Color(0.78f, 0.04f, 0.08f),
        new Color(0.06f, 0.18f, 0.78f),
        new Color(0.07f, 0.50f, 0.16f),
        new Color(0.45f, 0.12f, 0.65f),
        new Color(0.36f, 0.18f, 0.07f),
        new Color(0.96f, 0.76f, 0.08f)
    };

    private static readonly string[] CharacterNames =
    {
        "Đỏ", "Xanh lam", "Xanh lá", "Tím", "Nâu", "Vàng"
    };

    private static readonly string[] CharacterPortraitResourceNames =
    {
        "Red", "Blue", "Green", "Purple", "Brown", "Yellow"
    };

    public NetworkVariable<int> colorIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> ready = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<FixedString64Bytes> playerName = new NetworkVariable<FixedString64Bytes>(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Character Select")]
    public bool showSelectionPanel = true;
    public bool tintSkinnedMeshOnly = true;
    public bool useClothingOverlay = true;
    [SerializeField] private RuntimeAnimatorController characterAnimatorController;
    [SerializeField] private float footBoneGroundClearance = 0.035f;
    [SerializeField] private float maxGroundSnapUp = 0.35f;
    [SerializeField] private float maxGroundSnapDown = 4f;
    [SerializeField] private float maxGroundHitAbovePlayer = 0.65f;
    [SerializeField] private float nameTagClearance = 0.14f;
    [SerializeField] private bool buildRedCharacterFromStaticMesh;
    [SerializeField] private string redStaticCharacterResourcePath = "Characters/Do";
    [SerializeField] private Vector3 redStaticCharacterEulerOffset = Vector3.zero;
    [SerializeField] private float redStaticCharacterGroundLift = 0.04f;
    [SerializeField] private GameObject redCharacterPrefab;
    [SerializeField] private Material redCharacterMaterial;
    [SerializeField] private Avatar redCharacterAvatar;
    [SerializeField] private GameObject blueCharacterPrefab;
    [SerializeField] private Material blueCharacterMaterial;
    [SerializeField] private Avatar blueCharacterAvatar;
    [SerializeField] private GameObject greenCharacterPrefab;
    [SerializeField] private Material greenCharacterMaterial;
    [SerializeField] private Avatar greenCharacterAvatar;
    [SerializeField] private GameObject purpleCharacterPrefab;
    [SerializeField] private Material purpleCharacterMaterial;
    [SerializeField] private Avatar purpleCharacterAvatar;
    [SerializeField] private GameObject brownCharacterPrefab;
    [SerializeField] private Material brownCharacterMaterial;
    [SerializeField] private Avatar brownCharacterAvatar;
    [SerializeField] private GameObject yellowCharacterPrefab;
    [SerializeField] private Material yellowCharacterMaterial;
    [SerializeField] private Avatar yellowCharacterAvatar;

    private const int RedCharacterIndex = 0;
    private const int GreenCharacterIndex = 2;
    private const int PurpleCharacterIndex = 3;
    private const int BrownCharacterIndex = 4;
    private const int YellowCharacterIndex = 5;
    private const int DefaultUntintedCharacterIndex = 1;
    private Renderer[] renderers;
    private Transform defaultVisualModelRoot;
    private Transform visualModelRoot;
    private GameObject redCharacterInstance;
    private Transform redStaticVisualRoot;
    private GameObject blueCharacterInstance;
    private GameObject greenCharacterInstance;
    private GameObject purpleCharacterInstance;
    private GameObject brownCharacterInstance;
    private GameObject yellowCharacterInstance;
    private Transform headTopBone;
    private Transform leftToeBone;
    private Transform rightToeBone;
    private Renderer[] visualModelRenderers;
    private Renderer[] clothingRenderers;
    private Material clothingMaterial;
    private int previewColorIndex;
    private string localNameInput = "";
    private bool nameInputFocused;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle panelStyle;
    private GUIStyle cardStyle;
    private GUIStyle selectedCardStyle;
    private GUIStyle readyStyle;
    private GUIStyle statusBadgeStyle;
    private GUIStyle smallButtonStyle;
    private GUIStyle playButtonStyle;
    private GUIStyle closeButtonStyle;
    private GUIStyle inputStyle;
    private GUIStyle rosterStyle;
    private GUIStyle nameTagStyle;
    private GUIStyle inputTextStyle;
    private Font titleFont;
    private Font uiFont;
    private Color panelColor = new Color(0.10f, 0.17f, 0.16f, 0.98f);
    private Texture2D whiteTexture;
    private Texture2D panelTexture;
    private Texture2D cardTexture;
    private Texture2D selectedCardTexture;
    private Texture2D buttonTexture;
    private Texture2D playButtonTexture;
    private Texture2D closeButtonTexture;
    private Texture2D inputTexture;
    private Texture2D blackTexture;
    private Texture2D[] characterPortraits;
    private int appliedColorIndex;
    private float nextGroundSnapTime;
    private readonly Dictionary<Transform, Vector3> visualBaseLocalPositions = new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        defaultVisualModelRoot = transform.Find("Idle");
        visualModelRoot = defaultVisualModelRoot;
        RememberVisualBasePosition(defaultVisualModelRoot);
        RefreshVisualReferences();
        LoadCharacterPortraits();

        CreateClothingOverlay();
        previewColorIndex = Mathf.Clamp(colorIndex.Value, 0, ShirtColors.Length - 1);
    }

    private void Start()
    {
        ApplyOwnerVisibility();
    }

    public override void OnNetworkSpawn()
    {
        colorIndex.OnValueChanged += OnColorChanged;
        ready.OnValueChanged += OnReadyChanged;
        playerName.OnValueChanged += OnPlayerNameChanged;
        NetworkLobbyCoordinator.MatchStartedEvent += OnMatchStarted;

        if (IsOwner)
        {
            previewColorIndex = (int)(OwnerClientId % (ulong)ShirtColors.Length);
            localNameInput = "Người chơi " + (OwnerClientId + 1);
            nameInputFocused = true;
            RequestColorServerRpc(previewColorIndex);
            RequestNameServerRpc(ToNetworkPlayerName(localNameInput));
            RequestReadyServerRpc(false);
            showSelectionPanel = true;
        }

        ApplyColor(colorIndex.Value);
        ApplyOwnerVisibility();
        StartCoroutine(SnapVisualModelToGround());
    }

    public override void OnNetworkDespawn()
    {
        colorIndex.OnValueChanged -= OnColorChanged;
        ready.OnValueChanged -= OnReadyChanged;
        playerName.OnValueChanged -= OnPlayerNameChanged;
        NetworkLobbyCoordinator.MatchStartedEvent -= OnMatchStarted;

        if (IsOwner)
        {
            IsLocalSelectionOpen = false;
            SetCursorForSelection(false);
        }
    }

    private void OnDestroy()
    {
        DestroyRuntimeFont(titleFont);
        DestroyRuntimeFont(uiFont);
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (NetworkLobbyCoordinator.IsOnlineLobbyActive)
        {
            showSelectionPanel = true;
            IsLocalSelectionOpen = true;
            SetCursorForSelection(true);
            return;
        }

        showSelectionPanel = false;
        IsLocalSelectionOpen = false;
        SetCursorForSelection(false);
    }

    private void LateUpdate()
    {
        if (!IsSpawned)
            return;

        if (NetworkLobbyCoordinator.MatchStarted && Time.unscaledTime >= nextGroundSnapTime)
        {
            nextGroundSnapTime = Time.unscaledTime + 0.5f;
            ApplyVisualGroundSnap();
        }

        if (IsOwner)
        {
            SetCursorForSelection(NetworkLobbyCoordinator.IsOnlineLobbyActive);
        }
    }

    private void OnGUI()
    {
        if (!IsSpawned)
            return;

        BuildStyles();
        DrawWorldNameTag();

        if (!IsOwner || !NetworkLobbyCoordinator.IsOnlineLobbyActive)
            return;

        IsLocalSelectionOpen = true;
        SetCursorForSelection(true);
        DrawLobbyPanel();
    }

    private void DrawLobbyPanel()
    {
        float panelWidth = Mathf.Min(980f, Screen.width - 24f);
        float panelHeight = Mathf.Min(640f, Screen.height - 24f);
        Rect panelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight
        );

        Color oldColor = GUI.color;
        GUI.color = new Color(0.02f, 0.04f, 0.05f, 0.58f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture);
        GUI.color = Color.white;
        GUI.Box(panelRect, GUIContent.none, panelStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 26f, panelRect.y + 20f, panelRect.width - 52f, panelRect.height - 40f));
        GUILayout.BeginHorizontal();
        GUILayout.Label("PHÒNG CHỜ TRỰC TUYẾN", titleStyle, GUILayout.ExpandWidth(true));
        GUILayout.Label("MÃ PHÒNG: " + MultiplayerConnector.ActiveRoomCode, statusBadgeStyle, GUILayout.Width(220f), GUILayout.Height(34f));
        GUILayout.EndHorizontal();
        GUILayout.Space(14f);
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width((panelRect.width - 86f) * 0.64f));

        GUILayout.Label("TÊN NGƯỜI CHƠI", readyStyle);
        bool previousGuiEnabled = GUI.enabled;
        GUI.enabled = !ready.Value;
        DrawPlayerNameInput();

        GUILayout.Space(10f);
        GUILayout.Label("NHÂN VẬT", readyStyle);

        float cardWidth = Mathf.Max(124f, ((panelRect.width - 86f) * 0.64f - 42f) / 3f);

        for (int row = 0; row < 2; row++)
        {
            GUILayout.BeginHorizontal();

            for (int column = 0; column < 3; column++)
            {
                int index = row * 3 + column;
                DrawCharacterCard(index, cardWidth);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        GUI.enabled = previousGuiEnabled;
        GUILayout.Space(12f);
        string validPlayerName = SanitizePlayerName(localNameInput);
        previousGuiEnabled = GUI.enabled;
        GUI.enabled = ready.Value || !string.IsNullOrWhiteSpace(validPlayerName);
        string readyButtonText = ready.Value ? "HỦY SẴN SÀNG" : "SẴN SÀNG";

        if (GUILayout.Button(readyButtonText, ready.Value ? closeButtonStyle : playButtonStyle, GUILayout.Height(52f)))
        {
            bool nextReadyState = !ready.Value;

            if (nextReadyState)
            {
                localNameInput = validPlayerName;
            }
            else
            {
                nameInputFocused = true;
            }

            RequestReadyServerRpc(nextReadyState);

            if (nextReadyState)
            {
                RequestNameServerRpc(ToNetworkPlayerName(localNameInput));
            }
        }

        GUI.enabled = previousGuiEnabled;
        GUILayout.EndVertical();
        GUILayout.Space(22f);
        DrawPlayerRoster();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        GUI.color = oldColor;
    }

    private void DrawCharacterCard(int index, float width)
    {
        bool selected = previewColorIndex == index;
        GUIStyle style = selected ? selectedCardStyle : cardStyle;

        GUILayout.BeginVertical(style, GUILayout.Width(width), GUILayout.Height(142f));
        Rect previewRect = GUILayoutUtility.GetRect(width - 22f, 78f);
        DrawCharacterPreview(previewRect, index, ShirtColors[index], selected);
        GUILayout.Label(CharacterNames[index], subtitleStyle);

        if (GUILayout.Button(selected ? "Đã chọn" : "Chọn", smallButtonStyle))
        {
            previewColorIndex = index;
            RequestColorServerRpc(index);
            RequestReadyServerRpc(false);
        }

        GUILayout.EndVertical();
    }

    private void DrawPlayerRoster()
    {
        GUILayout.BeginVertical(rosterStyle, GUILayout.ExpandHeight(true));
        GUILayout.Label("NGƯỜI CHƠI", readyStyle);
        GUILayout.Space(8f);

        NetworkPlayerAppearance[] players = FindObjectsByType<NetworkPlayerAppearance>(FindObjectsSortMode.None);
        Array.Sort(players, (left, right) => left.OwnerClientId.CompareTo(right.OwnerClientId));

        for (int i = 0; i < players.Length; i++)
        {
            if (!players[i].IsSpawned)
                continue;

            GUILayout.BeginHorizontal(cardStyle, GUILayout.Height(46f));
            Color oldColor = GUI.color;
            GUI.color = ShirtColors[Mathf.Clamp(players[i].colorIndex.Value, 0, ShirtColors.Length - 1)];
            GUILayout.Box(GUIContent.none, GUILayout.Width(24f), GUILayout.Height(24f));
            GUI.color = oldColor;

            string displayName = players[i].playerName.Value.IsEmpty
                ? "Đang chọn tên..."
                : players[i].playerName.Value.ToString();
            string hostMarker = players[i].OwnerClientId == NetworkManager.ServerClientId ? " (CHỦ PHÒNG)" : "";
            GUILayout.Label(displayName + hostMarker, subtitleStyle, GUILayout.ExpandWidth(true));
            GUILayout.Label(players[i].ready.Value ? "SẴN SÀNG" : "CHỜ", players[i].ready.Value ? statusBadgeStyle : subtitleStyle, GUILayout.Width(104f));
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
        }

        GUILayout.FlexibleSpace();
        NetworkLobbyCoordinator.CanHostStart(out string startReason);

        if (IsHost)
        {
            bool canStart = NetworkLobbyCoordinator.CanHostStart(out startReason);
            GUILayout.Label(startReason, subtitleStyle);
            GUI.enabled = canStart;

            if (GUILayout.Button("BẮT ĐẦU", playButtonStyle, GUILayout.Height(52f)))
            {
                NetworkLobbyCoordinator.TryStartMatch();
            }

            GUI.enabled = true;
        }
        else
        {
            GUILayout.Label("ĐANG CHỜ CHỦ PHÒNG", subtitleStyle);
        }

        GUILayout.Space(8f);

        if (GUILayout.Button("RỜI PHÒNG", closeButtonStyle, GUILayout.Height(40f)))
        {
            MultiplayerConnector.LeaveCurrentSession();
        }

        GUILayout.EndVertical();
    }

    private void DrawCharacterPreview(Rect rect, int characterIndex, Color color, bool selected)
    {
        Texture2D portrait = characterPortraits != null && characterIndex >= 0 && characterIndex < characterPortraits.Length
            ? characterPortraits[characterIndex]
            : null;

        if (portrait != null)
        {
            Color previousColor = GUI.color;
            float portraitSize = Mathf.Min(rect.width, rect.height);
            Rect portraitRect = new Rect(
                rect.center.x - portraitSize * 0.5f,
                rect.center.y - portraitSize * 0.5f,
                portraitSize,
                portraitSize);
            GUI.color = selected ? Color.white : new Color(0.82f, 0.82f, 0.82f, 1f);
            GUI.DrawTexture(portraitRect, portrait, ScaleMode.ScaleAndCrop, false);
            GUI.color = previousColor;
            return;
        }

        Color oldColor = GUI.color;

        Rect body = new Rect(rect.x + 40f, rect.y + 10f, 46f, 56f);
        Rect head = new Rect(rect.x + 48f, rect.y, 30f, 26f);
        Rect visor = new Rect(rect.x + 58f, rect.y + 8f, 26f, 10f);
        Rect legs = new Rect(rect.x + 42f, rect.y + 62f, 40f, 12f);

        GUI.color = selected ? Color.white : new Color(0.82f, 0.82f, 0.82f);
        GUI.DrawTexture(new Rect(rect.x + 32f, rect.y + 4f, 62f, 76f), whiteTexture);

        GUI.color = color;
        GUI.DrawTexture(body, whiteTexture);
        GUI.DrawTexture(head, whiteTexture);
        GUI.DrawTexture(legs, whiteTexture);

        GUI.color = new Color(0.70f, 0.90f, 1f);
        GUI.DrawTexture(visor, whiteTexture);

        GUI.color = oldColor;
    }

    private void LoadCharacterPortraits()
    {
        characterPortraits = new Texture2D[CharacterPortraitResourceNames.Length];

        for (int i = 0; i < CharacterPortraitResourceNames.Length; i++)
        {
            characterPortraits[i] = Resources.Load<Texture2D>(
                "CharacterPortraits/" + CharacterPortraitResourceNames[i]);
        }
    }

    private void DrawWorldNameTag()
    {
        if (!NetworkLobbyCoordinator.MatchStarted || IsOwner || playerName.Value.IsEmpty)
            return;

        Camera targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        Vector3 labelWorldPosition;

        if (headTopBone != null)
        {
            labelWorldPosition = headTopBone.position + Vector3.up * nameTagClearance;
        }
        else if (GetVisualModelBounds(out Bounds modelBounds))
        {
            labelWorldPosition = new Vector3(
                modelBounds.center.x,
                modelBounds.max.y + nameTagClearance,
                modelBounds.center.z);
        }
        else
        {
            labelWorldPosition = transform.position + Vector3.up * 3.2f;
        }

        Vector3 screenPoint = targetCamera.WorldToScreenPoint(labelWorldPosition);

        if (screenPoint.z <= 0f)
            return;

        string displayName = playerName.Value.ToString();
        GUIContent content = new GUIContent(displayName);
        Vector2 size = nameTagStyle.CalcSize(content);
        float width = Mathf.Max(84f, size.x + 20f);
        Rect rect = new Rect(screenPoint.x - width * 0.5f, Screen.height - screenPoint.y - 16f, width, 30f);
        GUI.Label(rect, content, nameTagStyle);
    }

    private IEnumerator SnapVisualModelToGround()
    {
        yield return null;
        yield return null;

        for (int attempt = 0; attempt < 4; attempt++)
        {
            if (!IsSpawned)
                yield break;

            ApplyVisualGroundSnap();

            if (attempt < 3)
            {
                yield return new WaitForSecondsRealtime(0.35f);
            }
        }
    }

    private void ApplyVisualGroundSnap()
    {
        if (visualModelRoot == null || !GetVisualModelBounds(out Bounds modelBounds))
            return;

        if (!TryFindGroundBelowPlayer(out RaycastHit hit))
        {
            return;
        }

        Vector3 baseLocalPosition = GetVisualBaseLocalPosition(visualModelRoot);
        visualModelRoot.localPosition = baseLocalPosition;

        if (!GetVisualModelBounds(out modelBounds))
            return;

        float visualFootY = modelBounds.min.y;
        float desiredFootY = hit.point.y + footBoneGroundClearance;

        if (leftToeBone != null || rightToeBone != null)
        {
            visualFootY = float.PositiveInfinity;

            if (leftToeBone != null) visualFootY = Mathf.Min(visualFootY, leftToeBone.position.y);
            if (rightToeBone != null) visualFootY = Mathf.Min(visualFootY, rightToeBone.position.y);
            visualFootY = Mathf.Min(visualFootY, modelBounds.min.y);

            desiredFootY = hit.point.y + footBoneGroundClearance;
        }

        float worldCorrection = desiredFootY - visualFootY;

        if (worldCorrection > maxGroundSnapUp || worldCorrection < -maxGroundSnapDown)
            return;

        Transform parent = visualModelRoot.parent;
        float parentScaleY = parent != null ? Mathf.Abs(parent.lossyScale.y) : 1f;
        parentScaleY = Mathf.Max(parentScaleY, 0.0001f);
        visualModelRoot.localPosition = baseLocalPosition + Vector3.up * (worldCorrection / parentScaleY);
    }

    private void RememberVisualBasePosition(Transform visualRoot)
    {
        if (visualRoot == null)
            return;

        visualBaseLocalPositions[visualRoot] = visualRoot.localPosition;
    }

    private Vector3 GetVisualBaseLocalPosition(Transform visualRoot)
    {
        if (visualRoot != null && visualBaseLocalPositions.TryGetValue(visualRoot, out Vector3 basePosition))
        {
            return basePosition;
        }

        return visualRoot != null ? visualRoot.localPosition : Vector3.zero;
    }

    private bool TryFindGroundBelowPlayer(out RaycastHit result)
    {
        result = default;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
        const float rayDistance = 8f;
        bool found = false;
        float bestY = float.NegativeInfinity;

        if (Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit layerGroundHit,
            rayDistance,
            1 << 3,
            QueryTriggerInteraction.Ignore))
        {
            if (IsUsableGroundHit(layerGroundHit, ref bestY))
            {
                result = layerGroundHit;
                found = true;
            }
        }

        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            rayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            if (!IsUsableGroundHit(hits[i], ref bestY))
                continue;

            result = hits[i];
            found = true;
        }

        return found;
    }

    private bool IsUsableGroundHit(RaycastHit hit, ref float bestY)
    {
        if (hit.collider == null)
            return false;

        float maxAllowedY = transform.position.y + maxGroundHitAbovePlayer;

        if (hit.point.y > maxAllowedY)
            return false;

        if (hit.point.y <= bestY)
            return false;

        bestY = hit.point.y;
        return true;
    }

    private void OnMatchStarted()
    {
        if (isActiveAndEnabled)
        {
            StartCoroutine(SnapVisualModelToGround());
        }
    }

    private bool GetVisualModelBounds(out Bounds result)
    {
        result = default;

        if (visualModelRenderers == null || visualModelRenderers.Length == 0)
            return false;

        bool hasBounds = false;

        for (int i = 0; i < visualModelRenderers.Length; i++)
        {
            Renderer targetRenderer = visualModelRenderers[i];

            if (targetRenderer == null || targetRenderer is ParticleSystemRenderer || targetRenderer.forceRenderingOff)
                continue;

            if (!hasBounds)
            {
                result = targetRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(targetRenderer.bounds);
            }
        }

        return hasBounds;
    }

    private void SetCursorForSelection(bool isSelecting)
    {
        bool needsCursor = isSelecting || CookingMenuUI.IsMenuOpen || PauseMenuManager.isPaused;
        Cursor.lockState = needsCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = needsCursor;
    }

    private void DrawPlayerNameInput()
    {
        Rect inputRect = GUILayoutUtility.GetRect(10f, 42f, GUILayout.ExpandWidth(true));
        GUI.Box(inputRect, GUIContent.none, inputStyle);

        Event guiEvent = Event.current;

        if (!ready.Value && guiEvent.type == EventType.MouseDown)
        {
            nameInputFocused = inputRect.Contains(guiEvent.mousePosition);

            if (nameInputFocused)
            {
                guiEvent.Use();
            }
        }

        if (!ready.Value && nameInputFocused && guiEvent.type == EventType.KeyDown)
        {
            if (guiEvent.keyCode == KeyCode.Backspace || guiEvent.keyCode == KeyCode.Delete)
            {
                RemoveLastNameCharacter();
                guiEvent.Use();
            }
            else if (guiEvent.keyCode == KeyCode.Return || guiEvent.keyCode == KeyCode.KeypadEnter)
            {
                nameInputFocused = false;
                guiEvent.Use();
            }
            else if (!char.IsControl(guiEvent.character) && localNameInput.Length < 16)
            {
                localNameInput += guiEvent.character;
                guiEvent.Use();
            }
        }

        GUI.Label(inputRect, localNameInput, inputTextStyle);

        if (!ready.Value && nameInputFocused && Time.realtimeSinceStartup % 1f < 0.5f)
        {
            float textWidth = inputTextStyle.CalcSize(new GUIContent(localNameInput)).x;
            float caretX = Mathf.Min(inputRect.xMax - 12f, inputRect.x + 14f + textWidth);
            Color oldColor = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(caretX, inputRect.y + 9f, 2f, inputRect.height - 18f), whiteTexture);
            GUI.color = oldColor;
        }
    }

    private void RemoveLastNameCharacter()
    {
        if (string.IsNullOrEmpty(localNameInput))
            return;

        int removeCount = 1;

        if (localNameInput.Length >= 2 && char.IsLowSurrogate(localNameInput[localNameInput.Length - 1]))
        {
            removeCount = 2;
        }

        localNameInput = localNameInput.Remove(localNameInput.Length - removeCount, removeCount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestColorServerRpc(int requestedColor, ServerRpcParams rpcParams = default)
    {
        if (!IsRequestFromOwner(rpcParams))
            return;

        colorIndex.Value = Mathf.Clamp(requestedColor, 0, ShirtColors.Length - 1);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestReadyServerRpc(bool isReady, ServerRpcParams rpcParams = default)
    {
        if (!IsRequestFromOwner(rpcParams))
            return;

        ready.Value = isReady;
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNameServerRpc(FixedString64Bytes requestedName, ServerRpcParams rpcParams = default)
    {
        if (!IsRequestFromOwner(rpcParams))
            return;

        playerName.Value = ToNetworkPlayerName(requestedName.ToString());
    }

    private bool IsRequestFromOwner(ServerRpcParams rpcParams)
    {
        return rpcParams.Receive.SenderClientId == OwnerClientId;
    }

    private static FixedString64Bytes ToNetworkPlayerName(string value)
    {
        return new FixedString64Bytes(SanitizePlayerName(value));
    }

    private void OnColorChanged(int previousValue, int newValue)
    {
        previewColorIndex = Mathf.Clamp(newValue, 0, ShirtColors.Length - 1);
        ApplyColor(newValue);
    }

    private void OnReadyChanged(bool previousValue, bool newValue)
    {
    }

    private void OnPlayerNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
    }

    private static string SanitizePlayerName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        string cleanName = "";

        for (int i = 0; i < value.Length && cleanName.Length < 16; i++)
        {
            char character = value[i];

            if (char.IsLetterOrDigit(character) || character == ' ' || character == '_' || character == '-')
            {
                cleanName += character;
            }
        }

        return cleanName.TrimStart();
    }

    private void ApplyColor(int index)
    {
        int safeIndex = Mathf.Clamp(index, 0, ShirtColors.Length - 1);
        Color color = ShirtColors[safeIndex];
        appliedColorIndex = safeIndex;

        if (ApplyCharacterVariant(safeIndex))
        {
            ApplyOwnerVisibility();

            if (isActiveAndEnabled)
            {
                StartCoroutine(SnapVisualModelToGround());
            }

            return;
        }

        if (safeIndex == DefaultUntintedCharacterIndex)
        {
            SetClothingOverlayVisible(false);
            ApplyOwnerVisibility();
            return;
        }

        if (useClothingOverlay)
        {
            ApplyClothingOverlayColor(color);
            ApplyOwnerVisibility();
            return;
        }

        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        bool tintedSkinnedMesh = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i] is ParticleSystemRenderer)
                continue;

            if (tintSkinnedMeshOnly && renderers[i] is not SkinnedMeshRenderer)
                continue;

            TintRenderer(renderers[i], color);
            tintedSkinnedMesh = true;
        }

        if (tintSkinnedMeshOnly && tintedSkinnedMesh)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i] is ParticleSystemRenderer)
                continue;

            TintRenderer(renderers[i], color);
        }
    }

    private bool ApplyCharacterVariant(int index)
    {
        bool useRedCharacter = index == RedCharacterIndex && redCharacterPrefab != null;
        bool useBlueCharacter = index == DefaultUntintedCharacterIndex && blueCharacterPrefab != null;
        bool useGreenCharacter = index == GreenCharacterIndex && greenCharacterPrefab != null;
        bool usePurpleCharacter = index == PurpleCharacterIndex && purpleCharacterPrefab != null;
        bool useBrownCharacter = index == BrownCharacterIndex && brownCharacterPrefab != null;
        bool useYellowCharacter = index == YellowCharacterIndex && yellowCharacterPrefab != null;

        if (useRedCharacter)
        {
            EnsureRedCharacterInstance();
            useRedCharacter = IsCharacterInstanceUsable(redCharacterInstance);
        }

        if (useBlueCharacter)
        {
            EnsureBlueCharacterInstance();
            useBlueCharacter = IsCharacterInstanceUsable(blueCharacterInstance);
        }

        if (useGreenCharacter)
        {
            EnsureGreenCharacterInstance();
            useGreenCharacter = IsCharacterInstanceUsable(greenCharacterInstance);
        }

        if (usePurpleCharacter)
        {
            EnsurePurpleCharacterInstance();
            usePurpleCharacter = IsCharacterInstanceUsable(purpleCharacterInstance);
        }

        if (useBrownCharacter)
        {
            EnsureBrownCharacterInstance();
            useBrownCharacter = IsCharacterInstanceUsable(brownCharacterInstance);
        }

        if (useYellowCharacter)
        {
            EnsureYellowCharacterInstance();
            useYellowCharacter = IsCharacterInstanceUsable(yellowCharacterInstance);
        }

        bool useCustomCharacter =
            useRedCharacter || useBlueCharacter || useGreenCharacter || usePurpleCharacter ||
            useBrownCharacter || useYellowCharacter;

        if (defaultVisualModelRoot != null)
        {
            defaultVisualModelRoot.gameObject.SetActive(!useCustomCharacter);
        }

        if (redCharacterInstance != null)
        {
            redCharacterInstance.SetActive(useRedCharacter);
        }

        if (blueCharacterInstance != null)
        {
            blueCharacterInstance.SetActive(useBlueCharacter);
        }

        if (greenCharacterInstance != null)
        {
            greenCharacterInstance.SetActive(useGreenCharacter);
        }

        if (purpleCharacterInstance != null)
        {
            purpleCharacterInstance.SetActive(usePurpleCharacter);
        }

        if (brownCharacterInstance != null)
        {
            brownCharacterInstance.SetActive(useBrownCharacter);
        }

        if (yellowCharacterInstance != null)
        {
            yellowCharacterInstance.SetActive(useYellowCharacter);
        }

        visualModelRoot = useRedCharacter
            ? redCharacterInstance.transform
            : useBlueCharacter
                ? blueCharacterInstance.transform
                : useGreenCharacter
                    ? greenCharacterInstance.transform
                    : usePurpleCharacter
                        ? purpleCharacterInstance.transform
                        : useBrownCharacter
                            ? brownCharacterInstance.transform
                            : useYellowCharacter
                                ? yellowCharacterInstance.transform
                                : defaultVisualModelRoot;
        BindMovementAnimatorToCurrentModel();
        RefreshVisualReferences();
        renderers = GetComponentsInChildren<Renderer>(true);
        SetClothingOverlayVisible(!useCustomCharacter);

        return useCustomCharacter;
    }

    private void BindMovementAnimatorToCurrentModel()
    {
        if (visualModelRoot == null)
            return;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement == null)
            return;

        Animator animator = visualModelRoot.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            movement.BindCharacterAnimator(animator);
            return;
        }

        Debug.LogWarning(
            $"No Animator found under '{visualModelRoot.name}'. Add an Animator to the visible character model and assign its own Avatar plus the shared PlayerAnimator controller.",
            visualModelRoot);
    }

    private bool IsCharacterInstanceUsable(GameObject instance)
    {
        if (instance == null)
            return false;

        Animator animator = instance.GetComponentInChildren<Animator>(true);
        Avatar activeAvatar = animator != null ? animator.avatar : null;

        if (activeAvatar == null || !activeAvatar.isValid || !activeAvatar.isHuman)
        {
            Avatar fallbackAvatar = GetSharedHumanoidAvatar();

            if (animator != null && fallbackAvatar != null)
            {
                animator.avatar = fallbackAvatar;
                activeAvatar = animator.avatar;
            }
        }

        if (activeAvatar == null || !activeAvatar.isValid || !activeAvatar.isHuman)
        {
            Debug.LogWarning(
                $"Character variant '{instance.name}' has no valid humanoid Avatar. Falling back to the default character visual so it does not appear broken in multiplayer.",
                instance);
            return false;
        }

        return true;
    }

    private void EnsureRedCharacterInstance()
    {
        if (redCharacterInstance != null)
            return;

        redStaticVisualRoot = null;

        if (buildRedCharacterFromStaticMesh)
        {
            redCharacterInstance = CreateRiggedStaticRedCharacterInstance();

            if (redCharacterInstance != null)
                return;
        }

        if (redCharacterPrefab == null)
            return;

        redCharacterInstance = CreateCharacterInstance(
            redCharacterPrefab,
            redCharacterMaterial,
            redCharacterAvatar,
            "Red Character Model");
    }

    private void EnsureBlueCharacterInstance()
    {
        if (blueCharacterInstance != null || blueCharacterPrefab == null)
            return;

        blueCharacterInstance = CreateCharacterInstance(
            blueCharacterPrefab,
            blueCharacterMaterial,
            blueCharacterAvatar,
            "Blue Character Model");
    }

    private void EnsureGreenCharacterInstance()
    {
        if (greenCharacterInstance != null || greenCharacterPrefab == null)
            return;

        greenCharacterInstance = CreateCharacterInstance(
            greenCharacterPrefab,
            greenCharacterMaterial,
            greenCharacterAvatar,
            "Green Character Model");
    }

    private void EnsureYellowCharacterInstance()
    {
        if (yellowCharacterInstance != null || yellowCharacterPrefab == null)
            return;

        yellowCharacterInstance = CreateCharacterInstance(
            yellowCharacterPrefab,
            yellowCharacterMaterial,
            yellowCharacterAvatar,
            "Yellow Character Model");
    }

    private void EnsureBrownCharacterInstance()
    {
        if (brownCharacterInstance != null || brownCharacterPrefab == null)
            return;

        brownCharacterInstance = CreateCharacterInstance(
            brownCharacterPrefab,
            brownCharacterMaterial,
            brownCharacterAvatar,
            "Brown Character Model");
    }

    private void EnsurePurpleCharacterInstance()
    {
        if (purpleCharacterInstance != null || purpleCharacterPrefab == null)
            return;

        purpleCharacterInstance = CreateCharacterInstance(
            purpleCharacterPrefab,
            purpleCharacterMaterial,
            purpleCharacterAvatar,
            "Purple Character Model");
    }

    private GameObject CreateRiggedStaticRedCharacterInstance()
    {
        if (redCharacterPrefab == null)
            return null;

        GameObject staticSourcePrefab = Resources.Load<GameObject>(redStaticCharacterResourcePath);

        if (staticSourcePrefab == null)
        {
            Debug.LogWarning(
                $"Could not load red character mesh resource '{redStaticCharacterResourcePath}'. Falling back to the assigned red character prefab.",
                this);
            return null;
        }

        GameObject instance = Instantiate(redCharacterPrefab, transform);
        instance.name = "Red GLB Character Model";
        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = Quaternion.identity;
        instanceTransform.localScale = Vector3.one;

        EnsureCharacterAnimator(instance, redCharacterAvatar);

        if (!BuildStaticMeshCharacter(instanceTransform, staticSourcePrefab, redCharacterMaterial))
        {
            redStaticVisualRoot = null;
            Destroy(instance);
            return null;
        }

        MatchVariantToDefaultModel(instanceTransform);
        RememberVisualBasePosition(instanceTransform);
        return instance;
    }

    private bool BuildStaticMeshCharacter(Transform rigRoot, GameObject staticSourcePrefab, Material fallbackMaterial)
    {
        if (rigRoot == null || staticSourcePrefab == null)
            return false;

        Bounds targetBounds;
        bool hasTargetBounds = GetHierarchyBoundsInPlayerSpace(rigRoot, out targetBounds);

        Renderer[] rigRenderers = rigRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < rigRenderers.Length; i++)
        {
            if (rigRenderers[i] != null && rigRenderers[i] is not ParticleSystemRenderer)
            {
                rigRenderers[i].forceRenderingOff = true;
                rigRenderers[i].enabled = false;
            }
        }

        GameObject sourceInstance = Instantiate(staticSourcePrefab, rigRoot);
        sourceInstance.name = "Red GLB Source";
        Transform sourceTransform = sourceInstance.transform;
        redStaticVisualRoot = sourceTransform;
        sourceTransform.localPosition = Vector3.zero;
        sourceTransform.localRotation = Quaternion.Euler(redStaticCharacterEulerOffset);
        sourceTransform.localScale = Vector3.one;

        AutoOrientStaticCharacter(sourceTransform);

        if (hasTargetBounds && GetHierarchyBoundsInPlayerSpace(sourceTransform, out Bounds sourceBounds) &&
            sourceBounds.size.y > 0.0001f)
        {
            float uniformScale = targetBounds.size.y / sourceBounds.size.y;
            sourceTransform.localScale = Vector3.one * uniformScale;

            if (GetHierarchyBoundsInPlayerSpace(sourceTransform, out sourceBounds))
            {
                sourceTransform.localPosition += new Vector3(
                    targetBounds.center.x - sourceBounds.center.x,
                    targetBounds.min.y - sourceBounds.min.y + redStaticCharacterGroundLift,
                    targetBounds.center.z - sourceBounds.center.z);
            }
        }

        Renderer[] sourceRenderers = sourceInstance.GetComponentsInChildren<Renderer>(true);

        if (sourceRenderers.Length == 0)
        {
            Debug.LogWarning("The red GLB source has no renderable meshes, so the generated red multiplayer character was not created.", this);
            Destroy(sourceInstance);
            return false;
        }

        sourceInstance.name = "Red GLB Visual";

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            Renderer sourceRenderer = sourceRenderers[i];

            if (sourceRenderer == null || sourceRenderer is ParticleSystemRenderer)
                continue;

            if (fallbackMaterial != null)
            {
                Material[] materials = sourceRenderer.sharedMaterials;

                if (materials == null || materials.Length == 0)
                {
                    materials = new[] { fallbackMaterial };
                }
                else
                {
                    for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        materials[materialIndex] = fallbackMaterial;
                    }
                }

                sourceRenderer.sharedMaterials = materials;
            }

            sourceRenderer.forceRenderingOff = false;
            sourceRenderer.enabled = true;
        }

        Animator[] sourceAnimators = sourceInstance.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < sourceAnimators.Length; i++)
        {
            if (sourceAnimators[i] != null)
            {
                sourceAnimators[i].enabled = false;
            }
        }

        Collider[] sourceColliders = sourceInstance.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < sourceColliders.Length; i++)
        {
            if (sourceColliders[i] != null)
            {
                sourceColliders[i].enabled = false;
            }
        }

        return true;
    }

    private void AutoOrientStaticCharacter(Transform sourceTransform)
    {
        if (sourceTransform == null)
            return;

        Quaternion baseRotation = sourceTransform.localRotation;
        Quaternion[] candidateRotations =
        {
            baseRotation,
            baseRotation * Quaternion.Euler(90f, 0f, 0f),
            baseRotation * Quaternion.Euler(-90f, 0f, 0f),
            baseRotation * Quaternion.Euler(0f, 90f, 0f),
            baseRotation * Quaternion.Euler(0f, -90f, 0f),
            baseRotation * Quaternion.Euler(0f, 0f, 90f),
            baseRotation * Quaternion.Euler(0f, 0f, -90f),
            baseRotation * Quaternion.Euler(90f, 0f, 90f),
            baseRotation * Quaternion.Euler(-90f, 0f, 90f),
            baseRotation * Quaternion.Euler(90f, 90f, 0f),
            baseRotation * Quaternion.Euler(-90f, 90f, 0f),
        };

        Quaternion bestRotation = baseRotation;
        Vector3 bestEuler = sourceTransform.localEulerAngles;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < candidateRotations.Length; i++)
        {
            sourceTransform.localRotation = candidateRotations[i];

            if (!GetHierarchyBoundsInPlayerSpace(sourceTransform, out Bounds bounds) ||
                bounds.size.y <= 0.0001f)
            {
                continue;
            }

            float horizontalSize = Mathf.Max(bounds.size.x, bounds.size.z, 0.0001f);
            float uprightRatio = bounds.size.y / horizontalSize;
            float score = bounds.size.y + uprightRatio * 0.5f;

            if (score > bestScore)
            {
                bestScore = score;
                bestRotation = candidateRotations[i];
                bestEuler = sourceTransform.localEulerAngles;
            }
        }

        sourceTransform.localRotation = bestRotation;

        Debug.Log(
            $"Red GLB auto-orient selected local Euler {bestEuler} with score {bestScore:0.###}.",
            sourceTransform);
    }

    private GameObject CreateCharacterInstance(
        GameObject characterPrefab,
        Material characterMaterial,
        Avatar characterAvatar,
        string instanceName)
    {
        GameObject instance = Instantiate(characterPrefab, transform);
        instance.name = instanceName;
        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = Quaternion.identity;
        instanceTransform.localScale = Vector3.one;

        EnsureCharacterAnimator(instance, characterAvatar);
        ApplyCharacterMaterial(instanceTransform, characterMaterial);
        MatchVariantToDefaultModel(instanceTransform);
        RememberVisualBasePosition(instanceTransform);

        return instance;
    }

    private void EnsureCharacterAnimator(GameObject instance, Avatar characterAvatar)
    {
        if (instance == null)
            return;

        Animator animator = instance.GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }

        if (characterAvatar != null)
        {
            animator.avatar = characterAvatar;
        }

        if ((animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman) &&
            GetSharedHumanoidAvatar() != null)
        {
            animator.avatar = GetSharedHumanoidAvatar();
        }

        if (characterAnimatorController != null)
        {
            animator.runtimeAnimatorController = characterAnimatorController;
        }

        animator.enabled = true;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.Rebind();

        if (animator.gameObject.activeInHierarchy)
        {
            animator.Update(0f);
        }
    }

    private Avatar GetSharedHumanoidAvatar()
    {
        if (blueCharacterAvatar != null && blueCharacterAvatar.isValid && blueCharacterAvatar.isHuman)
            return blueCharacterAvatar;

        if (greenCharacterAvatar != null && greenCharacterAvatar.isValid && greenCharacterAvatar.isHuman)
            return greenCharacterAvatar;

        if (redCharacterAvatar != null && redCharacterAvatar.isValid && redCharacterAvatar.isHuman)
            return redCharacterAvatar;

        if (purpleCharacterAvatar != null && purpleCharacterAvatar.isValid && purpleCharacterAvatar.isHuman)
            return purpleCharacterAvatar;

        if (brownCharacterAvatar != null && brownCharacterAvatar.isValid && brownCharacterAvatar.isHuman)
            return brownCharacterAvatar;

        if (yellowCharacterAvatar != null && yellowCharacterAvatar.isValid && yellowCharacterAvatar.isHuman)
            return yellowCharacterAvatar;

        return null;
    }

    private void ApplyCharacterMaterial(Transform characterRoot, Material characterMaterial)
    {
        if (characterRoot == null || characterMaterial == null)
            return;

        Renderer[] characterRenderers = characterRoot.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < characterRenderers.Length; i++)
        {
            Material[] materials = characterRenderers[i].sharedMaterials;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                materials[materialIndex] = characterMaterial;
            }

            characterRenderers[i].sharedMaterials = materials;
        }
    }

    private void CopyDefaultPoseToVariant(Transform variantRoot)
    {
        if (defaultVisualModelRoot == null || variantRoot == null)
            return;

        Transform[] defaultBones = defaultVisualModelRoot.GetComponentsInChildren<Transform>(true);
        Dictionary<string, Transform> defaultBonesByName = new Dictionary<string, Transform>();

        for (int i = 0; i < defaultBones.Length; i++)
        {
            if (!defaultBonesByName.ContainsKey(defaultBones[i].name))
            {
                defaultBonesByName.Add(defaultBones[i].name, defaultBones[i]);
            }
        }

        Transform[] variantBones = variantRoot.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < variantBones.Length; i++)
        {
            Transform targetBone = variantBones[i];

            if (!targetBone.name.StartsWith("mixamorig:", StringComparison.Ordinal) ||
                !defaultBonesByName.TryGetValue(targetBone.name, out Transform sourceBone))
            {
                continue;
            }

            targetBone.localRotation = sourceBone.localRotation;
        }
    }

    private void MatchVariantToDefaultModel(Transform variantRoot)
    {
        if (defaultVisualModelRoot == null || variantRoot == null)
            return;

        if (!GetHierarchyBoundsInPlayerSpace(defaultVisualModelRoot, out Bounds defaultBounds) ||
            !GetHierarchyBoundsInPlayerSpace(variantRoot, out Bounds variantBounds) ||
            variantBounds.size.y <= 0.0001f)
        {
            variantRoot.localPosition = defaultVisualModelRoot.localPosition;
            variantRoot.localRotation = defaultVisualModelRoot.localRotation;
            return;
        }

        float uniformScale = defaultBounds.size.y / variantBounds.size.y;
        variantRoot.localScale = Vector3.one * uniformScale;

        if (!GetHierarchyBoundsInPlayerSpace(variantRoot, out variantBounds))
            return;

        variantRoot.localPosition += new Vector3(
            defaultBounds.center.x - variantBounds.center.x,
            defaultBounds.min.y - variantBounds.min.y,
            defaultBounds.center.z - variantBounds.center.z);
    }

    private bool GetHierarchyBoundsInPlayerSpace(Transform hierarchyRoot, out Bounds result)
    {
        result = default;

        if (hierarchyRoot == null)
            return false;

        Renderer[] hierarchyRenderers = hierarchyRoot.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;

        for (int i = 0; i < hierarchyRenderers.Length; i++)
        {
            Renderer targetRenderer = hierarchyRenderers[i];

            if (targetRenderer == null || targetRenderer is ParticleSystemRenderer)
                continue;

            Bounds worldBounds = targetRenderer.bounds;
            Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = new Vector3(
                worldBounds.size.x / Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.0001f),
                worldBounds.size.y / Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.0001f),
                worldBounds.size.z / Mathf.Max(Mathf.Abs(transform.lossyScale.z), 0.0001f));
            Bounds localBounds = new Bounds(localCenter, localSize);

            if (!hasBounds)
            {
                result = localBounds;
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(localBounds);
            }
        }

        return hasBounds;
    }

    private void RefreshVisualReferences()
    {
        visualModelRenderers = visualModelRoot != null
            ? visualModelRoot.GetComponentsInChildren<Renderer>(true)
            : Array.Empty<Renderer>();
        headTopBone = null;
        leftToeBone = null;
        rightToeBone = null;

        if (visualModelRoot == null)
            return;

        Transform[] modelBones = visualModelRoot.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < modelBones.Length; i++)
        {
            if (modelBones[i].name == "mixamorig:HeadTop_End") headTopBone = modelBones[i];
            else if (modelBones[i].name == "mixamorig:LeftToeBase") leftToeBone = modelBones[i];
            else if (modelBones[i].name == "mixamorig:RightToeBase") rightToeBone = modelBones[i];
        }

        if (IsUsingRedStaticVisual())
        {
            headTopBone = null;
            leftToeBone = null;
            rightToeBone = null;
        }
    }

    private bool IsUsingRedStaticVisual()
    {
        return redCharacterInstance != null &&
            redCharacterInstance.activeSelf &&
            redStaticVisualRoot != null &&
            redStaticVisualRoot.IsChildOf(redCharacterInstance.transform);
    }

    private void ApplyOwnerVisibility()
    {
        bool showWorldModel = true;

        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i] is ParticleSystemRenderer || IsClothingRenderer(renderers[i]))
                continue;

            renderers[i].enabled = showWorldModel;
        }

        bool isUsingCustomCharacter =
            (redCharacterInstance != null && redCharacterInstance.activeSelf) ||
            (blueCharacterInstance != null && blueCharacterInstance.activeSelf) ||
            (greenCharacterInstance != null && greenCharacterInstance.activeSelf) ||
            (purpleCharacterInstance != null && purpleCharacterInstance.activeSelf) ||
            (brownCharacterInstance != null && brownCharacterInstance.activeSelf) ||
            (yellowCharacterInstance != null && yellowCharacterInstance.activeSelf);
        bool usesClothingOverlay = appliedColorIndex != DefaultUntintedCharacterIndex;
        SetClothingOverlayVisible(showWorldModel && !isUsingCustomCharacter && usesClothingOverlay);
    }

    private bool IsClothingRenderer(Renderer targetRenderer)
    {
        if (targetRenderer == null || clothingRenderers == null)
            return false;

        for (int i = 0; i < clothingRenderers.Length; i++)
        {
            if (clothingRenderers[i] == targetRenderer)
                return true;
        }

        return false;
    }

    private void SetClothingOverlayVisible(bool visible)
    {
        if (clothingRenderers == null)
            return;

        for (int i = 0; i < clothingRenderers.Length; i++)
        {
            if (clothingRenderers[i] != null)
            {
                clothingRenderers[i].enabled = visible;
            }
        }
    }

    private void CreateClothingOverlay()
    {
        if (!useClothingOverlay || clothingRenderers != null)
            return;

        Transform existingOverlay = transform.Find("Network Shirt Color");

        if (existingOverlay != null)
        {
            Destroy(existingOverlay.gameObject);
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        clothingMaterial = new Material(shader);
        SetMaterialColor(clothingMaterial, ShirtColors[Mathf.Clamp(previewColorIndex, 0, ShirtColors.Length - 1)]);

        GameObject shirtRoot = new GameObject("Network Shirt Color");
        shirtRoot.transform.SetParent(transform, false);
        shirtRoot.transform.localPosition = Vector3.zero;
        shirtRoot.transform.localRotation = Quaternion.identity;
        shirtRoot.transform.localScale = Vector3.one;

        Bounds bodyBounds = GetBodyLocalBounds();
        Vector3 center = bodyBounds.center;
        float width = Mathf.Clamp(bodyBounds.size.x * 0.45f, 0.18f, 0.42f);
        float height = Mathf.Clamp(bodyBounds.size.y * 0.46f, 0.45f, 0.85f);
        float depth = Mathf.Clamp(bodyBounds.size.z * 0.5f, 0.08f, 0.18f);
        float sleeveWidth = Mathf.Clamp(width * 0.28f, 0.07f, 0.13f);
        float sleeveHeight = Mathf.Clamp(height * 0.48f, 0.22f, 0.38f);
        float top = center.y + height * 0.12f;

        Renderer front = CreateClothingPanel(
            "Shirt Front",
            shirtRoot.transform,
            new Vector3(center.x, top, center.z - depth),
            width,
            height,
            Quaternion.identity
        );

        Renderer back = CreateClothingPanel(
            "Shirt Back",
            shirtRoot.transform,
            new Vector3(center.x, top, center.z + depth),
            width,
            height,
            Quaternion.Euler(0f, 180f, 0f)
        );

        Renderer leftSleeve = CreateClothingPanel(
            "Left Sleeve",
            shirtRoot.transform,
            new Vector3(center.x - width * 0.68f, top + height * 0.08f, center.z),
            sleeveWidth,
            sleeveHeight,
            Quaternion.Euler(0f, 90f, 0f)
        );

        Renderer rightSleeve = CreateClothingPanel(
            "Right Sleeve",
            shirtRoot.transform,
            new Vector3(center.x + width * 0.68f, top + height * 0.08f, center.z),
            sleeveWidth,
            sleeveHeight,
            Quaternion.Euler(0f, -90f, 0f)
        );

        clothingRenderers = new[] { front, back, leftSleeve, rightSleeve };
    }

    private Bounds GetBodyLocalBounds()
    {
        Bounds result = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(0.6f, 1.8f, 0.35f));
        bool hasBounds = false;

        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i] is ParticleSystemRenderer || IsClothingRenderer(renderers[i]))
                continue;

            Bounds localBounds = new Bounds(
                transform.InverseTransformPoint(renderers[i].bounds.center),
                new Vector3(
                    renderers[i].bounds.size.x / Mathf.Max(transform.lossyScale.x, 0.0001f),
                    renderers[i].bounds.size.y / Mathf.Max(transform.lossyScale.y, 0.0001f),
                    renderers[i].bounds.size.z / Mathf.Max(transform.lossyScale.z, 0.0001f)
                )
            );

            if (!hasBounds)
            {
                result = localBounds;
                hasBounds = true;
            }
            else
            {
                result.Encapsulate(localBounds);
            }
        }

        return result;
    }

    private Renderer CreateClothingPanel(
        string pieceName,
        Transform parent,
        Vector3 localPosition,
        float width,
        float height,
        Quaternion localRotation)
    {
        GameObject piece = new GameObject(pieceName);
        piece.name = pieceName;
        piece.transform.SetParent(parent, false);
        piece.transform.localPosition = localPosition;
        piece.transform.localRotation = localRotation;
        piece.transform.localScale = Vector3.one;

        MeshFilter meshFilter = piece.AddComponent<MeshFilter>();
        MeshRenderer renderer = piece.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();
        float halfWidth = width * 0.5f;
        float halfHeight = height * 0.5f;

        mesh.vertices = new[]
        {
            new Vector3(-halfWidth, -halfHeight, 0f),
            new Vector3(halfWidth, -halfHeight, 0f),
            new Vector3(-halfWidth, halfHeight, 0f),
            new Vector3(halfWidth, halfHeight, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1, 1, 3, 0, 3, 2, 0 };
        mesh.RecalculateNormals();
        meshFilter.sharedMesh = mesh;

        renderer.sharedMaterial = clothingMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        return renderer;
    }

    private void ApplyClothingOverlayColor(Color color)
    {
        if (clothingRenderers == null || clothingRenderers.Length == 0)
        {
            CreateClothingOverlay();
        }

        if (clothingMaterial != null)
        {
            SetMaterialColor(clothingMaterial, color);
        }

        for (int i = 0; i < clothingRenderers.Length; i++)
        {
            if (clothingRenderers[i] == null)
                continue;

            Material material = clothingRenderers[i].material;
            SetMaterialColor(material, color);
        }
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void TintRenderer(Renderer targetRenderer, Color color)
    {
        Material[] materials = targetRenderer.materials;

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null && materials[i].HasProperty("_BaseColor"))
            {
                materials[i].SetColor("_BaseColor", color);
            }
            else if (materials[i] != null && materials[i].HasProperty("_Color"))
            {
                materials[i].SetColor("_Color", color);
            }
        }
    }

    private void BuildStyles()
    {
        if (whiteTexture == null)
        {
            whiteTexture = Texture2D.whiteTexture;
        }

        panelTexture ??= MakeRoundedRectTexture(96, 96, 14, panelColor);
        cardTexture ??= MakeRoundedRectTexture(64, 64, 8, new Color(0.15f, 0.23f, 0.22f, 1f));
        selectedCardTexture ??= MakeRoundedRectTexture(64, 64, 8, new Color(0.88f, 0.63f, 0.24f, 1f));
        buttonTexture ??= MakeRoundedRectTexture(64, 64, 7, new Color(0.20f, 0.52f, 0.43f, 1f));
        playButtonTexture ??= MakeRoundedRectTexture(64, 64, 8, new Color(0.88f, 0.63f, 0.24f, 1f));
        closeButtonTexture ??= MakeRoundedRectTexture(64, 64, 8, new Color(0.70f, 0.22f, 0.20f, 1f));
        inputTexture ??= MakeRoundedRectTexture(64, 64, 7, new Color(0.96f, 0.92f, 0.78f, 1f));
        blackTexture ??= MakeRoundedRectTexture(48, 48, 6, new Color(0.03f, 0.03f, 0.03f, 0.88f));
        titleFont ??= OnlineUIFont.CreateTitleFont();
        uiFont ??= OnlineUIFont.CreateUIFont();

        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = titleFont,
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(1f, 0.82f, 0.36f) }
        };

        panelStyle ??= new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(14, 14, 14, 14),
            normal = { background = panelTexture }
        };

        subtitleStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.86f, 0.91f, 0.82f) }
        };

        readyStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.95f, 0.84f, 0.46f) }
        };

        statusBadgeStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(10, 10, 4, 4),
            normal = { background = selectedCardTexture, textColor = new Color(0.12f, 0.09f, 0.04f) }
        };

        cardStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(5, 5, 5, 5),
            normal = { background = cardTexture, textColor = Color.white }
        };

        selectedCardStyle ??= new GUIStyle(cardStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { background = selectedCardTexture, textColor = Color.white }
        };

        smallButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            font = uiFont,
            border = new RectOffset(7, 7, 7, 7),
            fixedHeight = 26,
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { background = buttonTexture, textColor = Color.white },
            hover = { background = buttonTexture, textColor = new Color(1f, 0.92f, 0.70f) },
            active = { background = buttonTexture, textColor = Color.white }
        };

        playButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            font = uiFont,
            border = new RectOffset(8, 8, 8, 8),
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            normal = { background = playButtonTexture, textColor = new Color(0.12f, 0.09f, 0.04f) },
            hover = { background = playButtonTexture, textColor = Color.white },
            active = { background = playButtonTexture, textColor = new Color(1f, 0.92f, 0.70f) }
        };

        closeButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            font = uiFont,
            border = new RectOffset(8, 8, 8, 8),
            fontStyle = FontStyle.Bold,
            normal = { background = closeButtonTexture, textColor = Color.white },
            hover = { background = closeButtonTexture, textColor = new Color(1f, 0.92f, 0.70f) },
            active = { background = closeButtonTexture, textColor = Color.white }
        };

        inputStyle ??= new GUIStyle(GUI.skin.textField)
        {
            font = uiFont,
            alignment = TextAnchor.MiddleLeft,
            border = new RectOffset(7, 7, 7, 7),
            padding = new RectOffset(14, 14, 6, 6),
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { background = inputTexture, textColor = new Color(0.14f, 0.11f, 0.07f) },
            focused = { background = inputTexture, textColor = new Color(0.14f, 0.11f, 0.07f) }
        };

        inputTextStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(14, 14, 0, 0),
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip,
            normal = { textColor = new Color(0.08f, 0.07f, 0.05f) }
        };

        rosterStyle ??= new GUIStyle(GUI.skin.box)
        {
            border = new RectOffset(8, 8, 8, 8),
            padding = new RectOffset(12, 12, 12, 12),
            normal = { background = cardTexture, textColor = Color.white }
        };

        nameTagStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = uiFont,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(6, 6, 6, 6),
            padding = new RectOffset(10, 10, 4, 4),
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { background = blackTexture, textColor = Color.white }
        };
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();

        return texture;
    }

    private Texture2D MakeRoundedRectTexture(int width, int height, int radius, Color color)
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
}
