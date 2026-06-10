using UnityEngine;

public class GameDayManager : MonoBehaviour
{
    [Header("Day Settings")]
    public int currentDay = 1;
    public int maxDay = 7;
    public float dayDuration = 240f;

    [Header("Runtime")]
    public float remainingTime;
    public bool isDayRunning = false;

    [Header("References")]
    public PlayerHubUI playerHubUI;
    public QuestManager questManager;
    public GiongHunger giongHunger;

    private bool hasStartedCountdownThisDay = false;

    private void Start()
    {
        remainingTime = dayDuration;
        isDayRunning = false;
        hasStartedCountdownThisDay = false;

        if (giongHunger != null)
        {
            giongHunger.ResetForNewDay();
        }

        UpdateDayUI();
    }

    private void Update()
    {
        if (!isDayRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            UpdateDayUI();
            EndCurrentDay();
            return;
        }

        UpdateDayUI();
    }

    public void StartDayCountdown()
    {
        if (hasStartedCountdownThisDay)
        {
            return;
        }

        remainingTime = dayDuration;
        isDayRunning = true;
        hasStartedCountdownThisDay = true;

        Debug.Log("Bắt đầu đếm ngược ngày " + currentDay);

        UpdateDayUI();
    }

    public void StopDayCountdown()
    {
        isDayRunning = false;
    }

    private void UpdateDayUI()
    {
        if (playerHubUI != null)
        {
            playerHubUI.UpdateDayUI(currentDay, remainingTime);
        }
    }

    private void EndCurrentDay()
    {
        isDayRunning = false;

        Debug.Log("Ngày " + currentDay + " đã kết thúc.");

        if (giongHunger != null)
        {
            giongHunger.StopHungerDrain();

            if (!giongHunger.IsDaySuccess())
            {
                Debug.Log("Thất bại! Thanh đói của Gióng dưới 80%.");

                if (playerHubUI != null)
                {
                    playerHubUI.UpdateQuestUI(
                        "Thất bại",
                        "Thanh đói của Gióng đã xuống dưới 80%. Hãy chơi lại ngày này."
                    );
                }

                return;
            }
        }

        if (questManager != null)
        {
            questManager.CompleteSurviveStep();
        }

        if (currentDay < maxDay)
        {
            GoToNextDay();
        }
        else
        {
            FinishGame();
        }
    }

    private void GoToNextDay()
    {
        currentDay++;
        remainingTime = dayDuration;
        isDayRunning = false;
        hasStartedCountdownThisDay = false;

        Debug.Log("Bắt đầu ngày " + currentDay);

        UpdateDayUI();

        if (giongHunger != null)
        {
            giongHunger.ResetForNewDay();
        }

        if (questManager != null)
        {
            questManager.LoadDay(currentDay);
        }
    }
    

    private void FinishGame()
    {
        Debug.Log("Game finished after Day " + maxDay);

        if (playerHubUI != null)
        {
            playerHubUI.UpdateQuestUI(
                "Hoàn thành",
                "Thánh Gióng đã sẵn sàng xuất trận!"
            );
        }
    }
}