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
        "Red", "Blue", "Green", "Purple", "Brown", "Yellow"
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
    [SerializeField] private float visualGroundSink = 0.18f;
    [SerializeField] private float footBoneGroundClearance = 0.035f;
    [SerializeField] private float nameTagClearance = 0.14f;
    [SerializeField] private GameObject redCharacterPrefab;
    [SerializeField] private Material redCharacterMaterial;
    [SerializeField] private GameObject greenCharacterPrefab;
    [SerializeField] private Material greenCharacterMaterial;
    [SerializeField] private GameObject purpleCharacterPrefab;
    [SerializeField] private Material purpleCharacterMaterial;
    [SerializeField] private GameObject brownCharacterPrefab;
    [SerializeField] private Material brownCharacterMaterial;
    [SerializeField] private GameObject yellowCharacterPrefab;
    [SerializeField] private Material yellowCharacterMaterial;

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
    private GUIStyle cardStyle;
    private GUIStyle selectedCardStyle;
    private GUIStyle readyStyle;
    private GUIStyle smallButtonStyle;
    private GUIStyle playButtonStyle;
    private GUIStyle closeButtonStyle;
    private GUIStyle inputStyle;
    private GUIStyle rosterStyle;
    private GUIStyle nameTagStyle;
    private GUIStyle inputTextStyle;
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

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        defaultVisualModelRoot = transform.Find("Idle");
        visualModelRoot = defaultVisualModelRoot;
        RefreshVisualReferences();
        LoadCharacterPortraits();

        CreateClothingOverlay();
        previewColorIndex = Mathf.Clamp(colorIndex.Value, 0, ShirtColors.Length - 1);
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
            localNameInput = "Player " + (OwnerClientId + 1);
            nameInputFocused = true;
            RequestColorServerRpc(previewColorIndex);
            RequestNameServerRpc(localNameInput);
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
        if (!IsSpawned || !IsOwner)
            return;

        SetCursorForSelection(NetworkLobbyCoordinator.IsOnlineLobbyActive);
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
        float panelWidth = Mathf.Min(940f, Screen.width - 24f);
        float panelHeight = Mathf.Min(650f, Screen.height - 24f);
        Rect panelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight
        );

        Color oldColor = GUI.color;
        GUI.color = new Color(0.73f, 0.86f, 0.94f, 0.35f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture);
        GUI.color = Color.white;
        GUI.DrawTexture(panelRect, panelTexture);

        GUILayout.BeginArea(new Rect(panelRect.x + 20f, panelRect.y + 16f, panelRect.width - 40f, panelRect.height - 32f));
        GUILayout.Label("ONLINE LOBBY", titleStyle);
        GUILayout.Label("ROOM CODE: " + MultiplayerConnector.ActiveRoomCode, readyStyle);
        GUILayout.Label("Choose your color, enter your name, then ready up.", subtitleStyle);
        GUILayout.Space(12f);
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width((panelRect.width - 70f) * 0.64f));

        GUILayout.Label("PLAYER NAME", readyStyle);
        GUI.enabled = !ready.Value;
        DrawPlayerNameInput();

        GUILayout.Space(10f);
        GUILayout.Label("CHARACTER COLOR", readyStyle);

        float cardWidth = Mathf.Max(118f, ((panelRect.width - 70f) * 0.64f - 34f) / 3f);

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

        GUI.enabled = true;
        GUILayout.Space(10f);
        string validPlayerName = SanitizePlayerName(localNameInput);
        GUI.enabled = !string.IsNullOrWhiteSpace(validPlayerName);
        string readyButtonText = ready.Value ? "NOT READY" : "READY";

        if (GUILayout.Button(readyButtonText, ready.Value ? closeButtonStyle : playButtonStyle, GUILayout.Height(48f)))
        {
            bool nextReadyState = !ready.Value;

            if (nextReadyState)
            {
                localNameInput = validPlayerName;
                RequestNameServerRpc(localNameInput);
            }
            else
            {
                nameInputFocused = true;
            }

            RequestReadyServerRpc(nextReadyState);
        }

        GUI.enabled = true;
        GUILayout.EndVertical();
        GUILayout.Space(18f);
        DrawPlayerRoster();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        GUI.color = oldColor;
    }

    private void DrawCharacterCard(int index, float width)
    {
        bool selected = previewColorIndex == index;
        GUIStyle style = selected ? selectedCardStyle : cardStyle;

        GUILayout.BeginVertical(style, GUILayout.Width(width), GUILayout.Height(132f));
        Rect previewRect = GUILayoutUtility.GetRect(width - 22f, 76f);
        DrawCharacterPreview(previewRect, index, ShirtColors[index], selected);
        GUILayout.Label(CharacterNames[index], subtitleStyle);

        if (GUILayout.Button(selected ? "Selected" : "Select", smallButtonStyle))
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
        GUILayout.Label("PLAYERS", readyStyle);
        GUILayout.Space(6f);

        NetworkPlayerAppearance[] players = FindObjectsByType<NetworkPlayerAppearance>(FindObjectsSortMode.None);
        Array.Sort(players, (left, right) => left.OwnerClientId.CompareTo(right.OwnerClientId));

        for (int i = 0; i < players.Length; i++)
        {
            if (!players[i].IsSpawned)
                continue;

            GUILayout.BeginHorizontal(cardStyle, GUILayout.Height(44f));
            Color oldColor = GUI.color;
            GUI.color = ShirtColors[Mathf.Clamp(players[i].colorIndex.Value, 0, ShirtColors.Length - 1)];
            GUILayout.Box(GUIContent.none, GUILayout.Width(24f), GUILayout.Height(24f));
            GUI.color = oldColor;

            string displayName = players[i].playerName.Value.IsEmpty
                ? "Choosing name..."
                : players[i].playerName.Value.ToString();
            string hostMarker = players[i].OwnerClientId == NetworkManager.ServerClientId ? " (HOST)" : "";
            GUILayout.Label(displayName + hostMarker, subtitleStyle, GUILayout.ExpandWidth(true));
            GUILayout.Label(players[i].ready.Value ? "READY" : "...", players[i].ready.Value ? readyStyle : subtitleStyle, GUILayout.Width(62f));
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

            if (GUILayout.Button("PLAY", playButtonStyle, GUILayout.Height(52f)))
            {
                NetworkLobbyCoordinator.TryStartMatch();
            }

            GUI.enabled = true;
        }
        else
        {
            GUILayout.Label("WAITING FOR HOST", subtitleStyle);
        }

        GUILayout.Space(8f);

        if (GUILayout.Button("LEAVE ROOM", closeButtonStyle, GUILayout.Height(40f)))
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
        yield return new WaitForEndOfFrame();

        for (int attempt = 0; attempt < 4; attempt++)
        {
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

        Vector3 rayOrigin = transform.position + Vector3.up * 20f;

        if (!Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            50f,
            1 << 3,
            QueryTriggerInteraction.Ignore))
        {
            return;
        }

        float visualFootY = modelBounds.min.y;
        float desiredFootY = hit.point.y - visualGroundSink;

        if (leftToeBone != null || rightToeBone != null)
        {
            visualFootY = float.PositiveInfinity;

            if (leftToeBone != null) visualFootY = Mathf.Min(visualFootY, leftToeBone.position.y);
            if (rightToeBone != null) visualFootY = Mathf.Min(visualFootY, rightToeBone.position.y);

            desiredFootY = hit.point.y + footBoneGroundClearance;
        }

        float worldCorrection = desiredFootY - visualFootY;

        if (Mathf.Abs(worldCorrection) > 2f)
            return;

        Transform parent = visualModelRoot.parent;
        float parentScaleY = parent != null ? Mathf.Abs(parent.lossyScale.y) : 1f;
        parentScaleY = Mathf.Max(parentScaleY, 0.0001f);
        visualModelRoot.localPosition += Vector3.up * (worldCorrection / parentScaleY);
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

            if (targetRenderer == null || targetRenderer is ParticleSystemRenderer)
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

    [ServerRpc]
    private void RequestColorServerRpc(int requestedColor)
    {
        colorIndex.Value = Mathf.Clamp(requestedColor, 0, ShirtColors.Length - 1);
    }

    [ServerRpc]
    private void RequestReadyServerRpc(bool isReady)
    {
        ready.Value = isReady;
    }

    [ServerRpc]
    private void RequestNameServerRpc(string requestedName)
    {
        playerName.Value = new FixedString64Bytes(SanitizePlayerName(requestedName));
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
        bool useGreenCharacter = index == GreenCharacterIndex && greenCharacterPrefab != null;
        bool usePurpleCharacter = index == PurpleCharacterIndex && purpleCharacterPrefab != null;
        bool useBrownCharacter = index == BrownCharacterIndex && brownCharacterPrefab != null;
        bool useYellowCharacter = index == YellowCharacterIndex && yellowCharacterPrefab != null;

        if (useRedCharacter)
        {
            EnsureRedCharacterInstance();
            useRedCharacter = redCharacterInstance != null;
        }

        if (useGreenCharacter)
        {
            EnsureGreenCharacterInstance();
            useGreenCharacter = greenCharacterInstance != null;
        }

        if (usePurpleCharacter)
        {
            EnsurePurpleCharacterInstance();
            usePurpleCharacter = purpleCharacterInstance != null;
        }

        if (useBrownCharacter)
        {
            EnsureBrownCharacterInstance();
            useBrownCharacter = brownCharacterInstance != null;
        }

        if (useYellowCharacter)
        {
            EnsureYellowCharacterInstance();
            useYellowCharacter = yellowCharacterInstance != null;
        }

        bool useCustomCharacter =
            useRedCharacter || useGreenCharacter || usePurpleCharacter ||
            useBrownCharacter || useYellowCharacter;

        if (defaultVisualModelRoot != null)
        {
            defaultVisualModelRoot.gameObject.SetActive(!useCustomCharacter);
        }

        if (redCharacterInstance != null)
        {
            redCharacterInstance.SetActive(useRedCharacter);
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
            : useGreenCharacter
                ? greenCharacterInstance.transform
                : usePurpleCharacter
                    ? purpleCharacterInstance.transform
                    : useBrownCharacter
                        ? brownCharacterInstance.transform
                        : useYellowCharacter
                            ? yellowCharacterInstance.transform
                            : defaultVisualModelRoot;
        RefreshVisualReferences();
        renderers = GetComponentsInChildren<Renderer>(true);
        SetClothingOverlayVisible(
            !useCustomCharacter &&
            index != DefaultUntintedCharacterIndex &&
            (!IsSpawned || !IsOwner));

        return useCustomCharacter;
    }

    private void EnsureRedCharacterInstance()
    {
        if (redCharacterInstance != null || redCharacterPrefab == null)
            return;

        redCharacterInstance = CreateCharacterInstance(
            redCharacterPrefab,
            redCharacterMaterial,
            "Red Character Model");
    }

    private void EnsureGreenCharacterInstance()
    {
        if (greenCharacterInstance != null || greenCharacterPrefab == null)
            return;

        greenCharacterInstance = CreateCharacterInstance(
            greenCharacterPrefab,
            greenCharacterMaterial,
            "Green Character Model");
    }

    private void EnsureYellowCharacterInstance()
    {
        if (yellowCharacterInstance != null || yellowCharacterPrefab == null)
            return;

        yellowCharacterInstance = CreateCharacterInstance(
            yellowCharacterPrefab,
            yellowCharacterMaterial,
            "Yellow Character Model");
    }

    private void EnsureBrownCharacterInstance()
    {
        if (brownCharacterInstance != null || brownCharacterPrefab == null)
            return;

        brownCharacterInstance = CreateCharacterInstance(
            brownCharacterPrefab,
            brownCharacterMaterial,
            "Brown Character Model");
    }

    private void EnsurePurpleCharacterInstance()
    {
        if (purpleCharacterInstance != null || purpleCharacterPrefab == null)
            return;

        purpleCharacterInstance = CreateCharacterInstance(
            purpleCharacterPrefab,
            purpleCharacterMaterial,
            "Purple Character Model");
    }

    private GameObject CreateCharacterInstance(GameObject characterPrefab, Material characterMaterial, string instanceName)
    {
        GameObject instance = Instantiate(characterPrefab, transform);
        instance.name = instanceName;
        Transform instanceTransform = instance.transform;
        instanceTransform.localPosition = Vector3.zero;
        instanceTransform.localRotation = Quaternion.identity;
        instanceTransform.localScale = Vector3.one;

        ApplyCharacterMaterial(instanceTransform, characterMaterial);
        CopyDefaultPoseToVariant(instanceTransform);
        MatchVariantToDefaultModel(instanceTransform);

        return instance;
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
    }

    private void ApplyOwnerVisibility()
    {
        if (!IsSpawned)
            return;

        bool showWorldModel = !IsOwner;

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

        panelTexture ??= MakeTexture(new Color(0.18f, 0.23f, 0.14f, 0.98f));
        cardTexture ??= MakeTexture(new Color(0.25f, 0.28f, 0.19f, 1f));
        selectedCardTexture ??= MakeTexture(new Color(0.72f, 0.55f, 0.22f, 1f));
        buttonTexture ??= MakeTexture(new Color(0.29f, 0.48f, 0.24f, 1f));
        playButtonTexture ??= MakeTexture(new Color(0.82f, 0.61f, 0.23f, 1f));
        closeButtonTexture ??= MakeTexture(new Color(0.82f, 0.22f, 0.18f, 1f));
        inputTexture ??= MakeTexture(new Color(0.96f, 0.90f, 0.73f, 1f));
        blackTexture ??= MakeTexture(new Color(0.03f, 0.03f, 0.03f, 0.88f));

        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.84f, 0.42f) }
        };

        subtitleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.95f, 0.91f, 0.78f) }
        };

        readyStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.55f, 0.85f, 0.45f) }
        };

        cardStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(8, 8, 8, 8),
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
            fixedHeight = 26,
            fontStyle = FontStyle.Bold,
            normal = { background = buttonTexture, textColor = Color.white }
        };

        playButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            normal = { background = playButtonTexture, textColor = Color.white }
        };

        closeButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            normal = { background = closeButtonTexture, textColor = Color.white }
        };

        inputStyle ??= new GUIStyle(GUI.skin.textField)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(14, 14, 6, 6),
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { background = inputTexture, textColor = new Color(0.14f, 0.11f, 0.07f) },
            focused = { background = inputTexture, textColor = new Color(0.14f, 0.11f, 0.07f) }
        };

        inputTextStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(14, 14, 0, 0),
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip,
            normal = { textColor = new Color(0.08f, 0.07f, 0.05f) }
        };

        rosterStyle ??= new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(12, 12, 12, 12),
            normal = { background = cardTexture, textColor = Color.white }
        };

        nameTagStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(10, 10, 4, 4),
            fontSize = 15,
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
}
