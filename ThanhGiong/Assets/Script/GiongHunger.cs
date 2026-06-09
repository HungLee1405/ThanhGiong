using UnityEngine;
using UnityEngine.UI;

public class GiongHunger : MonoBehaviour
{
    [Header("Hunger Settings")]
    public float hunger = 100f;
    public float minRequiredHunger = 80f;
    public float decreaseRate = 1f;
    public float decreaseInterval = 3f;

    [Header("UI")]
    public Slider hungerSlider;

    private float timer = 0f;

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= decreaseInterval)
        {
            timer = 0f;
            DecreaseHunger(decreaseRate);
        }
    }

    public void DecreaseHunger(float amount)
    {
        hunger -= amount;
        hunger = Mathf.Clamp(hunger, 0f, 100f);

        UpdateUI();

        if (hunger <= 0)
        {
            Debug.Log("Gióng quá đói! Game Over.");
        }
    }

    public void Feed(float amount)
    {
        hunger += amount;
        hunger = Mathf.Clamp(hunger, 0f, 100f);

        UpdateUI();

        Debug.Log("Đã cho Gióng ăn. Thanh đói: " + hunger);
    }

    private void UpdateUI()
    {
        if (hungerSlider != null)
        {
            hungerSlider.value = hunger / 100f;
        }
    }

    public bool IsDaySuccess()
    {
        return hunger >= minRequiredHunger;
    }
}