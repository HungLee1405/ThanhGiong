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
        if (!playerInRange || playerInventory == null) return;

        if (Keyboard.current.eKey.isPressed)
        {
            TryCook();
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            CancelCooking();
        }
    }

    private void TryCook()
    {
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
        if (riceItem == null || waterItem == null || cookedRiceItem == null) return false;

        return playerInventory.HasItem(riceItem.itemId, riceCost)
            && playerInventory.HasItem(waterItem.itemId, waterCost);
    }

    private void FinishCooking()
    {
        if (!CanCook())
        {
            CancelCooking();
            return;
        }

        playerInventory.RemoveItem(riceItem.itemId, riceCost);
        playerInventory.RemoveItem(waterItem.itemId, waterCost);
        playerInventory.AddItem(cookedRiceItem, cookedRiceAmount);

        Debug.Log("Nấu cơm thành công!");

        QuestManager questManager = FindFirstObjectByType<QuestManager>();

        if (questManager != null)
        {
            questManager.AddProgress(QuestStepType.CookRice, "cooked_rice", 1);
        }

        isCooking = false;
        cookingTimer = 0f;

        if (interactionUI != null)
        {
            interactionUI.Show("Nấu cơm xong!");
            interactionUI.SetProgress(0f);
        }
    }

    private void CancelCooking()
    {
        isCooking = false;
        cookingTimer = 0f;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);

            if (playerInRange)
            {
                interactionUI.Show("Nhấn giữ E để nấu cơm");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInventory = other.GetComponent<PlayerInventory>();
        playerInRange = true;

        if (interactionUI != null)
        {
            interactionUI.Show("Nhấn giữ E để nấu cơm");
            interactionUI.SetProgress(0f);
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
            interactionUI.Hide();
        }
    }
}