using UnityEngine;
using UnityEngine.InputSystem;

public class CookingPot : MonoBehaviour
{
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
            if (!playerInventory.CanCookRice())
            {
                if (interactionUI != null)
                {
                    interactionUI.Show("Cần 1 gạo + 1 nước để nấu cơm");
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

        float progress = cookingTimer / cookTime;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(progress);
        }

        if (cookingTimer >= cookTime)
        {
            FinishCooking();
        }
    }

    private void FinishCooking()
    {
        bool cooked = playerInventory.CookRice();

        if (cooked)
        {
            Debug.Log("Nấu cơm thành công!");

            QuestManager questManager = FindFirstObjectByType<QuestManager>();

            if (questManager != null)
            {
                questManager.AddProgress(QuestStepType.CookRice, "cooked_rice", 1);
            }

            if (interactionUI != null)
            {
                interactionUI.Show("Nấu cơm xong!");
            }
        }

        isCooking = false;
        cookingTimer = 0f;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
        }
    }

    private void CancelCooking()
    {
        if (isCooking)
        {
            Debug.Log("Đã dừng nấu cơm.");
        }

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
        if (other.CompareTag("Player"))
        {
            playerInventory = other.GetComponent<PlayerInventory>();
            playerInRange = true;

            if (interactionUI != null)
            {
                interactionUI.Show("Nhấn giữ E để nấu cơm");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CancelCooking();

            playerInventory = null;
            playerInRange = false;

            if (interactionUI != null)
            {
                interactionUI.Hide();
            }
        }
    }
}