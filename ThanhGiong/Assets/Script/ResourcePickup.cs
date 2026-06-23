using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ResourcePickup : MonoBehaviour
{
    [Header("Item Settings")]
    public ItemData itemData;
    public int amount = 1;

    [Header("Collect Settings")]
    public bool destroyAfterCollect = false;

    [Header("Respawn Settings")]
    public bool respawnAfterCollect = false;
    public float respawnTime = 30f;

    [Header("Anti Exploit")]
    public bool requirePlayerExitAfterRespawn = true;

    [Header("Tool Requirement")]
    public bool requireTool = false;
    public string requiredToolItemId;
    public string missingToolMessage = "Bạn cần cầm công cụ phù hợp";

    [Header("Interaction Settings")]
    public float collectTime = 2f;
    public InteractionUI interactionUI;

    [Header("Quest Settings")]
    public bool reportQuestProgress = true;

    private bool playerInRange = false;
    private bool isCollecting = false;
    private bool isRespawning = false;

    // Khi resource hồi lại mà player vẫn đứng trong vùng,
    // khóa tương tác cho tới khi player bước ra rồi vào lại.
    private bool lockedUntilPlayerExit = false;

    private float collectTimer = 0f;
    private Coroutine respawnCoroutine;

    private PlayerInventory playerInventory;
    private PlayerHandController playerHandController;

    private Renderer[] renderers;
    private Collider[] colliders;
    private Collider triggerCollider;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = true;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (!CanInteract())
        {
            if (isCollecting)
            {
                CancelCollecting();
            }

            return;
        }

        if (Keyboard.current.eKey.isPressed)
        {
            StartCollecting();
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            CancelCollecting();
        }
    }

    private bool CanInteract()
    {
        if (lockedUntilPlayerExit) return false;
        if (!playerInRange) return false;
        if (playerInventory == null) return false;
        if (itemData == null) return false;

        if (isRespawning)
        {
            return CanReturnItem();
        }

        return true;
    }

    private void StartCollecting()
    {
        if (!CanInteract()) return;

        // Chỉ kiểm tra công cụ nếu không phải là hành động trả lại
        if (!CanReturnItem() && !CanCollectWithCurrentTool())
        {
            ResetCollecting();

            if (interactionUI != null)
            {
                interactionUI.SetProgress(0f);
                interactionUI.Show(missingToolMessage);
            }

            return;
        }

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
        if (!CanInteract())
        {
            ResetCollecting();
            return;
        }

        if (CanReturnItem())
        {
            ReturnItem();
            return;
        }

        if (!CanCollectWithCurrentTool())
        {
            ResetCollecting();

            if (interactionUI != null)
            {
                interactionUI.SetProgress(0f);
                interactionUI.Show(missingToolMessage);
            }

            return;
        }

        int pickupAmount = 1;

        bool success = playerInventory.AddItem(itemData, pickupAmount);

        ResetCollecting();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
        }

        if (!success)
        {
            Debug.Log("Inventory đầy, không thể lấy thêm: " + itemData.itemName);

            if (interactionUI != null)
            {
                interactionUI.Show("Inventory đầy");
            }

            return;
        }

        Debug.Log("Đã lấy: " + itemData.itemName);

        if (reportQuestProgress)
        {
            ReportQuestProgress(pickupAmount);
        }

        if (respawnAfterCollect)
        {
            respawnCoroutine = StartCoroutine(RespawnRoutine());
            return;
        }

        if (destroyAfterCollect)
        {
            if (interactionUI != null)
            {
                interactionUI.Hide();
            }

            Destroy(gameObject);
            return;
        }

        if (interactionUI != null)
        {
            interactionUI.Show(GetInteractionMessage());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        lockedUntilPlayerExit = false;

        ResetCollecting();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
            interactionUI.Hide();
        }

        SetObjectVisible(false);

        yield return new WaitForSeconds(respawnTime);

        SetObjectVisible(true);

        isRespawning = false;
        ResetCollecting();

        // Nếu player vẫn đang đứng trong vùng khi vật phẩm hồi lại,
        // không cho tương tác ngay. Phải bước ra rồi vào lại.
        if (requirePlayerExitAfterRespawn && playerInRange)
        {
            lockedUntilPlayerExit = true;

            if (interactionUI != null)
            {
                interactionUI.Hide();
                interactionUI.SetProgress(0f);
            }

            Debug.Log("Resource đã hồi, nhưng player đang đứng trong vùng. Cần rời vùng rồi vào lại.");
            yield break;
        }

        if (playerInRange && interactionUI != null && itemData != null)
        {
            interactionUI.Show(GetInteractionMessage());
            interactionUI.SetProgress(0f);
        }
    }

    private void SetObjectVisible(bool visible)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = visible;
            }
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;

            // Quan trọng:
            // Không tắt trigger chính, vì cần nó để biết player đã rời vùng chưa.
            if (colliders[i] == triggerCollider)
            {
                colliders[i].enabled = true;
                continue;
            }

            // Collider phụ, ví dụ collider thân cây, đá, model...
            // Có thể tắt khi resource biến mất.
            colliders[i].enabled = visible;
        }
    }

    private bool CanCollectWithCurrentTool()
    {
        if (!requireTool)
        {
            return true;
        }

        if (playerHandController == null)
        {
            return false;
        }

        return playerHandController.IsHoldingItem(requiredToolItemId);
    }

    private void ReportQuestProgress(int collectedAmount)
    {
        QuestManager questManager = FindFirstObjectByType<QuestManager>();

        if (questManager == null || itemData == null) return;

        switch (itemData.itemId)
        {
            case "water":
                questManager.AddProgress(QuestStepType.CollectWater, "water", collectedAmount);
                break;

            case "rice":
                questManager.AddProgress(QuestStepType.CollectRice, "rice", collectedAmount);
                break;

            case "iron_ore":
                questManager.AddProgress(QuestStepType.CollectIron, "iron_ore", collectedAmount);
                break;

            case "bamboo":
                questManager.AddProgress(QuestStepType.CollectBamboo, "bamboo", collectedAmount);
                break;

            case "chick":
                questManager.AddProgress(QuestStepType.CatchChicken, "chick", collectedAmount);
                break;
        }
    }

    private void CancelCollecting()
    {
        ResetCollecting();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);

            if (CanInteract())
            {
                interactionUI.Show(GetInteractionMessage());
            }
            else
            {
                interactionUI.Hide();
            }
        }
    }

    private void ResetCollecting()
    {
        isCollecting = false;
        collectTimer = 0f;
    }

    private bool CanReturnItem()
    {
        if (playerInventory == null || itemData == null) return false;

        // Chỉ cho phép lấy tối đa 1 axe hoặc 1 pickaxe
        if (itemData.itemId == "axe" && playerInventory.HasItem("axe", 1))
        {
            return true;
        }
        if (itemData.itemId == "pickaxe" && playerInventory.HasItem("pickaxe", 1))
        {
            return true;
        }

        return false;
    }

    private void ReturnItem()
    {
        if (playerInventory == null || itemData == null) return;

        bool success = playerInventory.RemoveItem(itemData.itemId, 1);

        ResetCollecting();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
        }

        if (!success)
        {
            Debug.LogWarning("Không thể trả lại vật phẩm: " + itemData.itemName);
            return;
        }

        Debug.Log("Đã trả lại vật phẩm: " + itemData.itemName);

        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }

        isRespawning = false;
        lockedUntilPlayerExit = false;

        SetObjectVisible(true);

        if (interactionUI != null)
        {
            interactionUI.Show(GetInteractionMessage());
        }
    }

    private string GetInteractionMessage()
    {
        if (itemData == null)
        {
            return "";
        }

        if (CanReturnItem())
        {
            return "Nhấn giữ E để trả lại " + itemData.itemName;
        }

        return "Nhấn giữ E để lấy " + itemData.itemName;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        BindPlayer(other);

        playerInRange = true;

        if (CanInteract() && interactionUI != null)
        {
            interactionUI.Show(GetInteractionMessage());
            interactionUI.SetProgress(0f);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Sửa lỗi player lần đầu vào vùng nhưng component chưa cập nhật.
        if (playerInventory == null || playerHandController == null)
        {
            BindPlayer(other);
        }

        playerInRange = true;

        if (CanInteract() && interactionUI != null && !isCollecting)
        {
            interactionUI.Show(GetInteractionMessage());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInventory = null;
        playerHandController = null;
        playerInRange = false;

        // Chỉ khi player rời vùng thì mới mở khóa tương tác sau respawn.
        lockedUntilPlayerExit = false;

        ResetCollecting();

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

        playerHandController = other.GetComponent<PlayerHandController>();

        if (playerHandController == null)
        {
            playerHandController = other.GetComponentInParent<PlayerHandController>();
        }

        if (playerHandController == null)
        {
            playerHandController = other.GetComponentInChildren<PlayerHandController>();
        }
    }
}