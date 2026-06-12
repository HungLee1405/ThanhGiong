using UnityEngine;
using UnityEngine.InputSystem;

public class ResourcePickup : MonoBehaviour
{
    [Header("Item Settings")]
    public ItemData itemData;
    public int amount = 1;

    [Header("Interaction Settings")]
    public float collectTime = 2f;
    public InteractionUI interactionUI;

    [Header("Quest Settings")]
    public bool reportQuestProgress = true;

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
        if (itemData == null) return;

        if (!isCollecting)
        {
            isCollecting = true;
            collectTimer = 0f;
        }

        collectTimer += Time.deltaTime;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(collectTimer / collectTime);
        }

        if (collectTimer >= collectTime)
        {
            FinishCollecting();
        }
    }

    private void FinishCollecting()
    {
        if (itemData == null)
        {
            ResetCollecting();
            return;
        }

        bool success = playerInventory.AddItem(itemData, amount);

        if (success)
        {
            Debug.Log("Đã lấy: " + itemData.itemName);

            if (reportQuestProgress)
            {
                ReportQuestProgress();
            }
        }
        else
        {
            Debug.Log("Không thể lấy thêm: " + itemData.itemName);
        }

        ResetCollecting();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
        }
    }

    private void ReportQuestProgress()
    {
        QuestManager questManager = FindFirstObjectByType<QuestManager>();

        if (questManager == null || itemData == null) return;

        switch (itemData.itemId)
        {
            case "water":
                questManager.AddProgress(QuestStepType.CollectWater, "water", amount);
                break;

            case "rice":
                questManager.AddProgress(QuestStepType.CollectRice, "rice", amount);
                break;

            case "iron_ore":
                questManager.AddProgress(QuestStepType.CollectIron, "iron_ore", amount);
                break;

            case "bamboo":
                questManager.AddProgress(QuestStepType.CollectBamboo, "bamboo", amount);
                break;

            case "chicken":
                questManager.AddProgress(QuestStepType.CatchChicken, "chicken", amount);
                break;
        }
    }

    private void CancelCollecting()
    {
        ResetCollecting();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
        }
    }

    private void ResetCollecting()
    {
        isCollecting = false;
        collectTimer = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInventory = other.GetComponent<PlayerInventory>();
        playerInRange = true;

        if (interactionUI != null && itemData != null)
        {
            interactionUI.Show("Nhấn giữ E để lấy " + itemData.itemName);
            interactionUI.SetProgress(0f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInventory = null;
        playerInRange = false;

        ResetCollecting();

        if (interactionUI != null)
        {
            interactionUI.Hide();
        }
    }
}