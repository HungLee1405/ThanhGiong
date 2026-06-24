using UnityEngine;

public enum QuestMarkerType
{
    Any,
    NPC,
    Item
}

public class QuestMarker : MonoBehaviour
{
    [Tooltip("Loại mục tiêu của marker này (NPC, Item, Any)")]
    public QuestMarkerType markerType = QuestMarkerType.Any;

    [Tooltip("ID của NPC (vd: village_elder) hoặc Item (vd: water) tương ứng với mục tiêu nhiệm vụ")]
    public string targetId;
    
    [Tooltip("Object hiệu ứng chỉ đường (mũi tên, vòng sáng...) sẽ được bật/tắt")]
    public GameObject markerVisual;

    private QuestManager questManager;

    private void Start()
    {
        questManager = FindFirstObjectByType<QuestManager>();
        if (questManager != null)
        {
            questManager.OnQuestStepChanged += RefreshMarker;
            RefreshMarker();
        }
    }

    private void OnDestroy()
    {
        if (questManager != null)
        {
            questManager.OnQuestStepChanged -= RefreshMarker;
        }
    }

    private void RefreshMarker()
    {
        if (questManager == null || markerVisual == null) return;

        QuestStep currentStep = questManager.GetCurrentStep();
        if (currentStep == null || currentStep.IsCompleted() || questManager.isDayQuestCompleted)
        {
            if (markerVisual.activeSelf)
                markerVisual.SetActive(false);
            return;
        }

        bool shouldShow = false;

        // Kiểm tra loại mục tiêu để không bị nhầm lẫn ID
        if ((markerType == QuestMarkerType.Any || markerType == QuestMarkerType.NPC) && 
            !string.IsNullOrEmpty(currentStep.targetNPCId) && currentStep.targetNPCId == targetId)
        {
            shouldShow = true;
        }
        else if ((markerType == QuestMarkerType.Any || markerType == QuestMarkerType.Item) && 
                 !string.IsNullOrEmpty(currentStep.targetItemId) && currentStep.targetItemId == targetId)
        {
            shouldShow = true;
        }

        if (markerVisual.activeSelf != shouldShow)
        {
            markerVisual.SetActive(shouldShow);
        }
    }
}
