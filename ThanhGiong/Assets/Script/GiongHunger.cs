using UnityEngine;
using UnityEngine.UI;

public class GiongHunger : MonoBehaviour
{
    [Header("Hunger Settings")]
    public float maxHunger = 100f;
    public float currentHunger = 100f;
    public float minRequiredHunger = 80f;

    [Header("Decrease Settings")]
    public float decreaseAmount = 1f;
    public float decreaseInterval = 3f;

    [Header("Runtime")]
    public bool isHungerRunning = false;

    [Header("UI")]
    public Slider hungerSlider;
    public GameObject hungerUIObject;
    public Text hungerText;

    private float decreaseTimer = 0f;

    private void Start()
    {
        if (hungerSlider == null)
        {
            CreateDefaultUI();
        }

        ResetHunger();
        StopHungerDrain();

        UpdateUI();
    }

    private void Update()
    {
        if (NetworkLobbyCoordinator.IsOnlineLobbyActive) return;
        if (!isHungerRunning) return;

        decreaseTimer += Time.deltaTime;

        if (decreaseTimer >= decreaseInterval)
        {
            decreaseTimer = 0f;
            DecreaseHunger(decreaseAmount);
        }
    }

    public void StartHungerDrain()
    {
        isHungerRunning = true;
        decreaseTimer = 0f;

        if (hungerUIObject != null)
        {
            hungerUIObject.SetActive(true);
        }

        Debug.Log("Thanh đói của Gióng bắt đầu tụt.");
    }

    public void StopHungerDrain()
    {
        isHungerRunning = false;
        decreaseTimer = 0f;

        Debug.Log("Thanh đói của Gióng đã dừng.");
    }

    public void ResetHunger()
    {
        currentHunger = maxHunger;
        decreaseTimer = 0f;
        UpdateUI();
    }

    public void ResetForNewDay()
    {
        ResetHunger();
        StopHungerDrain();

        if (hungerUIObject != null)
        {
            hungerUIObject.SetActive(true);
        }

        Debug.Log("Reset thanh đói cho ngày mới.");
    }

    public void DecreaseHunger(float amount)
    {
        currentHunger -= amount;
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);

        UpdateUI();

        if (currentHunger <= 0f)
        {
            Debug.Log("Gióng quá đói! Game Over.");
            StopHungerDrain();
        }
    }

    public void Feed(float amount)
    {
        currentHunger += amount;
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);

        UpdateUI();

        Debug.Log("Đã cho Gióng ăn. Thanh đói hiện tại: " + currentHunger);
    }

    public bool IsDaySuccess()
    {
        return currentHunger >= minRequiredHunger;
    }

    private void UpdateUI()
    {
        if (hungerSlider != null)
        {
            hungerSlider.value = currentHunger / maxHunger;
        }

        if (hungerText != null)
        {
            hungerText.text = $"Độ no của Gióng: {Mathf.CeilToInt(currentHunger)} / {Mathf.CeilToInt(maxHunger)}";
        }
    }

    private void CreateDefaultUI()
    {
        GameObject canvasObj = new GameObject("GiongHungerCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // Đặt lên trên cùng
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();
        
        hungerUIObject = new GameObject("HungerPanel");
        hungerUIObject.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = hungerUIObject.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -30f); 
        panelRect.sizeDelta = new Vector2(400, 40);

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(hungerUIObject.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(hungerUIObject.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = new Vector2(-10, -10);
        
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Màu đỏ

        hungerSlider = hungerUIObject.AddComponent<Slider>();
        hungerSlider.interactable = false;
        hungerSlider.transition = Selectable.Transition.None;
        hungerSlider.fillRect = fillRect;
        
        GameObject textObj = new GameObject("HungerText");
        textObj.transform.SetParent(hungerUIObject.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        hungerText = textObj.AddComponent<Text>();
        hungerText.alignment = TextAnchor.MiddleCenter;
        hungerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hungerText.color = Color.white;
        hungerText.fontSize = 20;
        hungerText.fontStyle = FontStyle.Bold;

        // Ẩn UI lúc ban đầu, chỉ hiện khi gọi StartHungerDrain
        hungerUIObject.SetActive(false);
    }
}
