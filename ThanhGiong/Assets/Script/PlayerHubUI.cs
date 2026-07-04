using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHubUI : MonoBehaviour
{
    [Header("Day UI")]
    public TMP_Text dayText;

    [Header("Quest UI")]
    public TMP_Text questText;
    public TMP_Text questNameText;

    [Header("Parallel Quest UI (Optional)")]
    public TMP_Text mainQuestText;
    public TMP_Text sideQuestText;

    private static readonly Color PanelColor = new Color(0.035f, 0.045f, 0.055f, 0.50f);
    private static readonly Color PanelBorderColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color TextColor = new Color(1f, 0.97f, 0.9f, 1f);
    private static readonly Color QuestTitleColor = new Color(0.78f, 0.83f, 0.88f, 1f);
    private static readonly Color MutedTextColor = new Color(0.72f, 0.9f, 1f, 1f);
    private static Sprite roundedPanelSprite;
    private bool styleApplied;

    private void Awake()
    {
        ApplyHudStyle();
    }

    private void Start()
    {
        ApplyHudStyle();
    }

    public void UpdateDayUI(int currentDay, float remainingTime)
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);

        ApplyHudStyle();

        if (dayText != null)
        {
            dayText.text =
                "<size=28><b>Ng\u00e0y " + currentDay + "</b></size>\n" +
                "<size=25>" + minutes.ToString("00") + ":" + seconds.ToString("00") + "</size>";
        }

        OnlineUIFont.ApplyToCurrentOnlineText();
    }

    public void UpdateQuestUI(string questName, string questDescription)
    {
        ApplyHudStyle();

        if (questNameText != null)
        {
            string title = string.IsNullOrWhiteSpace(questName) ? "NHI\u1ec6M V\u1ee4" : questName.ToUpper();
            questNameText.text = "-" + title + "-";
        }

        if (questText != null)
        {
            questText.text = FormatQuestDescription(questDescription);
        }

        ResizeQuestPanelToText();
        OnlineUIFont.ApplyToCurrentOnlineText(true);
    }

    public void UpdateQuestUI(
        string mainQuestName,
        string mainQuestDescription,
        string sideQuestName,
        string sideQuestDescription)
    {
        ApplyHudStyle();

        if (questNameText != null)
        {
            string title = string.IsNullOrWhiteSpace(mainQuestName) ? "NHI\u1ec6M V\u1ee4" : mainQuestName.ToUpper();
            questNameText.text = "-" + title + "-";
        }

        if (questText != null)
        {
            questText.text =
                FormatQuestDescription(mainQuestDescription) +
                "\n<size=19><color=#C7D0D8><b>-" + (string.IsNullOrWhiteSpace(sideQuestName) ? "NHI\u1ec6M V\u1ee4 PH\u1ee4" : sideQuestName.ToUpper()) + "-</b></color></size>\n" +
                FormatQuestDescription(sideQuestDescription);
        }

        ResizeQuestPanelToText();
        OnlineUIFont.ApplyToCurrentOnlineText(true);
    }

    public void ClearQuestUI()
    {
        ApplyHudStyle();

        if (questNameText != null)
        {
            questNameText.text = "-QUEST-";
        }

        if (questText != null)
        {
            questText.text = "Kh\u00f4ng c\u00f3 nhi\u1ec7m v\u1ee5 hi\u1ec7n t\u1ea1i.";
        }

        ResizeQuestPanelToText();
    }

    private void ApplyHudStyle()
    {
        if (styleApplied)
            return;

        styleApplied = true;

        ConfigureDayText();
        ConfigureQuestText(questNameText, 20f, QuestTitleColor, FontStyles.Bold);
        ConfigureQuestText(questText, 23f, MutedTextColor, FontStyles.Bold);
        ConfigureQuestText(mainQuestText, 21f, MutedTextColor, FontStyles.Normal);
        ConfigureQuestText(sideQuestText, 21f, MutedTextColor, FontStyles.Normal);

        ConfigureDayPanel();
        ConfigureQuestPanel();
    }

    private void ConfigureDayText()
    {
        if (dayText == null)
            return;

        dayText.alignment = TextAlignmentOptions.Center;
        dayText.color = TextColor;
        dayText.fontStyle = FontStyles.Bold;
        dayText.enableAutoSizing = false;
        dayText.fontSize = 28f;
        dayText.lineSpacing = -16f;
        dayText.raycastTarget = false;
        dayText.margin = new Vector4(12f, 8f, 12f, 8f);
        AddShadow(dayText.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(0f, -2f));
    }

    private void ConfigureQuestText(TMP_Text text, float size, Color color, FontStyles style)
    {
        if (text == null)
            return;

        text.color = color;
        text.fontSize = size;
        text.fontStyle = style;
        text.enableAutoSizing = false;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.lineSpacing = -4f;
        text.raycastTarget = false;
        AddShadow(text.gameObject, new Color(0f, 0f, 0f, 0.5f), new Vector2(1f, -1f));
    }

    private void ConfigureDayPanel()
    {
        if (dayText == null || dayText.transform.parent == null)
            return;

        RectTransform panel = dayText.transform.parent as RectTransform;
        if (panel == null)
            return;

        panel.anchorMin = new Vector2(0.5f, 1f);
        panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -8f);
        panel.sizeDelta = new Vector2(220f, 64f);

        RectTransform textRect = dayText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 4f);
        textRect.offsetMax = new Vector2(-12f, -4f);

        ConfigurePanelGraphic(panel.gameObject, PanelColor, PanelBorderColor);
    }

    private void ConfigureQuestPanel()
    {
        TMP_Text anchorText = questNameText != null ? questNameText : questText;
        if (anchorText == null || anchorText.transform.parent == null)
            return;

        RectTransform panel = anchorText.transform.parent as RectTransform;
        if (panel == null)
            return;

        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(32f, -142f);
        panel.sizeDelta = new Vector2(430f, 170f);

        if (questNameText != null)
        {
            RectTransform titleRect = questNameText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.offsetMin = new Vector2(18f, -38f);
            titleRect.offsetMax = new Vector2(-18f, -12f);
        }

        if (questText != null)
        {
            RectTransform bodyRect = questText.rectTransform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(20f, 16f);
            bodyRect.offsetMax = new Vector2(-18f, -44f);
        }

        ConfigurePanelGraphic(panel.gameObject, PanelColor, PanelBorderColor);
    }

    private void ConfigurePanelGraphic(GameObject panelObject, Color backgroundColor, Color borderColor)
    {
        Image image = panelObject.GetComponent<Image>();
        if (image == null)
        {
            image = panelObject.AddComponent<Image>();
        }

        image.sprite = GetRoundedPanelSprite();
        image.type = Image.Type.Sliced;
        image.color = backgroundColor;
        image.raycastTarget = false;

        Outline outline = panelObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = panelObject.AddComponent<Outline>();
        }

        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = true;
    }

    private void ResizeQuestPanelToText()
    {
        TMP_Text anchorText = questNameText != null ? questNameText : questText;
        if (anchorText == null || anchorText.transform.parent == null)
            return;

        RectTransform panel = anchorText.transform.parent as RectTransform;
        if (panel == null)
            return;

        float titleHeight = questNameText != null ? 34f : 0f;
        float bodyHeight = questText != null ? questText.GetPreferredValues(392f, 0f).y : 0f;
        float targetHeight = Mathf.Clamp(42f + titleHeight + bodyHeight, 112f, 232f);
        panel.sizeDelta = new Vector2(430f, targetHeight);
    }

    private void AddShadow(GameObject target, Color color, Vector2 distance)
    {
        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = target.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private Sprite GetRoundedPanelSprite()
    {
        if (roundedPanelSprite != null)
            return roundedPanelSprite;

        const int textureSize = 32;
        const int radius = 8;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "HUD Rounded Panel",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color transparent = new Color(1f, 1f, 1f, 0f);
        Color solid = Color.white;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float nearestX = Mathf.Clamp(x, radius, textureSize - radius - 1);
                float nearestY = Mathf.Clamp(y, radius, textureSize - radius - 1);
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nearestX, nearestY));
                texture.SetPixel(x, y, distance <= radius ? solid : transparent);
            }
        }

        texture.Apply();
        roundedPanelSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));

        return roundedPanelSprite;
    }

    private string FormatQuestDescription(string questDescription)
    {
        if (string.IsNullOrWhiteSpace(questDescription))
            return "";

        string text = questDescription.Trim();
        if (text.StartsWith("- "))
        {
            text = text.Substring(2).TrimStart();
        }

        string[] lines = text.Split('\n');
        string instruction = lines[0].Trim();
        string formattedText = "<color=#65E66F><b>- " + instruction + " -</b></color>";

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            formattedText += "\n<size=20><color=#E7ECF0>" + line + "</color></size>";
        }

        return formattedText;
    }
}
