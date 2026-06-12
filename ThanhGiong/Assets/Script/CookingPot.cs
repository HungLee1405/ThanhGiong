using UnityEngine;
using UnityEngine.InputSystem;

public class CookingPot : MonoBehaviour
{
    [Header("Recipe")]
    public ItemData riceItem;
    public ItemData waterItem;
    public ItemData cookedRiceItem;

    public int riceCost = 1;
    public int waterCost = 1;
    public int cookedRiceAmount = 1;

    [Header("Cooking Settings")]
    public float cookTime = 8f;

    [Header("Interaction UI")]
    public InteractionUI interactionUI;

    private bool playerInRange = false;
    private bool isCooking = false;

    private float cookingTimer = 0f;
    private PlayerInventory playerInventory;

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (!CanInteract())
        {
            if (isCooking)
            {
                CancelCooking();
            }

            return;
        }

        if (Keyboard.current.eKey.isPressed)
        {
            TryCook();
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            CancelCooking();
        }
    }

    private bool CanInteract()
    {
        if (!playerInRange) return false;
        if (playerInventory == null) return false;

        return true;
    }

    private void TryCook()
    {
        if (!CanInteract()) return;

        if (!isCooking)
        {
            if (!CanCook())
            {
                if (interactionUI != null)
                {
                    interactionUI.Show("Cần gạo và nước để nấu cơm");
                    interactionUI.SetProgress(0f);
                }

                return;
            }

            isCooking = true;
            cookingTimer = 0f;

            if (interactionUI != null)
            {
                interactionUI.Show("Đang nấu cơm...");
            }
        }

        cookingTimer += Time.deltaTime;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(cookingTimer / cookTime);
        }

        if (cookingTimer >= cookTime)
        {
            FinishCooking();
        }
    }

    private bool CanCook()
    {
        if (playerInventory == null) return false;
        if (riceItem == null || waterItem == null || cookedRiceItem == null) return false;

        return playerInventory.HasItem(riceItem.itemId, riceCost)
            && playerInventory.HasItem(waterItem.itemId, waterCost);
    }

    private void FinishCooking()
    {
        if (!CanInteract())
        {
            ResetCooking();
            return;
        }

        if (!CanCook())
        {
            CancelCooking();
            return;
        }

        bool removedRice = playerInventory.RemoveItem(riceItem.itemId, riceCost);
        bool removedWater = playerInventory.RemoveItem(waterItem.itemId, waterCost);

        if (!removedRice || !removedWater)
        {
            Debug.LogWarning("Không thể trừ nguyên liệu nấu cơm.");
            CancelCooking();
            return;
        }

        bool addedCookedRice = playerInventory.AddItem(cookedRiceItem, cookedRiceAmount);

        if (!addedCookedRice)
        {
            Debug.LogWarning("Inventory đầy, không thể nhận cơm.");

            // Trả lại nguyên liệu nếu nấu xong nhưng không thêm được cơm.
            playerInventory.AddItem(riceItem, riceCost);
            playerInventory.AddItem(waterItem, waterCost);

            CancelCooking();

            if (interactionUI != null)
            {
                interactionUI.Show("Inventory đầy");
            }

            return;
        }

        Debug.Log("Nấu cơm thành công!");

        ReportQuestProgress();

        ResetCooking();

        if (interactionUI != null)
        {
            interactionUI.Show("Nấu cơm xong!");
            interactionUI.SetProgress(0f);
        }
    }

    private void ReportQuestProgress()
    {
        QuestManager questManager = FindFirstObjectByType<QuestManager>();

        if (questManager != null)
        {
            questManager.AddProgress(QuestStepType.CookRice, "cooked_rice", 1);
        }
    }

    private void CancelCooking()
    {
        ResetCooking();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);

            if (CanInteract())
            {
                interactionUI.Show("Nhấn giữ E để nấu cơm");
            }
            else
            {
                interactionUI.Hide();
            }
        }
    }

    private void ResetCooking()
    {
        isCooking = false;
        cookingTimer = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        BindPlayer(other);

        playerInRange = true;

        if (interactionUI != null)
        {
            interactionUI.Show("Nhấn giữ E để nấu cơm");
            interactionUI.SetProgress(0f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (playerInventory == null)
        {
            BindPlayer(other);
        }

        playerInRange = true;

        if (interactionUI != null && !isCooking)
        {
            interactionUI.Show("Nhấn giữ E để nấu cơm");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInventory = null;
        playerInRange = false;

        CancelCooking();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
            interactionUI.Hide();
        }
    }

    private void BindPlayer(Collider other)
    {
        playerInventory = other.GetComponent<PlayerInventory>();

        if (playerInventory == null)
        {
            playerInventory = other.GetComponentInParent<PlayerInventory>();
        }

        if (playerInventory == null)
        {
            playerInventory = other.GetComponentInChildren<PlayerInventory>();
        }
    }
}