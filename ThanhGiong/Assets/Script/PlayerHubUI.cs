using UnityEngine;
using TMPro;

public class PlayerHubUI : MonoBehaviour
{
    [Header("Day UI")]
    public TMP_Text dayText;

    [Header("Quest UI")]
    public TMP_Text questText;
    public TMP_Text questNameText;

    public void UpdateDayUI(int currentDay, float remainingTime)
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);

        dayText.text = "Day " + currentDay + "\n" + minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    public void UpdateQuestUI(string questName, string questDescription)
    {
        if (questNameText != null)
        {
            questNameText.text = questName;
        }

        if (questText != null)
        {
            questText.text = questDescription;
        }
    }

    public void ClearQuestUI()
    {
        if (questNameText != null)
        {
            questNameText.text = "Quest:";
        }

        if (questText != null)
        {
            questText.text = "Không có nhiệm vụ hiện tại.";
        }
    }
}