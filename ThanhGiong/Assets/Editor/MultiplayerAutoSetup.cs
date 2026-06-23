using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MultiplayerAutoSetup
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string ResourcesPath = "Assets/Resources";
    private const string PrefabPath = "Assets/Resources/NetworkPlayer.prefab";
    private const string RedCharacterPath = "Assets/Characters/Variants/Do_Rigged.fbx";
    private const string RedMaterialPath = "Assets/Characters/Variants/Materials/Do.mat";
    private const string RedBaseTexturePath = "Assets/Characters/Variants/Textures/Do/texture_pbr_20250901.png";
    private const string RedNormalTexturePath = "Assets/Characters/Variants/Textures/Do/texture_pbr_20250901_normal.png";
    private const string GreenCharacterPath = "Assets/Characters/Variants/XanhLa_Rigged.fbx";
    private const string GreenMaterialPath = "Assets/Characters/Variants/Materials/XanhLa.mat";
    private const string GreenBaseTexturePath = "Assets/Characters/Variants/Textures/texture_pbr_20250901.png";
    private const string GreenNormalTexturePath = "Assets/Characters/Variants/Textures/texture_pbr_20250901_normal.png";
    private const string PurpleCharacterPath = "Assets/Characters/Variants/Tim_Rigged.fbx";
    private const string PurpleMaterialPath = "Assets/Characters/Variants/Materials/Tim.mat";
    private const string PurpleBaseTexturePath = "Assets/Characters/Variants/Textures/Tim/texture_pbr_20250901.png";
    private const string PurpleNormalTexturePath = "Assets/Characters/Variants/Textures/Tim/texture_pbr_20250901_normal.png";
    private const string BrownCharacterPath = "Assets/Characters/Variants/Nau_Rigged.fbx";
    private const string BrownMaterialPath = "Assets/Characters/Variants/Materials/Nau.mat";
    private const string BrownBaseTexturePath = "Assets/Characters/Variants/Textures/Nau/texture_pbr_20250901.png";
    private const string BrownNormalTexturePath = "Assets/Characters/Variants/Textures/Nau/texture_pbr_20250901_normal.png";
    private const string YellowCharacterPath = "Assets/Characters/Variants/Vang_Rigged.fbx";
    private const string YellowMaterialPath = "Assets/Characters/Variants/Materials/Vang.mat";
    private const string YellowBaseTexturePath = "Assets/Characters/Variants/Textures/Vang/texture_pbr_20250901.png";
    private const string YellowNormalTexturePath = "Assets/Characters/Variants/Textures/Vang/texture_pbr_20250901_normal.png";
    private const string MenuPath = "Thanh Giong/Setup Multiplayer";
    private const string AutoSetupSessionKey = "ThanhGiong.MultiplayerAutoSetup.DoneV3";

    static MultiplayerAutoSetup()
    {
        EditorApplication.delayCall += RunAutoSetupIfNeeded;
        EditorApplication.delayCall += EnsureGreenCharacterVariant;
    }

    private static void EnsureGreenCharacterVariant()
    {
        if (Application.isBatchMode)
            return;

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += EnsureGreenCharacterVariant;
            return;
        }

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Texture2D redBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RedBaseTexturePath);
        Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GreenBaseTexturePath);
        Texture2D purpleBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PurpleBaseTexturePath);
        Texture2D brownBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BrownBaseTexturePath);
        Texture2D yellowBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(YellowBaseTexturePath);

        if (playerPrefab == null || redBaseTexture == null || baseTexture == null || purpleBaseTexture == null ||
            brownBaseTexture == null || yellowBaseTexture == null)
            return;

        NetworkPlayerAppearance appearance = playerPrefab.GetComponent<NetworkPlayerAppearance>();
        SerializedObject serializedAppearance = appearance != null ? new SerializedObject(appearance) : null;
        Object assignedRedCharacter = serializedAppearance?.FindProperty("redCharacterPrefab")?.objectReferenceValue;
        Object assignedRedMaterial = serializedAppearance?.FindProperty("redCharacterMaterial")?.objectReferenceValue;
        Object assignedCharacter = serializedAppearance?.FindProperty("greenCharacterPrefab")?.objectReferenceValue;
        Object assignedMaterial = serializedAppearance?.FindProperty("greenCharacterMaterial")?.objectReferenceValue;
        Object assignedPurpleCharacter = serializedAppearance?.FindProperty("purpleCharacterPrefab")?.objectReferenceValue;
        Object assignedPurpleMaterial = serializedAppearance?.FindProperty("purpleCharacterMaterial")?.objectReferenceValue;
        Object assignedBrownCharacter = serializedAppearance?.FindProperty("brownCharacterPrefab")?.objectReferenceValue;
        Object assignedBrownMaterial = serializedAppearance?.FindProperty("brownCharacterMaterial")?.objectReferenceValue;
        Object assignedYellowCharacter = serializedAppearance?.FindProperty("yellowCharacterPrefab")?.objectReferenceValue;
        Object assignedYellowMaterial = serializedAppearance?.FindProperty("yellowCharacterMaterial")?.objectReferenceValue;

        if (assignedRedCharacter == null || assignedRedMaterial == null ||
            assignedCharacter == null || assignedMaterial == null ||
            assignedPurpleCharacter == null || assignedPurpleMaterial == null ||
            assignedBrownCharacter == null || assignedBrownMaterial == null ||
            assignedYellowCharacter == null || assignedYellowMaterial == null)
        {
            ConfigureGreenCharacterOnPrefab();
        }
    }

    private static void RunAutoSetupIfNeeded()
    {
        if (Application.isBatchMode)
            return;

        if (SessionState.GetBool(AutoSetupSessionKey, false))
            return;

        if (!NeedsSetup())
            return;

        SessionState.SetBool(AutoSetupSessionKey, true);
        Setup();
    }

    private static bool NeedsSetup()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            return true;

        if (!File.Exists(GameScenePath))
            return true;

        string sceneText = File.ReadAllText(GameScenePath);

        return !sceneText.Contains("OfflinePlayerDisabler") ||
            !sceneText.Contains("Assembly-CSharp::MultiplayerConnector") ||
            !sceneText.Contains("Unity.Netcode.Runtime::Unity.Netcode.NetworkManager") ||
            !File.ReadAllText(PrefabPath).Contains("Assembly-CSharp::NetworkPlayerAppearance");
    }

    [MenuItem(MenuPath)]
    public static void Setup()
    {
        EnsureFolders();

        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        GameObject scenePlayer = FindScenePlayer();

        if (scenePlayer == null)
        {
            Debug.LogError("Multiplayer setup failed: could not find a GameObject named Player with tag Player in GameScene.");
            return;
        }

        GameObject prefab = CreateOrUpdatePlayerPrefab(scenePlayer);
        EnsureComponent<OfflinePlayerDisabler>(scenePlayer);
        EnsureSceneNetworkManager(prefab);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Multiplayer setup complete. NetworkPlayer prefab and GameScene NetworkManager are ready.");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
        {
            AssetDatabase.CreateFolder("Assets", "Editor");
        }

        if (!AssetDatabase.IsValidFolder(ResourcesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }

    private static GameObject FindScenePlayer()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            GameObject player = FindTaggedPlayer(rootObjects[i].transform);

            if (player != null)
            {
                return player;
            }
        }

        return null;
    }

    private static GameObject FindTaggedPlayer(Transform root)
    {
        if (root.CompareTag("Player") && root.name == "Player")
        {
            return root.gameObject;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            GameObject player = FindTaggedPlayer(root.GetChild(i));

            if (player != null)
            {
                return player;
            }
        }

        return null;
    }

    private static GameObject CreateOrUpdatePlayerPrefab(GameObject scenePlayer)
    {
        GameObject workingCopy = Object.Instantiate(scenePlayer);
        workingCopy.name = "NetworkPlayer";

        EnsureComponent<NetworkObject>(workingCopy);
        NetworkTransform networkTransform = EnsureComponent<NetworkTransform>(workingCopy);
        networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        EnsureComponent<NetworkLocalPlayerSetup>(workingCopy);
        NetworkPlayerAppearance appearance = EnsureComponent<NetworkPlayerAppearance>(workingCopy);
        AssignCharacterVariants(appearance);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(workingCopy, PrefabPath);
        Object.DestroyImmediate(workingCopy);

        return prefab;
    }

    private static void AssignCharacterVariants(NetworkPlayerAppearance appearance)
    {
        GameObject redCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(RedCharacterPath);
        GameObject greenCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(GreenCharacterPath);
        GameObject purpleCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(PurpleCharacterPath);
        GameObject brownCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(BrownCharacterPath);
        GameObject yellowCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(YellowCharacterPath);

        if (appearance == null || redCharacter == null || greenCharacter == null || purpleCharacter == null ||
            brownCharacter == null || yellowCharacter == null)
            return;

        SerializedObject serializedAppearance = new SerializedObject(appearance);
        SerializedProperty redCharacterProperty = serializedAppearance.FindProperty("redCharacterPrefab");
        SerializedProperty redMaterialProperty = serializedAppearance.FindProperty("redCharacterMaterial");
        SerializedProperty greenCharacterProperty = serializedAppearance.FindProperty("greenCharacterPrefab");
        SerializedProperty greenMaterialProperty = serializedAppearance.FindProperty("greenCharacterMaterial");
        SerializedProperty purpleCharacterProperty = serializedAppearance.FindProperty("purpleCharacterPrefab");
        SerializedProperty purpleMaterialProperty = serializedAppearance.FindProperty("purpleCharacterMaterial");
        SerializedProperty brownCharacterProperty = serializedAppearance.FindProperty("brownCharacterPrefab");
        SerializedProperty brownMaterialProperty = serializedAppearance.FindProperty("brownCharacterMaterial");
        SerializedProperty yellowCharacterProperty = serializedAppearance.FindProperty("yellowCharacterPrefab");
        SerializedProperty yellowMaterialProperty = serializedAppearance.FindProperty("yellowCharacterMaterial");

        if (redCharacterProperty == null || redMaterialProperty == null ||
            greenCharacterProperty == null || greenMaterialProperty == null ||
            purpleCharacterProperty == null || purpleMaterialProperty == null ||
            brownCharacterProperty == null || brownMaterialProperty == null ||
            yellowCharacterProperty == null || yellowMaterialProperty == null)
            return;

        redCharacterProperty.objectReferenceValue = redCharacter;
        redMaterialProperty.objectReferenceValue = CreateOrUpdateCharacterMaterial(
            RedMaterialPath,
            RedBaseTexturePath,
            RedNormalTexturePath);
        greenCharacterProperty.objectReferenceValue = greenCharacter;
        greenMaterialProperty.objectReferenceValue = CreateOrUpdateCharacterMaterial(
            GreenMaterialPath,
            GreenBaseTexturePath,
            GreenNormalTexturePath);
        purpleCharacterProperty.objectReferenceValue = purpleCharacter;
        purpleMaterialProperty.objectReferenceValue = CreateOrUpdateCharacterMaterial(
            PurpleMaterialPath,
            PurpleBaseTexturePath,
            PurpleNormalTexturePath);
        brownCharacterProperty.objectReferenceValue = brownCharacter;
        brownMaterialProperty.objectReferenceValue = CreateOrUpdateCharacterMaterial(
            BrownMaterialPath,
            BrownBaseTexturePath,
            BrownNormalTexturePath);
        yellowCharacterProperty.objectReferenceValue = yellowCharacter;
        yellowMaterialProperty.objectReferenceValue = CreateOrUpdateCharacterMaterial(
            YellowMaterialPath,
            YellowBaseTexturePath,
            YellowNormalTexturePath);
        serializedAppearance.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material CreateOrUpdateCharacterMaterial(
        string materialPath,
        string baseTexturePath,
        string normalTexturePath)
    {
        string materialDirectory = Path.GetDirectoryName(materialPath)?.Replace('\\', '/');

        if (!string.IsNullOrEmpty(materialDirectory) && !AssetDatabase.IsValidFolder(materialDirectory))
        {
            Directory.CreateDirectory(materialDirectory);
            AssetDatabase.Refresh();
        }

        TextureImporter normalImporter = AssetImporter.GetAtPath(normalTexturePath) as TextureImporter;

        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(baseTexturePath);
        Texture2D normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexturePath);

        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", baseTexture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", baseTexture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);

        if (normalTexture != null && material.HasProperty("_BumpMap"))
        {
            material.SetTexture("_BumpMap", normalTexture);
            material.EnableKeyword("_NORMALMAP");
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    public static void ConfigureGreenCharacterOnPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);

        try
        {
            NetworkPlayerAppearance appearance = prefabRoot.GetComponent<NetworkPlayerAppearance>();
            AssignCharacterVariants(appearance);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        ValidateGreenCharacterSetup();
    }

    public static void ValidateGreenCharacterSetup()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject redCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(RedCharacterPath);
        GameObject greenCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(GreenCharacterPath);
        GameObject purpleCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(PurpleCharacterPath);
        GameObject brownCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(BrownCharacterPath);
        GameObject yellowCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(YellowCharacterPath);
        NetworkPlayerAppearance appearance = playerPrefab != null
            ? playerPrefab.GetComponent<NetworkPlayerAppearance>()
            : null;

        if (appearance == null || redCharacter == null || greenCharacter == null || purpleCharacter == null ||
            brownCharacter == null || yellowCharacter == null)
            throw new System.InvalidOperationException("Character variants are missing their player prefab or model assets.");

        SerializedObject serializedAppearance = new SerializedObject(appearance);
        ValidateCharacterVariant(serializedAppearance, "Red", redCharacter, "redCharacterPrefab", "redCharacterMaterial");
        ValidateCharacterVariant(serializedAppearance, "Green", greenCharacter, "greenCharacterPrefab", "greenCharacterMaterial");
        ValidateCharacterVariant(serializedAppearance, "Purple", purpleCharacter, "purpleCharacterPrefab", "purpleCharacterMaterial");
        ValidateCharacterVariant(serializedAppearance, "Brown", brownCharacter, "brownCharacterPrefab", "brownCharacterMaterial");
        ValidateCharacterVariant(serializedAppearance, "Yellow", yellowCharacter, "yellowCharacterPrefab", "yellowCharacterMaterial");
    }

    private static void ValidateCharacterVariant(
        SerializedObject serializedAppearance,
        string variantName,
        GameObject character,
        string characterPropertyName,
        string materialPropertyName)
    {
        Object assignedCharacter = serializedAppearance.FindProperty(characterPropertyName)?.objectReferenceValue;
        Material assignedMaterial = serializedAppearance.FindProperty(materialPropertyName)?.objectReferenceValue as Material;
        SkinnedMeshRenderer[] skinnedMeshes = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        int boneCount = skinnedMeshes.Length > 0 ? skinnedMeshes[0].bones.Length : 0;

        if (assignedCharacter != character || assignedMaterial == null || assignedMaterial.mainTexture == null ||
            skinnedMeshes.Length == 0 || boneCount == 0)
        {
            throw new System.InvalidOperationException(
                $"{variantName} character validation failed. Assigned: {assignedCharacter == character}, " +
                $"material: {assignedMaterial != null}, texture: {assignedMaterial?.mainTexture != null}, " +
                $"skinned meshes: {skinnedMeshes.Length}, bones: {boneCount}.");
        }

        Debug.Log($"{variantName} character validated: textured material, {skinnedMeshes.Length} skinned mesh, {boneCount} bones.");
    }

    private static void EnsureSceneNetworkManager(GameObject playerPrefab)
    {
        NetworkManager manager = Object.FindFirstObjectByType<NetworkManager>();

        if (manager == null)
        {
            GameObject managerObject = new GameObject("Network Manager");
            manager = managerObject.AddComponent<NetworkManager>();
        }

        UnityTransport transport = manager.GetComponent<UnityTransport>();

        if (transport == null)
        {
            transport = manager.gameObject.AddComponent<UnityTransport>();
        }

        SharedQuestNetwork.EnsureExists(manager.gameObject);
        manager.NetworkConfig.NetworkTransport = transport;
        manager.NetworkConfig.PlayerPrefab = playerPrefab;

        if (!manager.NetworkConfig.Prefabs.Contains(playerPrefab))
        {
            manager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = playerPrefab });
        }

        MultiplayerConnector connector = Object.FindFirstObjectByType<MultiplayerConnector>();

        if (connector == null)
        {
            connector = manager.gameObject.AddComponent<MultiplayerConnector>();
        }

        connector.playerPrefab = playerPrefab;
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(connector);
        EditorUtility.SetDirty(manager.gameObject);
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();

        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}
