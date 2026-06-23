using UnityEngine;
using UnityEngine.InputSystem;

public class FeedGiong : MonoBehaviour
{
    [Header("References")]
    public GiongHunger giongHunger;
    public InteractionUI interactionUI;
    public QuestManager questManager;

    [System.Serializable]
    public class FoodRestoreInfo
    {
        public string itemId;
        public float restoreAmount;
    }

    [Header("Food Settings")]
    public List<FoodRestoreInfo> acceptedFoods = new List<FoodRestoreInfo>
    {
        new FoodRestoreInfo { itemId = "chicken_rice", restoreAmount = 40f },
        new FoodRestoreInfo { itemId = "bamboo_rice", restoreAmount = 25f },
        new FoodRestoreInfo { itemId = "cooked_rice", restoreAmount = 15f }
    };

    public float feedTime = 1.5f;

    [Header("Quest Settings")]
    public bool reportQuestProgress = false;
    public string targetItemId = "cooked_rice";

    private bool playerInRange = false;
    private bool isFeeding = false;

    private float feedTimer = 0f;
    private PlayerInventory playerInventory;
    private PlayerHandController playerHandController;

    private void Start()
    {
        if (questManager == null)
        {
            questManager = FindFirstObjectByType<QuestManager>();
        }

        if (interactionUI != null)
        {
            interactionUI.Hide();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (!CanShowFeedInteraction())
        {
            ResetFeeding();

            if (interactionUI != null)
            {
                interactionUI.Hide();
            }

            return;
        }

        if (playerInRange && interactionUI != null && !isFeeding)
        {
            interactionUI.Show("Nhấn giữ E để đưa cơm cho mẹ Gióng");
        }

        if (!playerInRange || playerInventory == null) return;

        if (Keyboard.current.eKey.isPressed)
        {
            StartFeeding();
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            CancelFeeding();
        }
    }

    private bool CanShowFeedInteraction()
    {
        if (!playerInRange) return false;
        if (playerInventory == null) return false;

        bool hasFood = false;

        if (playerHandController != null)
        {
            ItemData heldItem = playerHandController.GetHeldItemData();
            if (heldItem != null)
            {
                foreach (var food in acceptedFoods)
                {
                    if (heldItem.itemId == food.itemId)
                    {
                        hasFood = true;
                        break;
                    }
                }
            }
        }

        if (!hasFood)
        {
            foreach (var food in acceptedFoods)
            {
                if (playerInventory.HasItem(food.itemId, 1))
                {
                    hasFood = true;
                    break;
                }
            }
        }

        if (!hasFood) return false;

        if (questManager == null)
        {
            return true;
        }

        QuestStep currentStep = questManager.GetCurrentStep();

        if (currentStep == null) return false;

        return currentStep.stepType == QuestStepType.SurviveUntilDayEnd
            || currentStep.stepType == QuestStepType.FeedGiong;
    }

    private void StartFeeding()
    {
        if (!CanShowFeedInteraction()) return;

        if (!isFeeding)
        {
            isFeeding = true;
            feedTimer = 0f;

            if (interactionUI != null)
            {
                interactionUI.Show("Đang đưa cơm cho mẹ Gióng...");
                interactionUI.SetProgress(0f);
            }
        }

        feedTimer += Time.deltaTime;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(feedTimer / feedTime);
        }

        if (feedTimer >= feedTime)
        {
            FinishFeeding();
        }
    }

    private void FinishFeeding()
    {
        string foodToConsume = null;
        float restoreAmount = 0f;
        bool consumedFromHand = false;

        if (playerHandController != null)
        {
            ItemData heldItem = playerHandController.GetHeldItemData();
            if (heldItem != null)
            {
                foreach (var food in acceptedFoods)
                {
                    if (heldItem.itemId == food.itemId)
                    {
                        foodToConsume = food.itemId;
                        restoreAmount = food.restoreAmount;
                        consumedFromHand = true;
                        break;
                    }
                }
            }
        }

        if (foodToConsume == null)
        {
            foreach (var food in acceptedFoods)
            {
                if (playerInventory.HasItem(food.itemId, 1))
                {
                    foodToConsume = food.itemId;
                    restoreAmount = food.restoreAmount;
                    break; 
                }
            }
        }

        if (foodToConsume == null)
        {
            ResetFeeding();
            return;
        }

        bool removed = false;
        if (consumedFromHand)
        {
            removed = playerHandController.TryConsumeHeldItem(1);
        }
        else
        {
            removed = playerInventory.RemoveItem(foodToConsume, 1);
        }

        if (!removed)
        {
            ResetFeeding();

            if (interactionUI != null)
            {
                interactionUI.Hide();
            }

            return;
        }

        if (giongHunger != null)
        {
            giongHunger.Feed(restoreAmount);
        }

        Debug.Log("Đã đưa " + foodToConsume + " cho mẹ Gióng.");

        TryReportQuestProgress();

        ResetFeeding();

        if (interactionUI != null)
        {
            interactionUI.Hide();
        }
    }

    private void TryReportQuestProgress()
    {
        if (!reportQuestProgress) return;
        if (questManager == null) return;

        QuestStep currentStep = questManager.GetCurrentStep();

        if (currentStep == null) return;

        if (currentStep.stepType != QuestStepType.FeedGiong) return;

        questManager.AddProgress(QuestStepType.FeedGiong, targetItemId, 1);
    }

    private void CancelFeeding()
    {
        if (!isFeeding) return;

        ResetFeeding();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);

            if (CanShowFeedInteraction())
            {
                interactionUI.Show("Nhấn giữ E để đưa cơm cho mẹ Gióng");
            }
            else
            {
                interactionUI.Hide();
            }
        }
    }

    private void ResetFeeding()
    {
        isFeeding = false;
        feedTimer = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInventory = other.GetComponent<PlayerInventory>();
        playerHandController = other.GetComponent<PlayerHandController>();
        playerInRange = true;

        if (CanShowFeedInteraction() && interactionUI != null)
        {
            interactionUI.Show("Nhấn giữ E để đưa cơm cho mẹ Gióng");
            interactionUI.SetProgress(0f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInventory = null;
        playerHandController = null;
        playerInRange = false;

        ResetFeeding();

        if (interactionUI != null)
        {
            interactionUI.Hide();
        }
    }
}