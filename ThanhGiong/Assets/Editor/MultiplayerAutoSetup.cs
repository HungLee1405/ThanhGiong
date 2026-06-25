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
    private const string FootstepClipPath = "Assets/Assets/Sound/footstep_walk.wav";
    private const string PlayerAnimatorControllerPath = "Assets/Assets/model_3D/animation/PlayerAnimator.controller";
    private const string RedCharacterPath = "Assets/Characters/Variants/XanhLa_Rigged.fbx";
    private const bool RedCharacterUsesStaticMesh = false;
    private const string RedStaticCharacterResourcePath = "Characters/Do";
    private static readonly Vector3 RedStaticCharacterEulerOffset = Vector3.zero;
    private const float RedStaticCharacterGroundLift = 0.04f;
    private const string RedMaterialPath = "Assets/Characters/Variants/Materials/Do.mat";
    private const string RedBaseTexturePath = "Assets/Characters/Variants/Textures/MultiplayerRed/texture_pbr_20250901.png";
    private const string RedNormalTexturePath = "Assets/Characters/Variants/Textures/texture_pbr_20250901_normal.png";
    private const string BlueCharacterPath = "Assets/Characters/Variants/XanhLa_Rigged.fbx";
    private const string BlueMaterialPath = "Assets/Characters/Variants/Materials/XanhDuong.mat";
    private const string BlueBaseTexturePath = "Assets/Characters/Variants/Textures/MultiplayerBlue/texture_pbr_20250901.png";
    private const string BlueNormalTexturePath = "Assets/Characters/Variants/Textures/texture_pbr_20250901_normal.png";
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
        GameObject expectedRedCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(RedCharacterPath);
        GameObject expectedBlueCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(BlueCharacterPath);
        RuntimeAnimatorController expectedAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorControllerPath);
        Texture2D redBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RedBaseTexturePath);
        Texture2D blueBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BlueBaseTexturePath);
        Texture2D baseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(GreenBaseTexturePath);
        Texture2D purpleBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PurpleBaseTexturePath);
        Texture2D brownBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BrownBaseTexturePath);
        Texture2D yellowBaseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(YellowBaseTexturePath);

        if (playerPrefab == null || expectedRedCharacter == null || expectedBlueCharacter == null ||
            expectedAnimatorController == null || redBaseTexture == null || blueBaseTexture == null ||
            baseTexture == null || purpleBaseTexture == null ||
            brownBaseTexture == null || yellowBaseTexture == null)
            return;

        NetworkPlayerAppearance appearance = playerPrefab.GetComponent<NetworkPlayerAppearance>();
        SerializedObject serializedAppearance = appearance != null ? new SerializedObject(appearance) : null;
        Object assignedRedCharacter = serializedAppearance?.FindProperty("redCharacterPrefab")?.objectReferenceValue;
        bool assignedRedStaticMeshMode = serializedAppearance?.FindProperty("buildRedCharacterFromStaticMesh")?.boolValue ?? false;
        string assignedRedStaticResourcePath = serializedAppearance?.FindProperty("redStaticCharacterResourcePath")?.stringValue;
        Vector3 assignedRedStaticEulerOffset =
            serializedAppearance?.FindProperty("redStaticCharacterEulerOffset")?.vector3Value ?? Vector3.zero;
        float assignedRedStaticGroundLift =
            serializedAppearance?.FindProperty("redStaticCharacterGroundLift")?.floatValue ?? 0f;
        Object assignedAnimatorController = serializedAppearance?.FindProperty("characterAnimatorController")?.objectReferenceValue;
        Object assignedRedMaterial = serializedAppearance?.FindProperty("redCharacterMaterial")?.objectReferenceValue;
        Object assignedRedAvatar = serializedAppearance?.FindProperty("redCharacterAvatar")?.objectReferenceValue;
        Object assignedBlueCharacter = serializedAppearance?.FindProperty("blueCharacterPrefab")?.objectReferenceValue;
        Object assignedBlueMaterial = serializedAppearance?.FindProperty("blueCharacterMaterial")?.objectReferenceValue;
        Object assignedBlueAvatar = serializedAppearance?.FindProperty("blueCharacterAvatar")?.objectReferenceValue;
        Object assignedCharacter = serializedAppearance?.FindProperty("greenCharacterPrefab")?.objectReferenceValue;
        Object assignedMaterial = serializedAppearance?.FindProperty("greenCharacterMaterial")?.objectReferenceValue;
        Object assignedGreenAvatar = serializedAppearance?.FindProperty("greenCharacterAvatar")?.objectReferenceValue;
        Object assignedPurpleCharacter = serializedAppearance?.FindProperty("purpleCharacterPrefab")?.objectReferenceValue;
        Object assignedPurpleMaterial = serializedAppearance?.FindProperty("purpleCharacterMaterial")?.objectReferenceValue;
        Object assignedPurpleAvatar = serializedAppearance?.FindProperty("purpleCharacterAvatar")?.objectReferenceValue;
        Object assignedBrownCharacter = serializedAppearance?.FindProperty("brownCharacterPrefab")?.objectReferenceValue;
        Object assignedBrownMaterial = serializedAppearance?.FindProperty("brownCharacterMaterial")?.objectReferenceValue;
        Object assignedBrownAvatar = serializedAppearance?.FindProperty("brownCharacterAvatar")?.objectReferenceValue;
        Object assignedYellowCharacter = serializedAppearance?.FindProperty("yellowCharacterPrefab")?.objectReferenceValue;
        Object assignedYellowMaterial = serializedAppearance?.FindProperty("yellowCharacterMaterial")?.objectReferenceValue;
        Object assignedYellowAvatar = serializedAppearance?.FindProperty("yellowCharacterAvatar")?.objectReferenceValue;

        if (assignedAnimatorController != expectedAnimatorController ||
            assignedRedStaticMeshMode != RedCharacterUsesStaticMesh ||
            assignedRedStaticResourcePath != RedStaticCharacterResourcePath ||
            assignedRedStaticEulerOffset != RedStaticCharacterEulerOffset ||
            !Mathf.Approximately(assignedRedStaticGroundLift, RedStaticCharacterGroundLift) ||
            assignedRedCharacter != expectedRedCharacter || assignedRedMaterial == null || assignedRedAvatar == null ||
            assignedBlueCharacter != expectedBlueCharacter || assignedBlueMaterial == null || assignedBlueAvatar == null ||
            assignedCharacter == null || assignedMaterial == null || assignedGreenAvatar == null ||
            assignedPurpleCharacter == null || assignedPurpleMaterial == null || assignedPurpleAvatar == null ||
            assignedBrownCharacter == null || assignedBrownMaterial == null || assignedBrownAvatar == null ||
            assignedYellowCharacter == null || assignedYellowMaterial == null || assignedYellowAvatar == null)
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
        RemoveOfflineNetworkComponents(scenePlayer);
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
        AssignPlayerAudio(workingCopy);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(workingCopy, PrefabPath);
        Object.DestroyImmediate(workingCopy);

        return prefab;
    }

    private static void RemoveOfflineNetworkComponents(GameObject scenePlayer)
    {
        if (scenePlayer == null) return;

        NetworkTransform networkTransform = scenePlayer.GetComponent<NetworkTransform>();
        if (networkTransform != null) Object.DestroyImmediate(networkTransform);

        NetworkObject networkObject = scenePlayer.GetComponent<NetworkObject>();
        if (networkObject != null) Object.DestroyImmediate(networkObject);
    }

    private static void AssignPlayerAudio(GameObject player)
    {
        PlayerMovement movement = player != null ? player.GetComponent<PlayerMovement>() : null;
        AudioClip footstepClip = AssetDatabase.LoadAssetAtPath<AudioClip>(FootstepClipPath);

        if (movement == null || footstepClip == null)
            return;

        SerializedObject serializedMovement = new SerializedObject(movement);
        SerializedProperty clipProperty = serializedMovement.FindProperty("footstepClip");

        if (clipProperty != null)
        {
            clipProperty.objectReferenceValue = footstepClip;
            serializedMovement.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void AssignCharacterVariants(NetworkPlayerAppearance appearance)
    {
        GameObject redCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(RedCharacterPath);
        RuntimeAnimatorController animatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerAnimatorControllerPath);
        GameObject blueCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(BlueCharacterPath);
        GameObject greenCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(GreenCharacterPath);
        GameObject purpleCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(PurpleCharacterPath);
        GameObject brownCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(BrownCharacterPath);
        GameObject yellowCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(YellowCharacterPath);
        Avatar redAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(RedCharacterPath);
        Avatar blueAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(BlueCharacterPath);
        Avatar greenAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(GreenCharacterPath);
        Avatar purpleAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(PurpleCharacterPath);
        Avatar brownAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(BrownCharacterPath);
        Avatar yellowAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(YellowCharacterPath);

        if (appearance == null || animatorController == null ||
            redCharacter == null || blueCharacter == null || greenCharacter == null || purpleCharacter == null ||
            brownCharacter == null || yellowCharacter == null || redAvatar == null || blueAvatar == null ||
            greenAvatar == null ||
            purpleAvatar == null || brownAvatar == null || yellowAvatar == null)
            return;

        SerializedObject serializedAppearance = new SerializedObject(appearance);
        SerializedProperty animatorControllerProperty = serializedAppearance.FindProperty("characterAnimatorController");
        SerializedProperty buildRedFromStaticMeshProperty = serializedAppearance.FindProperty("buildRedCharacterFromStaticMesh");
        SerializedProperty redStaticResourcePathProperty = serializedAppearance.FindProperty("redStaticCharacterResourcePath");
        SerializedProperty redStaticEulerOffsetProperty = serializedAppearance.FindProperty("redStaticCharacterEulerOffset");
        SerializedProperty redStaticGroundLiftProperty = serializedAppearance.FindProperty("redStaticCharacterGroundLift");
        SerializedProperty redCharacterProperty = serializedAppearance.FindProperty("redCharacterPrefab");
        SerializedProperty redMaterialProperty = serializedAppearance.FindProperty("redCharacterMaterial");
        SerializedProperty redAvatarProperty = serializedAppearance.FindProperty("redCharacterAvatar");
        SerializedProperty blueCharacterProperty = serializedAppearance.FindProperty("blueCharacterPrefab");
        SerializedProperty blueMaterialProperty = serializedAppearance.FindProperty("blueCharacterMaterial");
        SerializedProperty blueAvatarProperty = serializedAppearance.FindProperty("blueCharacterAvatar");
        SerializedProperty greenCharacterProperty = serializedAppearance.FindProperty("greenCharacterPrefab");
        SerializedProperty greenMaterialProperty = serializedAppearance.FindProperty("greenCharacterMaterial");
        SerializedProperty greenAvatarProperty = serializedAppearance.FindProperty("greenCharacterAvatar");
        SerializedProperty purpleCharacterProperty = serializedAppearance.FindProperty("purpleCharacterPrefab");
        SerializedProperty purpleMaterialProperty = serializedAppearance.FindProperty("purpleCharacterMaterial");
        SerializedProperty purpleAvatarProperty = serializedAppearance.FindProperty("purpleCharacterAvatar");
        SerializedProperty brownCharacterProperty = serializedAppearance.FindProperty("brownCharacterPrefab");
        SerializedProperty brownMaterialProperty = serializedAppearance.FindProperty("brownCharacterMaterial");
        SerializedProperty brownAvatarProperty = serializedAppearance.FindProperty("brownCharacterAvatar");
        SerializedProperty yellowCharacterProperty = serializedAppearance.FindProperty("yellowCharacterPrefab");
        SerializedProperty yellowMaterialProperty = serializedAppearance.FindProperty("yellowCharacterMaterial");
        SerializedProperty yellowAvatarProperty = serializedAppearance.FindProperty("yellowCharacterAvatar");

        if (animatorControllerProperty == null ||
            buildRedFromStaticMeshProperty == null || redStaticResourcePathProperty == null ||
            redStaticEulerOffsetProperty == null ||
            redStaticGroundLiftProperty == null ||
            redCharacterProperty == null || redMaterialProperty == null || redAvatarProperty == null ||
            blueCharacterProperty == null || blueMaterialProperty == null || blueAvatarProperty == null ||
            greenCharacterProperty == null || greenMaterialProperty == null || greenAvatarProperty == null ||
            purpleCharacterProperty == null || purpleMaterialProperty == null || purpleAvatarProperty == null ||
            brownCharacterProperty == null || brownMaterialProperty == null || brownAvatarProperty == null ||
            yellowCharacterProperty == null || yellowMaterialProperty == null || yellowAvatarProperty == null)
            return;

        animatorControllerProperty.objectReferenceValue = animatorController;
        buildRedFromStaticMeshProperty.boolValue = RedCharacterUsesStaticMesh;
        redStaticResourcePathProperty.stringValue = RedStaticCharacterResourcePath;
        redStaticEulerOffsetProperty.vector3Value = RedStaticCharacterEulerOffset;
        redStaticGroundLiftProperty.floatValue = RedStaticCharacterGroundLift;
        redCharacterProperty.objectReferenceValue = redCharacter;
        redAvatarProperty.objectReferenceValue = redAvatar;
        redMaterialProperty.objectReferenceValue = CreateOrUpdateCharacterMaterial(
            RedMaterialPath,
            RedBaseTexturePath,
            RedNormalTexturePath);
        blueCharacterProperty.objectReferenceValue = blueCharacter;
        blueAvatarProperty.objectReferenceValue = blueAvatar;
        blueMaterialProperty.objectReferenceValue = CreateOrUpdateCharacterMaterial(
            BlueMaterialPath,
            BlueBaseTexturePath,
            BlueNormalTexturePath);
        greenCharacterProperty.objectReferenceValue = greenCharacter;
        greenAvatarProperty.objectReferenceValue = greenAvatar;
        greenMaterialProperty.objectReferenceValue = CreateOrUpdateCharacterMaterial(
            GreenMaterialPath,
            GreenBaseTexturePath,
            GreenNormalTexturePath);
        purpleCharacterProperty.objectReferenceValue = purpleCharacter;
        purpleAvatarProperty.objectReferenceValue = purpleAvatar;
        purpleMaterialProperty.objectReferenceValue = CreateOrUpdateCharacterMaterial(
            PurpleMaterialPath,
            PurpleBaseTexturePath,
            PurpleNormalTexturePath);
        brownCharacterProperty.objectReferenceValue = brownCharacter;
        brownAvatarProperty.objectReferenceValue = brownAvatar;
        brownMaterialProperty.objectReferenceValue = CreateOrUpdateCharacterMaterial(
            BrownMaterialPath,
            BrownBaseTexturePath,
            BrownNormalTexturePath);
        yellowCharacterProperty.objectReferenceValue = yellowCharacter;
        yellowAvatarProperty.objectReferenceValue = yellowAvatar;
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
        GameObject blueCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(BlueCharacterPath);
        GameObject greenCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(GreenCharacterPath);
        GameObject purpleCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(PurpleCharacterPath);
        GameObject brownCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(BrownCharacterPath);
        GameObject yellowCharacter = AssetDatabase.LoadAssetAtPath<GameObject>(YellowCharacterPath);
        NetworkPlayerAppearance appearance = playerPrefab != null
            ? playerPrefab.GetComponent<NetworkPlayerAppearance>()
            : null;

        if (appearance == null || redCharacter == null || blueCharacter == null ||
            greenCharacter == null || purpleCharacter == null ||
            brownCharacter == null || yellowCharacter == null)
            throw new System.InvalidOperationException("Character variants are missing their player prefab or model assets.");

        SerializedObject serializedAppearance = new SerializedObject(appearance);
        ValidateCharacterVariant(serializedAppearance, "Red", redCharacter, "redCharacterPrefab", "redCharacterMaterial");
        ValidateCharacterVariant(serializedAppearance, "Blue", blueCharacter, "blueCharacterPrefab", "blueCharacterMaterial");
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
