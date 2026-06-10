using UnityEngine;
using UnityEngine.InputSystem;

public enum ResourceType
{
    Rice,
    Water
}

public class ResourcePickup : MonoBehaviour
{
    [Header("Resource Settings")]
    public ResourceType resourceType;
    public int amount = 1;

    [Header("Interaction Settings")]
    public float collectTime = 2f;
    public InteractionUI interactionUI;

    private bool playerInRange = false;
    private bool isCollecting = false;

    private float collectTimer = 0f;
    private PlayerInventory playerInventory;

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (!playerInRange || playerInventory == null) return;

        if (Keyboard.current.eKey.isPressed)
        {
            StartCollecting();
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            CancelCollecting();
        }
    }

    private void StartCollecting()
    {
        if (!isCollecting)
        {
            isCollecting = true;
            collectTimer = 0f;
        }

        collectTimer += Time.deltaTime;

        float progress = collectTimer / collectTime;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(progress);
        }

        if (collectTimer >= collectTime)
        {
            FinishCollecting();
        }
    }

    private void FinishCollecting()
    {
        bool success = false;
        QuestStepType questStepType = QuestStepType.CollectWater;
        string targetId = "";

        switch (resourceType)
        {
            case ResourceType.Rice:
                success = playerInventory.AddRice(amount);
                questStepType = QuestStepType.CollectRice;
                targetId = "rice";
                break;

            case ResourceType.Water:
                success = playerInventory.AddWater(amount);
                questStepType = QuestStepType.CollectWater;
                targetId = "water";
                break;
        }

        if (success)
        {
            Debug.Log("Đã lấy " + resourceType);

            QuestManager questManager = FindFirstObjectByType<QuestManager>();

            if (questManager != null)
            {
                questManager.AddProgress(questStepType, targetId, amount);
            }
        }
        else
        {
            Debug.Log("Không thể lấy thêm " + resourceType);
        }

        isCollecting = false;
        collectTimer = 0f;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
        }
    }

    private void CancelCollecting()
    {
        isCollecting = false;
        collectTimer = 0f;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
        }
    }

    private string GetInteractionMessage()
    {
        switch (resourceType)
        {
            case ResourceType.Rice:
                return "Nhấn giữ E để lấy gạo";

            case ResourceType.Water:
                return "Nhấn giữ E để lấy nước";

            default:
                return "Nhấn giữ E để tương tác";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInventory = other.GetComponent<PlayerInventory>();
            playerInRange = true;

            if (interactionUI != null)
            {
                interactionUI.Show(GetInteractionMessage());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CancelCollecting();

            playerInRange = false;
            playerInventory = null;

            if (interactionUI != null)
            {
                interactionUI.Hide();
            }
        }
    }
}