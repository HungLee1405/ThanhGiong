using TMPro;
using UnityEngine;

public static class OnlineUIFont
{
    private const string TitleFontName = "Be Vietnam Pro";
    private const string UIFontName = "Be Vietnam Pro";
    private const string TitleFontResourcePath = "Fonts/BeVietnamPro-Regular";
    private const string UIFontResourcePath = "Fonts/BeVietnamPro-Regular";
    private const float ApplyInterval = 0.5f;

    private static Font titleFont;
    private static Font uiFont;
    private static TMP_FontAsset uiTmpFont;
    private static float nextApplyTime;

    public static Font CreateTitleFont()
    {
        titleFont ??= Resources.Load<Font>(TitleFontResourcePath);

        if (titleFont != null)
            return titleFont;

        return CreateRuntimeFont(TitleFontName, "Arial", "Segoe UI");
    }

    public static Font CreateUIFont()
    {
        uiFont ??= Resources.Load<Font>(UIFontResourcePath);

        if (uiFont != null)
            return uiFont;

        return CreateRuntimeFont(UIFontName, "Arial", "Segoe UI");
    }

    public static void ApplyToCurrentOnlineText(bool force = false)
    {
        ApplyToCurrentText(force);
    }

    public static void ApplyToCurrentText(bool force = false)
    {
        if (!force && Time.unscaledTime < nextApplyTime)
            return;

        nextApplyTime = Time.unscaledTime + ApplyInterval;
        EnsureFonts();

        if (uiTmpFont != null)
        {
            TMP_Settings.defaultFontAsset = uiTmpFont;

            TMP_Text[] tmpTexts = UnityEngine.Object.FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < tmpTexts.Length; i++)
            {
                if (tmpTexts[i] != null && tmpTexts[i].font != uiTmpFont)
                {
                    tmpTexts[i].font = uiTmpFont;
                }
            }
        }

        UnityEngine.UI.Text[] legacyTexts = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < legacyTexts.Length; i++)
        {
            if (legacyTexts[i] != null && legacyTexts[i].font != uiFont)
            {
                legacyTexts[i].font = uiFont;
            }
        }
    }

    private static void EnsureFonts()
    {
        uiFont ??= Resources.Load<Font>(UIFontResourcePath);

        if (uiTmpFont == null && uiFont != null)
        {
            try
            {
                uiTmpFont = TMP_FontAsset.CreateFontAsset(uiFont);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("UI font could not create a TMP font asset: " + exception.Message);
                return;
            }

            if (uiTmpFont != null)
            {
                uiTmpFont.name = UIFontName + " Game TMP";
            }
        }
    }

    private static Font CreateRuntimeFont(params string[] fontNames)
    {
        for (int i = 0; i < fontNames.Length; i++)
        {
            Font font = Font.CreateDynamicFontFromOSFont(fontNames[i], 18);

            if (font != null)
                return font;
        }

        return GUI.skin.font;
    }
}

public class UIFontApplier : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<UIFontApplier>() != null)
            return;

        GameObject applierObject = new GameObject("UI Font Applier");
        DontDestroyOnLoad(applierObject);
        applierObject.AddComponent<UIFontApplier>();
        OnlineUIFont.ApplyToCurrentText(true);
    }

    private void Update()
    {
        OnlineUIFont.ApplyToCurrentText();
    }
}
