using UnityEngine;
using UnityEngine.InputSystem;

public class FeedGiong : MonoBehaviour
{
    [Header("References")]
    public GiongHunger giongHunger;
    public InteractionUI interactionUI;

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

    private void Update()
    {
        if (Keyboard.current == null) return;
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

    private void StartFeeding()
    {
        if (!isFeeding)
        {
            if (playerInventory.cookedRice <= 0)
            {
                if (interactionUI != null)
                {
                    interactionUI.Show("Bạn chưa có cơm để đưa cho mẹ Gióng");
                    interactionUI.SetProgress(0f);
                }

                return;
            }

            isFeeding = true;
            feedTimer = 0f;

            if (interactionUI != null)
            {
                interactionUI.Show("Đang đưa cơm cho mẹ Gióng...");
                interactionUI.SetProgress(0f);
            }
        }

        feedTimer += Time.deltaTime;

        float progress = feedTimer / feedTime;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(progress);
        }

        if (feedTimer >= feedTime)
        {
            FinishFeeding();
        }
    }

    private void FinishFeeding()
    {
        bool hasFood = playerInventory.UseCookedRice();

        if (!hasFood)
        {
            ResetFeeding();

            if (interactionUI != null)
            {
                interactionUI.Show("Bạn chưa có cơm để đưa cho mẹ Gióng");
                interactionUI.SetProgress(0f);
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
            interactionUI.Show("Đã đưa cơm cho mẹ Gióng");
            interactionUI.SetProgress(0f);
        }
    }

    private void TryReportQuestProgress()
    {
        if (!reportQuestProgress) return;

        QuestManager questManager = FindFirstObjectByType<QuestManager>();

        if (questManager == null) return;

        QuestStep currentStep = questManager.GetCurrentStep();

        if (currentStep == null) return;

        if (currentStep.stepType != QuestStepType.FeedGiong)
        {
            return;
        }

        questManager.AddProgress(QuestStepType.FeedGiong, targetItemId, 1);
    }

    private void CancelFeeding()
    {
        if (!isFeeding) return;

        ResetFeeding();

        if (interactionUI != null)
        {
            interactionUI.Show("Nhấn giữ E để đưa cơm cho mẹ Gióng");
            interactionUI.SetProgress(0f);
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

        if (interactionUI != null)
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