using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

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

    [Header("Menu System")]
    public CookingMenuUI cookingMenuUI;
    public bool useDataDrivenMenu = true;
    private CookingRecipe currentRecipe;

    private ItemData pendingOutputItem;
    private int pendingOutputAmount;
    private string pendingRecipeId;

    private float cookingTimer = 0f;
    private PlayerInventory playerInventory;

    public bool IsCooking => isCooking;
    public float CookingProgress => isCooking ? cookingTimer / (currentRecipe != null ? currentRecipe.cookTime : cookTime) : 0f;
    public bool HasPendingOutput => pendingOutputItem != null;
    public ItemData PendingOutputItem => pendingOutputItem;
    public int PendingOutputAmount => pendingOutputAmount;
    public string CurrentRecipeName => currentRecipe != null ? currentRecipe.displayName : "Cơm";

    private void Update()
    {
        if (PauseMenuManager.isPaused) return;
        if (NetworkLobbyCoordinator.IsOnlineLobbyActive) return;
        if (Keyboard.current == null) return;

        if (!CanInteract())
        {
            if (isCooking)
            {
                CancelCooking();
            }

            return;
        }

        if (!isCooking)
        {
            if (pendingOutputItem != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    TryTakePendingOutput();
                }
                return;
            }

            if (useDataDrivenMenu)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    if (cookingMenuUI != null)
                    {
                        cookingMenuUI.OpenMenu(this, playerInventory);
                    }
                }
            }
            else
            {
                if (Keyboard.current.eKey.isPressed)
                {
                    TryCook();
                }

                if (Keyboard.current.eKey.wasReleasedThisFrame)
                {
                    CancelCooking();
                }
            }
        }
        else
        {
            // Update cooking timer
            cookingTimer += Time.deltaTime;

            if (interactionUI != null)
            {
                float targetTime = currentRecipe != null ? currentRecipe.cookTime : cookTime;
                interactionUI.SetProgress(cookingTimer / targetTime);
            }

            if (cookingTimer >= (currentRecipe != null ? currentRecipe.cookTime : cookTime))
            {
                FinishCooking();
            }
        }
    }

    private bool CanInteract()
    {
        if (!playerInRange) return false;
        if (playerInventory == null) return false;

        return true;
    }

    public void StartDataDrivenCooking(CookingRecipe recipe)
    {
        if (isCooking || pendingOutputItem != null) return;

        foreach (var ing in recipe.ingredients)
        {
            playerInventory.RemoveItem(ing.item.itemId, ing.amount);
        }

        currentRecipe = recipe;
        isCooking = true;
        cookingTimer = 0f;

        if (interactionUI != null)
        {
            interactionUI.Show("Đang nấu " + recipe.displayName + "...");
        }
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

        if (useDataDrivenMenu && currentRecipe != null)
        {
            bool addedOutput = playerInventory.AddItem(currentRecipe.outputItem, currentRecipe.outputAmount);
            if (!addedOutput)
            {
                pendingOutputItem = currentRecipe.outputItem;
                pendingOutputAmount = currentRecipe.outputAmount;
                pendingRecipeId = currentRecipe.recipeId;

                ResetCooking();
                if (interactionUI != null) interactionUI.Show("Thành phẩm đang chờ (Nhấn E để lấy)");
                return;
            }
            
            ReportQuestProgress(currentRecipe.recipeId);
            ResetCooking();
        }
        else
        {
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

            ReportQuestProgress("cook_rice_legacy");
            bool addedCookedRice = playerInventory.AddItem(cookedRiceItem, cookedRiceAmount);

            if (!addedCookedRice)
            {
                pendingOutputItem = cookedRiceItem;
                pendingOutputAmount = cookedRiceAmount;
                ResetCooking();
                if (interactionUI != null) interactionUI.Show("Thành phẩm đang chờ (Nhấn E để lấy)");
                return;
            }
            
            ResetCooking();
        }

        Debug.Log("Nấu ăn thành công!");

        if (interactionUI != null)
        {
            interactionUI.Show("Nấu xong!");
            interactionUI.SetProgress(0f);
        }
    }

    public void TryTakePendingOutput()
    {
        if (playerInventory == null) return;
        
        bool added = playerInventory.AddItem(pendingOutputItem, pendingOutputAmount);
        if (added)
        {
            // Do NOT report progress here anymore
            pendingOutputItem = null;
            pendingOutputAmount = 0;
            pendingRecipeId = null;

            if (interactionUI != null)
            {
                interactionUI.Show(useDataDrivenMenu ? "Nhấn E mở Menu nấu ăn" : "Nhấn giữ E để nấu cơm");
            }
        }
        else
        {
            if (interactionUI != null) interactionUI.Show("Túi đồ đầy!");
        }
    }

    private void ReportQuestProgress(string recipeId)
    {
        QuestManager questManager = FindFirstObjectByType<QuestManager>();

        if (questManager != null)
        {
            // Default backward compatibility
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

    // Kiểm tra xem collider có phải là local player không.
    // Trong offline: chấp nhận mọi Player. Trong online: chỉ chấp nhận local owner.
    private bool IsLocalPlayer(Collider other)
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
            return true;

        PlayerMovement movement = other.GetComponent<PlayerMovement>();
        if (movement == null) movement = other.GetComponentInParent<PlayerMovement>();
        if (movement == null) movement = other.GetComponentInChildren<PlayerMovement>();

        if (movement != null && movement.IsSpawned)
            return movement.IsOwner;

        PlayerHandController hand = other.GetComponent<PlayerHandController>();
        if (hand == null) hand = other.GetComponentInParent<PlayerHandController>();
        if (hand == null) hand = other.GetComponentInChildren<PlayerHandController>();

        if (hand != null && hand.IsSpawned)
            return hand.IsOwner;

        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!IsLocalPlayer(other)) return;

        BindPlayer(other);

        playerInRange = true;

        if (interactionUI != null)
        {
            interactionUI.Show(useDataDrivenMenu ? "Nhấn E mở Menu nấu ăn" : "Nhấn giữ E để nấu cơm");
            interactionUI.SetProgress(0f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!IsLocalPlayer(other)) return;

        if (playerInventory == null)
        {
            BindPlayer(other);
        }

        playerInRange = true;

        if (interactionUI != null && !isCooking)
        {
            if (pendingOutputItem != null)
            {
                interactionUI.Show("Thành phẩm đang chờ (Nhấn E để lấy)");
            }
            else
            {
                interactionUI.Show(useDataDrivenMenu ? "Nhấn E mở Menu nấu ăn" : "Nhấn giữ E để nấu cơm");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!IsLocalPlayer(other)) return;

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
