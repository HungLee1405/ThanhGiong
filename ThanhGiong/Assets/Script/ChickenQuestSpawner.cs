using UnityEngine;

public class ChickenQuestSpawner : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private ChickenController[] chickens;
    [SerializeField] private bool hideWhenQuestInactive = true;

    private void Start()
    {
        if (questManager == null)
        {
            questManager = FindFirstObjectByType<QuestManager>();
        }

        if (questManager != null)
        {
            questManager.OnActiveQuestsChanged += Refresh;
            questManager.OnQuestStepChanged += Refresh;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (questManager != null)
        {
            questManager.OnActiveQuestsChanged -= Refresh;
            questManager.OnQuestStepChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (questManager == null) return;

        bool isCatchQuestActive = questManager.IsStepActive(QuestStepType.CatchChicken);

        foreach (var chicken in chickens)
        {
            if (chicken == null) continue;

            if (isCatchQuestActive)
            {
                // Bật các gà chưa Delivered
                if (!chicken.isDelivered)
                {
                    if (!chicken.gameObject.activeSelf)
                    {
                        chicken.gameObject.SetActive(true);
                        chicken.ResetToSpawn();
                    }
                }
                else
                {
                    // Gà đã Delivered thì giữ visual tắt ngoài map
                    chicken.gameObject.SetActive(false);
                }
            }
            else
            {
                // Khi quest inactive (chưa nhận, hoặc đã hoàn thành)
                // Tắt các gà chưa được giao ngoài thế giới
                if (hideWhenQuestInactive && !chicken.isDelivered)
                {
                    chicken.gameObject.SetActive(false);
                }
            }
        }
    }
}
