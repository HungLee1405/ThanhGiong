using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public static class EndingSceneTestMenu
{
    private const string EndingScenePath = "Assets/Scenes/EndingScene.unity";
    private const string AutoRunFlagPath = "Temp/CodexRunEndingTest.flag";

    static EndingSceneTestMenu()
    {
        EditorApplication.delayCall += RunRequestedEndingTest;
    }

    private static void RunRequestedEndingTest()
    {
        if (!File.Exists(AutoRunFlagPath))
            return;

        File.Delete(AutoRunFlagPath);
        Debug.Log("Codex ending test: opening EndingScene and entering Play Mode.");
        TestEndingScene();
    }

    [MenuItem("Tools/Thánh Gióng/Test Ending Scene")]
    private static void TestEndingScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(EndingScenePath, OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
}
