using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

public class ProjectCompileValidator
{
    public static void ValidateCompilation()
    {
        AssetDatabase.Refresh();
        if (EditorUtility.scriptCompilationFailed)
        {
            Debug.LogError("Compilation failed during initial domain load.");
            EditorApplication.Exit(1);
            return;
        }

        CompilationPipeline.compilationFinished += OnCompilationFinished;
        CompilationPipeline.RequestScriptCompilation();
    }

    private static void OnCompilationFinished(object obj)
    {
        CompilationPipeline.compilationFinished -= OnCompilationFinished;
        if (EditorUtility.scriptCompilationFailed)
        {
            Debug.LogError("Scripts have compiler errors");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("Compilation successful. No errors.");
            EditorApplication.Exit(0);
        }
    }
}
