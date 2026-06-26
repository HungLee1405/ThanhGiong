using UnityEngine;
using System.Collections;
using Unity.Netcode;

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

    [Header("Ending")]
    [SerializeField] private string endingSceneName = "EndingScene";
    [SerializeField] private float endingSceneDelay = 1.5f;

    [Header("References")]
    public PlayerHubUI playerHubUI;
    public QuestManager questManager;
    public GiongHunger giongHunger;

    private bool hasStartedCountdownThisDay = false;
    private float networkSyncTimer;
    private bool endingStarted;

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
        if (NetworkLobbyCoordinator.IsOnlineLobbyActive) return;
        if (!CanSimulateSharedWorld()) return;

        networkSyncTimer += Time.deltaTime;
        if (networkSyncTimer >= 0.25f)
        {
            networkSyncTimer = 0f;
            SharedQuestNetwork.PublishWorldState();
        }

        if (!isDayRunning) return;
        if (isTransitioningDay) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0)
        {
            remainingTime = 0;
            UpdateDayUI();
            
            bool success = true;
            if (giongHunger != null && !giongHunger.IsDaySuccess())
            {
                Debug.Log("Gióng chưa đủ no! Ngày thất bại.");
                success = false;
            }

            if (questManager != null)
            {
                if (!questManager.CompleteSurviveStep())
                {
                    success = false;
                }
            }

            if (!success)
            {
                isDayRunning = false;
                if (playerHubUI != null)
                {
                    playerHubUI.UpdateQuestUI("Thất bại", "Không đạt điều kiện qua ngày. Hãy nạp lại Scene hoặc Load Game!");
                }
                if (GameOverUI.Instance != null)
                {
                    GameOverUI.Instance.ShowGameOver("Thất bại", "Không đạt điều kiện qua ngày. Hãy thử lại!", false);
                }
                return;
            }

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
        SharedQuestNetwork.PublishWorldState();
    }

    public void StopDayCountdown()
    {
        isDayRunning = false;
        SharedQuestNetwork.PublishWorldState();
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
        SharedQuestNetwork.PublishWorldState();
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
        if (endingStarted)
            return;

        endingStarted = true;
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

        StartCoroutine(LoadEndingSceneRoutine());
    }

    private IEnumerator LoadEndingSceneRoutine()
    {
        if (endingSceneDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(endingSceneDelay);
        }

        SharedQuestNetwork.LoadEndingSceneForAll(endingSceneName);
    }

    public void ApplySharedState(int day, float timeRemaining, bool running, bool transitioning)
    {
        currentDay = Mathf.Clamp(day, 1, maxDay);
        remainingTime = Mathf.Clamp(timeRemaining, 0f, dayDuration);
        isDayRunning = running;
        isTransitioningDay = transitioning;
        UpdateDayUI();
    }

    private static bool CanSimulateSharedWorld()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager == null || !manager.IsListening || manager.IsServer;
    }
}
