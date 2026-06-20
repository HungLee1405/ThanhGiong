using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkPlayerAppearance : NetworkBehaviour
{
    public static bool IsLocalSelectionOpen { get; private set; }

    private static readonly Color[] ShirtColors =
    {
        new Color(0.78f, 0.04f, 0.08f),
        new Color(0.06f, 0.18f, 0.78f),
        new Color(0.07f, 0.50f, 0.16f),
        new Color(0.93f, 0.22f, 0.55f),
        new Color(0.95f, 0.38f, 0.06f),
        new Color(0.96f, 0.76f, 0.08f)
    };

    private static readonly string[] CharacterNames =
    {
        "Red", "Blue", "Green", "Pink", "Orange", "Yellow"
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

    [Header("Character Select")]
    public bool showSelectionPanel = true;
    public bool tintSkinnedMeshOnly = true;
    public bool useClothingOverlay = true;

    private Renderer[] renderers;
    private Renderer[] clothingRenderers;
    private Material clothingMaterial;
    private int previewColorIndex;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle cardStyle;
    private GUIStyle selectedCardStyle;
    private GUIStyle readyStyle;
    private GUIStyle smallButtonStyle;
    private GUIStyle playButtonStyle;
    private GUIStyle closeButtonStyle;
    private Texture2D whiteTexture;
    private Texture2D panelTexture;
    private Texture2D cardTexture;
    private Texture2D selectedCardTexture;
    private Texture2D buttonTexture;
    private Texture2D playButtonTexture;
    private Texture2D closeButtonTexture;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        CreateClothingOverlay();
        previewColorIndex = Mathf.Clamp(colorIndex.Value, 0, ShirtColors.Length - 1);
    }

    public override void OnNetworkSpawn()
    {
        colorIndex.OnValueChanged += OnColorChanged;
        ready.OnValueChanged += OnReadyChanged;

        if (IsOwner)
        {
            previewColorIndex = (int)(OwnerClientId % (ulong)ShirtColors.Length);
            RequestColorServerRpc(previewColorIndex);
            RequestReadyServerRpc(false);
            showSelectionPanel = true;
        }

        ApplyColor(colorIndex.Value);
        ApplyOwnerVisibility();
    }

    public override void OnNetworkDespawn()
    {
        colorIndex.OnValueChanged -= OnColorChanged;
        ready.OnValueChanged -= OnReadyChanged;

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

        if (Keyboard.current != null)
        {
            if (showSelectionPanel && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseSelectionPanel(false);
            }
            else if (!showSelectionPanel && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                OpenSelectionPanel();
            }
        }

        IsLocalSelectionOpen = showSelectionPanel;
        SetCursorForSelection(showSelectionPanel);
    }

    private void LateUpdate()
    {
        if (!IsSpawned || !IsOwner)
            return;

        SetCursorForSelection(showSelectionPanel);
    }

    private void OnGUI()
    {
        if (!IsSpawned || !IsOwner)
            return;

        BuildStyles();
        IsLocalSelectionOpen = showSelectionPanel;
        SetCursorForSelection(showSelectionPanel);

        if (!showSelectionPanel)
        {
            DrawMiniButton();
            return;
        }

        DrawCharacterSelectPanel();
    }

    private void DrawCharacterSelectPanel()
    {
        float panelWidth = 560f;
        float panelHeight = 450f;
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

        Rect closeRect = new Rect(panelRect.xMax - 48f, panelRect.y + 14f, 32f, 32f);

        if (GUI.Button(closeRect, "X", closeButtonStyle))
        {
            CloseSelectionPanel(false);
        }

        GUILayout.BeginArea(new Rect(panelRect.x + 20f, panelRect.y + 16f, panelRect.width - 40f, panelRect.height - 32f));
        GUILayout.Label("Choose Character", titleStyle);
        GUILayout.Label("Pick your shirt color. Everyone shares the same quest.", subtitleStyle);
        GUILayout.Space(14f);

        for (int row = 0; row < 2; row++)
        {
            GUILayout.BeginHorizontal();

            for (int column = 0; column < 3; column++)
            {
                int index = row * 3 + column;
                DrawCharacterCard(index);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8f);
        }

        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.Label(ready.Value ? "Ready" : "Choosing", readyStyle, GUILayout.Width(160f));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Play", playButtonStyle, GUILayout.Width(150f), GUILayout.Height(46f)))
        {
            CloseSelectionPanel(true);
        }

        GUILayout.EndHorizontal();
        GUILayout.Label("Press Esc to close. Press Tab while playing to change character.", subtitleStyle);
        GUILayout.EndArea();
        GUI.color = oldColor;
    }

    private void DrawCharacterCard(int index)
    {
        bool selected = previewColorIndex == index;
        GUIStyle style = selected ? selectedCardStyle : cardStyle;

        GUILayout.BeginVertical(style, GUILayout.Width(160f), GUILayout.Height(132f));
        Rect previewRect = GUILayoutUtility.GetRect(118f, 78f);
        DrawCharacterPreview(previewRect, ShirtColors[index], selected);
        GUILayout.Label(CharacterNames[index], subtitleStyle);

        if (GUILayout.Button(selected ? "Selected" : "Select", smallButtonStyle))
        {
            previewColorIndex = index;
            RequestColorServerRpc(index);
            RequestReadyServerRpc(false);
        }

        GUILayout.EndVertical();
    }

    private void DrawCharacterPreview(Rect rect, Color color, bool selected)
    {
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

    private void DrawMiniButton()
    {
        Rect rect = new Rect(Screen.width - 220f, 16f, 200f, 42f);

        if (GUI.Button(rect, "Press Tab: Character"))
        {
            OpenSelectionPanel();
        }
    }

    private void SetCursorForSelection(bool isSelecting)
    {
        Cursor.lockState = isSelecting ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isSelecting;
    }

    private void OpenSelectionPanel()
    {
        showSelectionPanel = true;
        IsLocalSelectionOpen = true;
        SetCursorForSelection(true);
        RequestReadyServerRpc(false);
    }

    private void CloseSelectionPanel(bool markReady)
    {
        if (markReady)
        {
            RequestReadyServerRpc(true);
        }

        showSelectionPanel = false;
        IsLocalSelectionOpen = false;
        SetCursorForSelection(false);
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

    private void OnColorChanged(int previousValue, int newValue)
    {
        previewColorIndex = Mathf.Clamp(newValue, 0, ShirtColors.Length - 1);
        ApplyColor(newValue);
    }

    private void OnReadyChanged(bool previousValue, bool newValue)
    {
    }

    private void ApplyColor(int index)
    {
        Color color = ShirtColors[Mathf.Clamp(index, 0, ShirtColors.Length - 1)];

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

        SetClothingOverlayVisible(showWorldModel);
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

        panelTexture ??= MakeTexture(new Color(0.93f, 0.90f, 0.78f, 0.98f));
        cardTexture ??= MakeTexture(new Color(0.98f, 0.96f, 0.86f, 1f));
        selectedCardTexture ??= MakeTexture(new Color(0.83f, 0.95f, 0.80f, 1f));
        buttonTexture ??= MakeTexture(new Color(0.30f, 0.58f, 0.82f, 1f));
        playButtonTexture ??= MakeTexture(new Color(0.22f, 0.66f, 0.30f, 1f));
        closeButtonTexture ??= MakeTexture(new Color(0.82f, 0.22f, 0.18f, 1f));

        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.22f, 0.20f, 0.13f) }
        };

        subtitleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.25f, 0.25f, 0.20f) }
        };

        readyStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.16f, 0.42f, 0.18f) }
        };

        cardStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(8, 8, 8, 8),
            margin = new RectOffset(5, 5, 5, 5),
            normal = { background = cardTexture, textColor = new Color(0.20f, 0.20f, 0.18f) }
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
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();

        return texture;
    }
}
