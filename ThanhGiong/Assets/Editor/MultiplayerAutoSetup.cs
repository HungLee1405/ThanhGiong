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
    private const string MenuPath = "Thanh Giong/Setup Multiplayer";
    private const string AutoSetupSessionKey = "ThanhGiong.MultiplayerAutoSetup.DoneV3";

    static MultiplayerAutoSetup()
    {
        EditorApplication.delayCall += RunAutoSetupIfNeeded;
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
        EnsureComponent<NetworkPlayerAppearance>(workingCopy);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(workingCopy, PrefabPath);
        Object.DestroyImmediate(workingCopy);

        return prefab;
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
