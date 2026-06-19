using System.IO;
using UnityEditor;
using UnityEngine;

public static class SmallFlamePrefabCreator
{
    private const string PrefabFolder = "Assets/Assets/Prefabs";
    private const string PrefabPath = PrefabFolder + "/SmallAnimatedFlame.prefab";

    [MenuItem("Tools/Thanh Giong/Create Small Animated Flame")]
    public static void CreatePrefabFromMenu()
    {
        CreateOrUpdatePrefab(true);
    }

    [InitializeOnLoadMethod]
    private static void CreatePrefabOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(PrefabPath))
            {
                CreateOrUpdatePrefab(false);
            }
        };
    }

    private static void CreateOrUpdatePrefab(bool pingAsset)
    {
        if (!Directory.Exists(PrefabFolder))
        {
            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();
        }

        GameObject flame = new GameObject("SmallAnimatedFlame");
        AnimatedFlame animatedFlame = flame.AddComponent<AnimatedFlame>();
        animatedFlame.flameHeight = 1.15f;
        animatedFlame.flameWidth = 0.58f;
        animatedFlame.tongues = 5;
        animatedFlame.swayAmount = 0.1f;
        animatedFlame.flickerAmount = 0.16f;
        animatedFlame.lightIntensity = 1.25f;
        animatedFlame.lightRange = 2.75f;
        animatedFlame.smokeHeight = 1.15f;
        animatedFlame.Rebuild();

        PrefabUtility.SaveAsPrefabAsset(flame, PrefabPath);
        Object.DestroyImmediate(flame);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (pingAsset)
        {
            Object prefab = AssetDatabase.LoadAssetAtPath<Object>(PrefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
    }
}
