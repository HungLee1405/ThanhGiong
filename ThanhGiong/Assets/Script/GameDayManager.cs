using UnityEngine;


public class GameDayManager : MonoBehaviour
{
    [Header("Day Settings")]
    public int currentDay = 1;
    public int maxDay = 7;
    public float dayDuration = 240f;

    [Header("Runtime")]
    public float remainingTime;
    public bool isDayRunning = true;

    [Header("References")]
    public PlayerHubUI playerHubUI;
    public QuestManager questManager;

    private void Start()
    {
        remainingTime = dayDuration;

        UpdateDayUI();
        UpdateQuestByDay();
    }

    private void Update()
    {
        if (!isDayRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0)
        {
            remainingTime = 0;
            EndCurrentDay();
        }

        UpdateDayUI();
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

        Debug.Log("Day " + currentDay + " ended.");

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
        isDayRunning = true;

        Debug.Log("Start Day " + currentDay);

        UpdateDayUI();

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

    private void UpdateQuestByDay()
    {
        if (playerHubUI == null) return;

        switch (currentDay)
        {
            case 1:
                playerHubUI.UpdateQuestUI(
                    "Nhiệm vụ Ngày 1",
                    "- Nói chuyện với Già Làng\n- Học cách lấy gạo và nước\n- Chuẩn bị nuôi Thánh Gióng"
                );
                break;

            case 2:
                playerHubUI.UpdateQuestUI(
                    "Nhiệm vụ Ngày 2",
                    "- Tiếp tục nấu cơm trắng\n- Giữ thanh đói của Gióng trên 80%"
                );
                break;

            case 3:
                playerHubUI.UpdateQuestUI(
                    "Nhiệm vụ Ngày 3",
                    "- Bắt gà xổng chuồng\n- Mở khóa món Cơm Gà\n- Tiếp tục nuôi Thánh Gióng"
                );
                break;

            case 4:
                playerHubUI.UpdateQuestUI(
                    "Nhiệm vụ Ngày 4",
                    "- Khai thác quặng sắt\n- Tích trữ sắt cho Thợ Rèn\n- Vẫn phải giữ Gióng no bụng"
                );
                break;

            case 5:
                playerHubUI.UpdateQuestUI(
                    "Nhiệm vụ Ngày 5",
                    "- Thu hoạch tre\n- Mở khóa món Cơm Lam\n- Chuẩn bị vật liệu phòng thủ"
                );
                break;

            case 6:
                playerHubUI.UpdateQuestUI(
                    "Nhiệm vụ Ngày 6",
                    "- Tổng lực dự trữ gạo, nước, sắt và tre\n- Giữ thanh đói của Gióng trên 80%"
                );
                break;

            case 7:
                playerHubUI.UpdateQuestUI(
                    "Nhiệm vụ Ngày 7",
                    "- Hỗ trợ Thợ Rèn\n- Làm nguội vũ khí bằng nước\n- Cho Gióng ăn liên tục trước giờ xuất trận"
                );
                break;
        }
    }
}