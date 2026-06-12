using UnityEngine;
using System.Collections;

public class GameDayManager : MonoBehaviour
{
    [Header("Day Settings")]
    public int currentDay = 1;
    public int maxDay = 7;
    public float dayDuration = 240f;

    [Header("Runtime")]
    public float remainingTime;
    public bool isDayRunning = false;
    public bool isTransitioningDay = false;

    [Header("Transition")]
    public DayTransitionUI dayTransitionUI;

    [Header("References")]
    public PlayerHubUI playerHubUI;
    public QuestManager questManager;
    public GiongHunger giongHunger;

    private bool hasStartedCountdownThisDay = false;

    private void Start()
    {
        remainingTime = dayDuration;
        isDayRunning = false;
        isTransitioningDay = false;
        hasStartedCountdownThisDay = false;

        ResetGiongHunger();
        UpdateDayUI();
        LoadQuestForCurrentDay();
    }

    private void Update()
    {
        if (!isDayRunning) return;
        if (isTransitioningDay) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0)
        {
            remainingTime = 0;
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

    public void EndCurrentDay()
    {
        if (isTransitioningDay) return;

        StartCoroutine(EndCurrentDayRoutine());
    }

    private IEnumerator EndCurrentDayRoutine()
    {
        isTransitioningDay = true;
        isDayRunning = false;

        if (currentDay >= maxDay)
        {
            FinishGame();
            isTransitioningDay = false;
            yield break;
        }

        int nextDay = currentDay + 1;

        if (dayTransitionUI != null)
        {
            yield return StartCoroutine(dayTransitionUI.PlayDayTransition(nextDay));
        }

        GoToNextDay(nextDay);

        isTransitioningDay = false;
    }

    private void GoToNextDay(int nextDay)
    {
        currentDay = nextDay;
        remainingTime = dayDuration;

        isDayRunning = false;
        hasStartedCountdownThisDay = false;

        Debug.Log("Bắt đầu ngày " + currentDay);

        ResetGiongHunger();
        UpdateDayUI();
        LoadQuestForCurrentDay();
    }

    private void UpdateDayUI()
    {
        if (playerHubUI != null)
        {
            playerHubUI.UpdateDayUI(currentDay, remainingTime);
        }
    }

    private void LoadQuestForCurrentDay()
    {
        if (questManager != null)
        {
            questManager.LoadDay(currentDay);
        }
    }

    private void ResetGiongHunger()
    {
        if (giongHunger != null)
        {
            giongHunger.ResetForNewDay();
        }
    }

    private void FinishGame()
    {
        Debug.Log("Game finished after Day " + maxDay);

        isDayRunning = false;
        isTransitioningDay = false;

        if (playerHubUI != null)
        {
            playerHubUI.UpdateQuestUI(
                "Hoàn thành",
                "Thánh Gióng đã sẵn sàng xuất trận!"
            );
        }
    }
}