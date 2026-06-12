using UnityEngine;
using UnityEngine.InputSystem;

public class FeedGiong : MonoBehaviour
{
    [Header("References")]
    public GiongHunger giongHunger;
    public InteractionUI interactionUI;
    public QuestManager questManager;

    [Header("Food Item")]
    public ItemData cookedRiceItem;

    [Header("Feed Settings")]
    public float feedTime = 1.5f;
    public float cookedRiceRestoreAmount = 15f;

    [Header("Quest Settings")]
    public bool reportQuestProgress = false;
    public string targetItemId = "cooked_rice";

    private bool playerInRange = false;
    private bool isFeeding = false;

    private float feedTimer = 0f;
    private PlayerInventory playerInventory;

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
        if (cookedRiceItem == null) return false;

        if (!playerInventory.HasItem(cookedRiceItem.itemId, 1)) return false;

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
        if (cookedRiceItem == null)
        {
            ResetFeeding();
            return;
        }

        bool removed = playerInventory.RemoveItem(cookedRiceItem.itemId, 1);

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
            giongHunger.Feed(cookedRiceRestoreAmount);
        }

        Debug.Log("Đã đưa cơm cho mẹ Gióng.");

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
        playerInRange = false;

        ResetFeeding();

        if (interactionUI != null)
        {
            interactionUI.Hide();
        }
    }
}