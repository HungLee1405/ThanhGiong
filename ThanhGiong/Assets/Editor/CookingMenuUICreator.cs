#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

public class CookingMenuUICreator : EditorWindow
{
    private static readonly Color WoodBrown = ColorUtility.TryParseHtmlString("#5C4033", out Color c1) ? c1 : Color.gray;
    private static readonly Color DarkWood = ColorUtility.TryParseHtmlString("#3B271F", out Color c2) ? c2 : Color.black;
    private static readonly Color WheatBorder = ColorUtility.TryParseHtmlString("#F5DEB3", out Color c3) ? c3 : Color.white;
    private static readonly Color CreamText = ColorUtility.TryParseHtmlString("#FFFDD0", out Color c4) ? c4 : Color.white;
    private static readonly Color ReddishBrown = ColorUtility.TryParseHtmlString("#A52A2A", out Color c5) ? c5 : Color.red;

    [MenuItem("Tools/Thánh Gióng/Create Cooking Menu UI")]
    public static void CreateUI()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != "Assets/Scenes/GameScene.unity")
        {
            EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        }

        Canvas targetCanvas = FindOrCreateCanvas();
        EnsureEventSystem();

        GameObject prefabIngredientRow = CreateOrUpdateIngredientRowPrefab();
        GameObject prefabRecipeRow = CreateOrUpdateRecipeRowPrefab();

        GameObject menuRoot = FindOrCreateUIObject("CookingMenuPanel", targetCanvas.transform);
        Undo.RegisterCreatedObjectUndo(menuRoot, "Create Cooking Menu UI");
        
        // Ensure root is active
        menuRoot.SetActive(true);

        RectTransform rootRect = menuRoot.GetComponent<RectTransform>();
        SetStretch(rootRect);

        CookingMenuUI menuUI = menuRoot.GetComponent<CookingMenuUI>() ?? menuRoot.AddComponent<CookingMenuUI>();

        // 1. DarkOverlay
        GameObject darkOverlay = FindOrCreateUIObject("DarkOverlay", menuRoot.transform);
        RectTransform darkRect = darkOverlay.GetComponent<RectTransform>();
        SetStretch(darkRect);
        Image darkImg = darkOverlay.GetComponent<Image>() ?? darkOverlay.AddComponent<Image>();
        darkImg.color = new Color(0, 0, 0, 0.7f);
        // Block raycasts
        darkImg.raycastTarget = true;
        menuUI.darkOverlay = darkOverlay;
        
        // We'll use VisualContainer pattern
        GameObject visualContainer = FindOrCreateUIObject("VisualContainer", menuRoot.transform);
        RectTransform vcRect = visualContainer.GetComponent<RectTransform>();
        SetStretch(vcRect);
        menuUI.visualContainer = visualContainer;

        // 2. MainPanel
        GameObject mainPanel = FindOrCreateUIObject("MainPanel", visualContainer.transform);
        RectTransform mainRect = mainPanel.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.15f, 0.125f);
        mainRect.anchorMax = new Vector2(0.85f, 0.875f);
        mainRect.offsetMin = Vector2.zero;
        mainRect.offsetMax = Vector2.zero;
        Image mainImg = mainPanel.GetComponent<Image>() ?? mainPanel.AddComponent<Image>();
        mainImg.color = WoodBrown;
        Outline mainOutline = mainPanel.GetComponent<Outline>() ?? mainPanel.AddComponent<Outline>();
        mainOutline.effectColor = WheatBorder;
        mainOutline.effectDistance = new Vector2(4, -4);
        menuUI.mainPanel = mainPanel;
        menuUI.menuPanel = mainPanel;

        // Header
        GameObject header = FindOrCreateUIObject("Header", mainPanel.transform);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0.9f);
        headerRect.anchorMax = new Vector2(1, 1f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;
        Image headerImg = header.GetComponent<Image>() ?? header.AddComponent<Image>();
        headerImg.color = DarkWood;

        TextMeshProUGUI titleText = CreateText("TitleText", header.transform, "Menu Nấu Ăn", 36, TextAlignmentOptions.Center);
        SetStretch(titleText.rectTransform);

        GameObject closeBtnGo = CreateButton("CloseButton", header.transform, "X", out TextMeshProUGUI closeBtnTxt);
        RectTransform closeBtnRect = closeBtnGo.GetComponent<RectTransform>();
        closeBtnRect.anchorMin = new Vector2(1, 0.5f);
        closeBtnRect.anchorMax = new Vector2(1, 0.5f);
        closeBtnRect.pivot = new Vector2(1, 0.5f);
        closeBtnRect.sizeDelta = new Vector2(50, 50);
        closeBtnRect.anchoredPosition = new Vector2(-10, 0);
        menuUI.closeButton = closeBtnGo.GetComponent<Button>();

        // Body
        GameObject body = FindOrCreateUIObject("Body", mainPanel.transform);
        RectTransform bodyRect = body.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0, 0);
        bodyRect.anchorMax = new Vector2(1, 0.9f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
        HorizontalLayoutGroup bodyLayout = body.GetComponent<HorizontalLayoutGroup>() ?? body.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 10;
        bodyLayout.padding = new RectOffset(10, 10, 10, 10);
        bodyLayout.childControlHeight = true;
        bodyLayout.childControlWidth = true;

        // RecipeListPanel
        GameObject listPanel = FindOrCreateUIObject("RecipeListPanel", body.transform);
        LayoutElement listLe = listPanel.GetComponent<LayoutElement>() ?? listPanel.AddComponent<LayoutElement>();
        listLe.flexibleWidth = 0.35f;
        Image listImg = listPanel.GetComponent<Image>() ?? listPanel.AddComponent<Image>();
        listImg.color = DarkWood;

        TextMeshProUGUI listTitle = CreateText("ListTitleText", listPanel.transform, "Danh sách món", 28, TextAlignmentOptions.Center);
        listTitle.rectTransform.anchorMin = new Vector2(0, 0.9f);
        listTitle.rectTransform.anchorMax = new Vector2(1, 1f);
        SetOffsets(listTitle.rectTransform, 0, 0, 0, 0);

        GameObject scrollView = FindOrCreateUIObject("RecipeScrollView", listPanel.transform);
        RectTransform scrollRectT = scrollView.GetComponent<RectTransform>();
        scrollRectT.anchorMin = new Vector2(0, 0);
        scrollRectT.anchorMax = new Vector2(1, 0.9f);
        SetOffsets(scrollRectT, 0, 0, 0, 0);
        ScrollRect sr = scrollView.GetComponent<ScrollRect>() ?? scrollView.AddComponent<ScrollRect>();
        sr.horizontal = false;
        
        GameObject viewport = FindOrCreateUIObject("Viewport", scrollView.transform);
        RectTransform viewRect = viewport.GetComponent<RectTransform>();
        SetStretch(viewRect);
        Image viewImg = viewport.GetComponent<Image>() ?? viewport.AddComponent<Image>();
        viewImg.color = new Color(0,0,0,0.1f);
        Mask mask = viewport.GetComponent<Mask>() ?? viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        sr.viewport = viewRect;

        GameObject content = FindOrCreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0); // driven by fitter
        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>() ?? content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 5;
        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>() ?? content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRect;
        menuUI.recipeListContent = content.transform;

        // RecipeDetailPanel
        GameObject detailPanel = FindOrCreateUIObject("RecipeDetailPanel", body.transform);
        LayoutElement detailLe = detailPanel.GetComponent<LayoutElement>() ?? detailPanel.AddComponent<LayoutElement>();
        detailLe.flexibleWidth = 0.65f;
        Image detailImg = detailPanel.GetComponent<Image>() ?? detailPanel.AddComponent<Image>();
        detailImg.color = DarkWood;

        GameObject iconGo = FindOrCreateUIObject("RecipeIcon", detailPanel.transform);
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.7f);
        iconRect.anchorMax = new Vector2(0.5f, 0.7f);
        iconRect.sizeDelta = new Vector2(150, 150);
        iconRect.anchoredPosition = new Vector2(0, 0);
        Image iconImage = iconGo.GetComponent<Image>() ?? iconGo.AddComponent<Image>();
        menuUI.recipeIcon = iconImage;

        TextMeshProUGUI nameTxt = CreateText("RecipeNameText", detailPanel.transform, "Tên Món", 36, TextAlignmentOptions.Center);
        nameTxt.rectTransform.anchorMin = new Vector2(0, 0.6f);
        nameTxt.rectTransform.anchorMax = new Vector2(1, 0.7f);
        SetOffsets(nameTxt.rectTransform, 0, 0, 0, 0);
        menuUI.recipeNameText = nameTxt;

        TextMeshProUGUI descTxt = CreateText("RecipeDescriptionText", detailPanel.transform, "Mô tả", 22, TextAlignmentOptions.TopLeft);
        descTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.45f);
        descTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.6f);
        SetOffsets(descTxt.rectTransform, 0, 0, 0, 0);
        menuUI.recipeDescriptionText = descTxt;

        TextMeshProUGUI ingTitle = CreateText("IngredientTitleText", detailPanel.transform, "Nguyên Liệu:", 24, TextAlignmentOptions.Left);
        ingTitle.rectTransform.anchorMin = new Vector2(0.05f, 0.4f);
        ingTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.45f);
        SetOffsets(ingTitle.rectTransform, 0, 0, 0, 0);

        GameObject ingList = FindOrCreateUIObject("IngredientList", detailPanel.transform);
        RectTransform ingListRect = ingList.GetComponent<RectTransform>();
        ingListRect.anchorMin = new Vector2(0.05f, 0.15f);
        ingListRect.anchorMax = new Vector2(0.95f, 0.4f);
        SetOffsets(ingListRect, 0, 0, 0, 0);
        VerticalLayoutGroup ingVLG = ingList.GetComponent<VerticalLayoutGroup>() ?? ingList.AddComponent<VerticalLayoutGroup>();
        ingVLG.childControlHeight = false;
        ingVLG.childControlWidth = true;
        ingVLG.childForceExpandHeight = false;
        ingVLG.spacing = 5;
        menuUI.ingredientListContent = ingList.transform;

        TextMeshProUGUI timeTxt = CreateText("CookTimeText", detailPanel.transform, "Thời gian:", 22, TextAlignmentOptions.Left);
        timeTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.1f);
        timeTxt.rectTransform.anchorMax = new Vector2(0.45f, 0.15f);
        SetOffsets(timeTxt.rectTransform, 0, 0, 0, 0);
        menuUI.cookTimeText = timeTxt;

        TextMeshProUGUI effectTxt = CreateText("EffectText", detailPanel.transform, "Thành phẩm:", 22, TextAlignmentOptions.Left);
        effectTxt.rectTransform.anchorMin = new Vector2(0.5f, 0.1f);
        effectTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.15f);
        SetOffsets(effectTxt.rectTransform, 0, 0, 0, 0);
        menuUI.effectText = effectTxt;

        TextMeshProUGUI lockTxt = CreateText("LockedHintText", detailPanel.transform, "Yêu cầu:", 22, TextAlignmentOptions.Center);
        lockTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.05f);
        lockTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.1f);
        SetOffsets(lockTxt.rectTransform, 0, 0, 0, 0);
        lockTxt.color = new Color(0.8f, 0.3f, 0.3f);
        menuUI.lockedHintText = lockTxt;

        GameObject cookBtnGo = CreateButton("CookButton", detailPanel.transform, "Nấu", out TextMeshProUGUI cookBtnTxt);
        RectTransform cookBtnRect = cookBtnGo.GetComponent<RectTransform>();
        cookBtnRect.anchorMin = new Vector2(0.3f, 0.02f);
        cookBtnRect.anchorMax = new Vector2(0.7f, 0.08f);
        SetOffsets(cookBtnRect, 0, 0, 0, 0);
        menuUI.cookButton = cookBtnGo.GetComponent<Button>();

        // 3. CookingProgressPanel
        GameObject progPanel = FindOrCreateUIObject("CookingProgressPanel", visualContainer.transform);
        RectTransform progRect = progPanel.GetComponent<RectTransform>();
        progRect.anchorMin = new Vector2(0.3f, 0.4f);
        progRect.anchorMax = new Vector2(0.7f, 0.6f);
        SetOffsets(progRect, 0, 0, 0, 0);
        Image progImg = progPanel.GetComponent<Image>() ?? progPanel.AddComponent<Image>();
        progImg.color = WoodBrown;
        Outline progOutline = progPanel.GetComponent<Outline>() ?? progPanel.AddComponent<Outline>();
        progOutline.effectColor = WheatBorder;
        progOutline.effectDistance = new Vector2(2, -2);
        menuUI.progressPanel = progPanel;

        TextMeshProUGUI progTitle = CreateText("ProgressTitleText", progPanel.transform, "Đang nấu...", 28, TextAlignmentOptions.Center);
        progTitle.rectTransform.anchorMin = new Vector2(0, 0.6f);
        progTitle.rectTransform.anchorMax = new Vector2(1, 0.9f);
        SetOffsets(progTitle.rectTransform, 0, 0, 0, 0);
        menuUI.progressTitleText = progTitle;

        GameObject progBg = FindOrCreateUIObject("ProgressBarBackground", progPanel.transform);
        RectTransform progBgRect = progBg.GetComponent<RectTransform>();
        progBgRect.anchorMin = new Vector2(0.1f, 0.3f);
        progBgRect.anchorMax = new Vector2(0.9f, 0.5f);
        SetOffsets(progBgRect, 0, 0, 0, 0);
        Image pBgImg = progBg.GetComponent<Image>() ?? progBg.AddComponent<Image>();
        pBgImg.color = DarkWood;

        GameObject progFill = FindOrCreateUIObject("ProgressBarFill", progBg.transform);
        RectTransform progFillRect = progFill.GetComponent<RectTransform>();
        SetStretch(progFillRect);
        Image pFillImg = progFill.GetComponent<Image>() ?? progFill.AddComponent<Image>();
        pFillImg.color = ReddishBrown;
        pFillImg.type = Image.Type.Filled;
        pFillImg.fillMethod = Image.FillMethod.Horizontal;
        menuUI.progressFillImage = pFillImg;

        TextMeshProUGUI progTxt = CreateText("ProgressText", progPanel.transform, "0%", 24, TextAlignmentOptions.Center);
        progTxt.rectTransform.anchorMin = new Vector2(0, 0.05f);
        progTxt.rectTransform.anchorMax = new Vector2(1, 0.25f);
        SetOffsets(progTxt.rectTransform, 0, 0, 0, 0);
        menuUI.progressText = progTxt;

        // 4. PendingOutputPanel
        GameObject pendPanel = FindOrCreateUIObject("PendingOutputPanel", visualContainer.transform);
        RectTransform pendRect = pendPanel.GetComponent<RectTransform>();
        pendRect.anchorMin = new Vector2(0.3f, 0.35f);
        pendRect.anchorMax = new Vector2(0.7f, 0.65f);
        SetOffsets(pendRect, 0, 0, 0, 0);
        Image pendImg = pendPanel.GetComponent<Image>() ?? pendPanel.AddComponent<Image>();
        pendImg.color = WoodBrown;
        Outline pendOutline = pendPanel.GetComponent<Outline>() ?? pendPanel.AddComponent<Outline>();
        pendOutline.effectColor = WheatBorder;
        pendOutline.effectDistance = new Vector2(2, -2);
        menuUI.pendingOutputPanel = pendPanel;

        GameObject pendIconGo = FindOrCreateUIObject("OutputIcon", pendPanel.transform);
        RectTransform pendIconRect = pendIconGo.GetComponent<RectTransform>();
        pendIconRect.anchorMin = new Vector2(0.5f, 0.65f);
        pendIconRect.anchorMax = new Vector2(0.5f, 0.65f);
        pendIconRect.sizeDelta = new Vector2(100, 100);
        pendIconRect.anchoredPosition = new Vector2(0, 0);
        Image pendIconImg = pendIconGo.GetComponent<Image>() ?? pendIconGo.AddComponent<Image>();
        menuUI.pendingOutputIcon = pendIconImg;

        TextMeshProUGUI pendTxt = CreateText("OutputText", pendPanel.transform, "Nhận: N/A", 26, TextAlignmentOptions.Center);
        pendTxt.rectTransform.anchorMin = new Vector2(0, 0.35f);
        pendTxt.rectTransform.anchorMax = new Vector2(1, 0.45f);
        SetOffsets(pendTxt.rectTransform, 0, 0, 0, 0);
        menuUI.pendingOutputText = pendTxt;

        GameObject takeBtnGo = CreateButton("TakeButton", pendPanel.transform, "Lấy ra", out TextMeshProUGUI takeBtnTxt);
        RectTransform takeBtnRect = takeBtnGo.GetComponent<RectTransform>();
        takeBtnRect.anchorMin = new Vector2(0.3f, 0.1f);
        takeBtnRect.anchorMax = new Vector2(0.7f, 0.25f);
        SetOffsets(takeBtnRect, 0, 0, 0, 0);
        menuUI.takeOutputButton = takeBtnGo.GetComponent<Button>();

        menuUI.recipeRowPrefab = prefabRecipeRow;
        menuUI.ingredientRowPrefab = prefabIngredientRow;

        LinkCookingPots(menuUI);

        EditorUtility.SetDirty(menuRoot);
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("Cooking Menu UI Created successfully!");
    }

    [MenuItem("Tools/Thánh Gióng/Remove Generated Cooking Menu UI")]
    public static void RemoveUI()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        GameObject root = GameObject.Find("CookingMenuPanel");
        if (root != null)
        {
            Undo.DestroyObjectImmediate(root);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("CookingMenuPanel removed.");
        }
    }

    private static Canvas FindOrCreateCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas target = canvases.FirstOrDefault(c => 
            c.renderMode != RenderMode.WorldSpace && 
            (c.name.Contains("PlayerHub") || c.name.Contains("HUD")));
        
        if (target != null) return target;
        
        target = canvases.FirstOrDefault(c => c.renderMode != RenderMode.WorldSpace);
        if (target != null) return target;

        GameObject canvasGo = new GameObject("CookingUICanvas", typeof(RectTransform));
        target = canvasGo.AddComponent<Canvas>();
        target.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");
        return target;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length == 0)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
        }
    }

    private static GameObject CreateOrUpdateRecipeRowPrefab()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");

        string path = "Assets/Prefabs/UI/RecipeRow.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject rowGo = new GameObject("RecipeRow", typeof(RectTransform));
        RectTransform rt = rowGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 90);
        
        LayoutElement le = rowGo.AddComponent<LayoutElement>();
        le.minHeight = 90;

        Image bg = rowGo.AddComponent<Image>();
        bg.color = DarkWood;

        Button btn = rowGo.AddComponent<Button>();
        btn.targetGraphic = bg;
        ColorBlock cb = btn.colors;
        cb.normalColor = DarkWood;
        cb.highlightedColor = new Color(DarkWood.r + 0.1f, DarkWood.g + 0.1f, DarkWood.b + 0.1f);
        cb.pressedColor = WoodBrown;
        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = new Color(DarkWood.r, DarkWood.g, DarkWood.b, 0.5f);
        btn.colors = cb;

        RecipeRowUI ui = rowGo.AddComponent<RecipeRowUI>();

        GameObject iconGo = new GameObject("RecipeIcon", typeof(RectTransform));
        iconGo.transform.SetParent(rowGo.transform, false);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(10, 0);
        iconRt.sizeDelta = new Vector2(70, 70);
        Image iconImg = iconGo.AddComponent<Image>();
        ui.iconImage = iconImg;

        GameObject tc = new GameObject("TextContainer", typeof(RectTransform));
        tc.transform.SetParent(rowGo.transform, false);
        RectTransform tcRt = tc.GetComponent<RectTransform>();
        tcRt.anchorMin = new Vector2(0, 0);
        tcRt.anchorMax = new Vector2(1, 1);
        tcRt.offsetMin = new Vector2(90, 5);
        tcRt.offsetMax = new Vector2(-5, -5);

        TextMeshProUGUI nameTxt = CreateText("RecipeNameText", tc.transform, "Recipe Name", 26, TextAlignmentOptions.Left);
        nameTxt.rectTransform.anchorMin = new Vector2(0, 0.5f);
        nameTxt.rectTransform.anchorMax = new Vector2(1, 1f);
        SetOffsets(nameTxt.rectTransform, 0, 0, 0, 0);
        ui.nameText = nameTxt;

        TextMeshProUGUI statusTxt = CreateText("RecipeStatusText", tc.transform, "Sẵn sàng", 20, TextAlignmentOptions.Left);
        statusTxt.rectTransform.anchorMin = new Vector2(0, 0);
        statusTxt.rectTransform.anchorMax = new Vector2(1, 0.5f);
        SetOffsets(statusTxt.rectTransform, 0, 0, 0, 0);
        ui.statusText = statusTxt;

        GameObject lockGo = new GameObject("LockOverlay", typeof(RectTransform));
        lockGo.transform.SetParent(rowGo.transform, false);
        RectTransform lockRt = lockGo.GetComponent<RectTransform>();
        SetStretch(lockRt);
        Image lockImg = lockGo.AddComponent<Image>();
        lockImg.color = new Color(0, 0, 0, 0.5f);
        ui.lockOverlay = lockGo;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rowGo, path);
        DestroyImmediate(rowGo);
        return prefab;
    }

    private static GameObject CreateOrUpdateIngredientRowPrefab()
    {
        string path = "Assets/Prefabs/UI/IngredientRow.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject rowGo = new GameObject("IngredientRow", typeof(RectTransform));
        RectTransform rt = rowGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 40);
        
        LayoutElement le = rowGo.AddComponent<LayoutElement>();
        le.minHeight = 40;

        IngredientRowUI ui = rowGo.AddComponent<IngredientRowUI>();

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
        iconGo.transform.SetParent(rowGo.transform, false);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(5, 0);
        iconRt.sizeDelta = new Vector2(30, 30);
        Image iconImg = iconGo.AddComponent<Image>();
        ui.iconImage = iconImg;

        TextMeshProUGUI nameTxt = CreateText("NameText", rowGo.transform, "Ingredient", 22, TextAlignmentOptions.Left);
        nameTxt.rectTransform.anchorMin = new Vector2(0, 0);
        nameTxt.rectTransform.anchorMax = new Vector2(0.7f, 1f);
        SetOffsets(nameTxt.rectTransform, 45, 0, 0, 0);
        ui.nameText = nameTxt;

        TextMeshProUGUI amtTxt = CreateText("AmountText", rowGo.transform, "0/1", 22, TextAlignmentOptions.Right);
        amtTxt.rectTransform.anchorMin = new Vector2(0.7f, 0);
        amtTxt.rectTransform.anchorMax = new Vector2(1, 1f);
        SetOffsets(amtTxt.rectTransform, 0, 0, -5, 0);
        ui.amountText = amtTxt;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rowGo, path);
        DestroyImmediate(rowGo);
        return prefab;
    }

    private static void LinkCookingPots(CookingMenuUI menuUI)
    {
        CookingPot[] pots = FindObjectsByType<CookingPot>(FindObjectsSortMode.None);
        int linked = 0;
        foreach (var p in pots)
        {
            p.cookingMenuUI = menuUI;
            p.useDataDrivenMenu = true;
            EditorUtility.SetDirty(p);
            linked++;
        }
        Debug.Log($"Linked {linked} CookingPot(s) to CookingMenuUI.");
    }

    private static GameObject FindOrCreateUIObject(string name, Transform parent)
    {
        Transform t = parent.Find(name);
        if (t != null)
        {
            RectTransform rt = t.GetComponent<RectTransform>();
            if (rt == null)
            {
                Undo.DestroyObjectImmediate(t.gameObject);
                // Proceeds to recreate below
            }
            else
            {
                return t.gameObject;
            }
        }
        return CreateUIObject(name, parent);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetStretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetOffsets(RectTransform rt, float left, float bottom, float right, float top)
    {
        if (rt == null) return;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(right, top);
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, TextAlignmentOptions align)
    {
        GameObject go = FindOrCreateUIObject(name, parent);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = CreamText;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static GameObject CreateButton(string name, Transform parent, string text, out TextMeshProUGUI txtComp)
    {
        GameObject go = FindOrCreateUIObject(name, parent);
        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = ReddishBrown;
        Button btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
        
        ColorBlock cb = btn.colors;
        cb.normalColor = ReddishBrown;
        cb.highlightedColor = new Color(ReddishBrown.r + 0.1f, ReddishBrown.g + 0.1f, ReddishBrown.b + 0.1f);
        cb.pressedColor = DarkWood;
        cb.selectedColor = cb.highlightedColor;
        cb.disabledColor = new Color(ReddishBrown.r, ReddishBrown.g, ReddishBrown.b, 0.5f);
        btn.colors = cb;

        txtComp = CreateText("ButtonText", go.transform, text, 24, TextAlignmentOptions.Center);
        SetStretch(txtComp.rectTransform);
        return go;
    }
}
#endif
