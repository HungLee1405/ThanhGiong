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

    private float decreaseTimer = 0f;

    private void Start()
    {
        ResetHunger();
        StopHungerDrain();

        UpdateUI();
    }

    private void Update()
    {
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
    }
}