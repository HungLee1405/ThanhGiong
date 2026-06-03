using UnityEditor;
using UnityEngine;

public class URPPackageImporter : EditorWindow
{
    [MenuItem("Tools/Vietnam Village/Import URP Assets")]
    public static void ImportURPAssets()
    {
        string packagePath = "Assets/LeartesStudios/BanditsValley/URP/URP_Bandit_Valley.unitypackage";
        
        if (!System.IO.File.Exists(packagePath))
        {
            Debug.LogError($"Không tìm thấy file package URP tại: {packagePath}");
            EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy file package URP tại:\n{packagePath}", "Đóng");
            return;
        }

        Debug.Log("Đang bắt đầu giải nén gói URP Asset. Quá trình này có thể mất vài phút...");
        
        // Import package. Setting interactive to true will show the selection window,
        // which lets the user see what is being imported and click "Import".
        AssetDatabase.ImportPackage(packagePath, true);
    }
}
