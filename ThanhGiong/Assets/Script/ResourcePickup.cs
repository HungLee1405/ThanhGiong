using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class ResourcePickup : MonoBehaviour
{
    private static readonly Dictionary<string, ResourcePickup> SharedPickups = new Dictionary<string, ResourcePickup>();

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
    private bool waitingForSharedCollect;
    private float sharedRespawnEndTime;
    private string networkResourceId;

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
        networkResourceId = BuildNetworkResourceId();
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = true;
        }
    }

    private void OnEnable()
    {
        RegisterSharedPickup();
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(networkResourceId) &&
            SharedPickups.TryGetValue(networkResourceId, out ResourcePickup pickup) &&
            pickup == this)
        {
            SharedPickups.Remove(networkResourceId);
        }
    }

    private void Update()
    {
        if (PauseMenuManager.isPaused) return;
        if (NetworkLobbyCoordinator.IsOnlineLobbyActive) return;
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
        if (waitingForSharedCollect) return false;
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

        if (ShouldUseSharedOnlineRespawn())
        {
            TryCollectSharedResource(pickupAmount);
            return;
        }

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

    private void TryCollectSharedResource(int pickupAmount)
    {
        if (!playerInventory.CanAddItem(itemData, pickupAmount))
        {
            ResetCollecting();
            if (interactionUI != null)
            {
                interactionUI.SetProgress(0f);
                interactionUI.Show("Inventory day");
            }
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsListening)
            return;

        ResetCollecting();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
        }

        if (networkManager.IsServer)
        {
            if (TryReserveSharedCollect(networkResourceId, out string itemId, out int amountToGrant, out _))
            {
                GrantSharedCollectReward(itemId, amountToGrant);
            }
            else if (interactionUI != null)
            {
                interactionUI.Hide();
            }
            return;
        }

        waitingForSharedCollect = true;
        if (interactionUI != null)
        {
            interactionUI.Show("Dang lay...");
        }

        SharedQuestNetwork.RequestResourceCollect(networkResourceId);
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

    private IEnumerator SharedRespawnRoutine(float seconds)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, seconds));

        ApplySharedVisibility(true, false);

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsServer)
        {
            SharedQuestNetwork.PublishResourceState(networkResourceId, false, 0f);
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

    private void ApplySharedVisibility(bool visible, bool lockIfPlayerInside)
    {
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }

        SetObjectVisible(visible);
        isRespawning = !visible;
        sharedRespawnEndTime = visible ? 0f : Time.time + respawnTime;
        waitingForSharedCollect = false;
        ResetCollecting();

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
            if (!visible || !playerInRange)
            {
                interactionUI.Hide();
            }
            else if (CanInteract())
            {
                interactionUI.Show(GetInteractionMessage());
            }
        }

        if (visible && requirePlayerExitAfterRespawn && lockIfPlayerInside && playerInRange)
        {
            lockedUntilPlayerExit = true;
            if (interactionUI != null)
            {
                interactionUI.Hide();
            }
        }
        else if (!visible)
        {
            lockedUntilPlayerExit = false;
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

            case "chick":
                questManager.AddProgress(QuestStepType.CatchChicken, "chick", collectedAmount);
                break;
        }
    }

    private bool ShouldUseSharedOnlineRespawn()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        return networkManager != null &&
            networkManager.IsListening &&
            respawnAfterCollect &&
            !CanReturnItem();
    }

    private string BuildNetworkResourceId()
    {
        string itemId = itemData != null ? itemData.itemId : "missing";
        Vector3 position = transform.position;
        return string.Format(
            "{0}:{1}:{2}:{3}:{4}",
            gameObject.scene.name,
            itemId,
            Mathf.RoundToInt(position.x * 100f),
            Mathf.RoundToInt(position.y * 100f),
            Mathf.RoundToInt(position.z * 100f));
    }

    private void RegisterSharedPickup()
    {
        if (string.IsNullOrEmpty(networkResourceId))
        {
            networkResourceId = BuildNetworkResourceId();
        }

        if (respawnAfterCollect)
        {
            SharedPickups[networkResourceId] = this;
        }
    }

    public static bool TryReserveSharedCollect(
        string resourceId,
        out string itemId,
        out int amount,
        out float respawnSeconds)
    {
        itemId = "";
        amount = 0;
        respawnSeconds = 0f;

        if (string.IsNullOrEmpty(resourceId) ||
            !SharedPickups.TryGetValue(resourceId, out ResourcePickup pickup) ||
            pickup == null ||
            pickup.itemData == null ||
            pickup.isRespawning)
        {
            return false;
        }

        itemId = pickup.itemData.itemId;
        amount = Mathf.Max(1, pickup.amount);
        respawnSeconds = Mathf.Max(0.05f, pickup.respawnTime);
        pickup.ApplySharedVisibility(false, false);
        pickup.sharedRespawnEndTime = Time.time + respawnSeconds;
        pickup.respawnCoroutine = pickup.StartCoroutine(pickup.SharedRespawnRoutine(respawnSeconds));
        SharedQuestNetwork.PublishResourceState(resourceId, true, respawnSeconds);
        return true;
    }

    public static void ApplySharedResourceState(string resourceId, bool hidden, float remainingSeconds)
    {
        if (string.IsNullOrEmpty(resourceId) ||
            !SharedPickups.TryGetValue(resourceId, out ResourcePickup pickup) ||
            pickup == null)
        {
            return;
        }

        if (hidden)
        {
            pickup.ApplySharedVisibility(false, false);
            pickup.respawnCoroutine = pickup.StartCoroutine(pickup.SharedRespawnRoutine(remainingSeconds));
        }
        else
        {
            pickup.ApplySharedVisibility(true, true);
        }
    }

    public static void ApplySharedCollectResult(string resourceId, string itemId, int amount, bool success)
    {
        if (string.IsNullOrEmpty(resourceId) ||
            !SharedPickups.TryGetValue(resourceId, out ResourcePickup pickup) ||
            pickup == null)
        {
            return;
        }

        pickup.waitingForSharedCollect = false;

        if (!success)
        {
            if (pickup.interactionUI != null)
            {
                pickup.interactionUI.Hide();
            }
            return;
        }

        pickup.GrantSharedCollectReward(itemId, amount);
    }

    public static void PublishKnownSharedStatesToClient(ulong clientId)
    {
        foreach (ResourcePickup pickup in SharedPickups.Values)
        {
            if (pickup == null || !pickup.isRespawning)
                continue;

            float remainingSeconds = Mathf.Max(0.05f, pickup.sharedRespawnEndTime - Time.time);
            SharedQuestNetwork.SendResourceState(clientId, pickup.networkResourceId, true, remainingSeconds);
        }
    }

    private void GrantSharedCollectReward(string itemId, int amountToGrant)
    {
        PlayerInventory inventory = playerInventory != null ? playerInventory : FindLocalInventory();
        ItemData rewardItem = FindItemData(itemId);

        if (inventory == null || rewardItem == null)
            return;

        bool success = inventory.AddItem(rewardItem, amountToGrant);
        if (!success)
        {
            if (interactionUI != null)
            {
                interactionUI.Show("Inventory day");
            }
            return;
        }

        Debug.Log("Da lay: " + rewardItem.itemName);

        if (reportQuestProgress)
        {
            ReportQuestProgress(amountToGrant);
        }
    }

    private static PlayerInventory FindLocalInventory()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (PlayerMovement movement in players)
        {
            if (movement == null || !movement.gameObject.activeInHierarchy)
                continue;

            if (networkManager != null && networkManager.IsListening && movement.IsSpawned && !movement.IsOwner)
                continue;

            PlayerInventory inventory = movement.GetComponent<PlayerInventory>();
            if (inventory != null)
                return inventory;
        }

        return null;
    }

    private static ItemData FindItemData(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return null;

        ItemData[] items = Resources.FindObjectsOfTypeAll<ItemData>();
        foreach (ItemData item in items)
        {
            if (item != null && item.itemId == itemId)
                return item;
        }

        return null;
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

    // Kiểm tra xem collider có phải là local player không.
    // Trong offline: chấp nhận mọi Player. Trong online: chỉ chấp nhận local owner.
    private bool IsLocalPlayer(Collider other)
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
            return true;

        // Kiểm tra qua PlayerMovement (NetworkBehaviour)
        PlayerMovement movement = other.GetComponent<PlayerMovement>();
        if (movement == null) movement = other.GetComponentInParent<PlayerMovement>();
        if (movement == null) movement = other.GetComponentInChildren<PlayerMovement>();

        if (movement != null && movement.IsSpawned)
            return movement.IsOwner;

        // Fallback: kiểm tra qua PlayerHandController (NetworkBehaviour)
        PlayerHandController hand = other.GetComponent<PlayerHandController>();
        if (hand == null) hand = other.GetComponentInParent<PlayerHandController>();
        if (hand == null) hand = other.GetComponentInChildren<PlayerHandController>();

        if (hand != null && hand.IsSpawned)
            return hand.IsOwner;

        // Nếu không phải network object thì chấp nhận (offline scene)
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!IsLocalPlayer(other)) return;

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
        if (!IsLocalPlayer(other)) return;

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
        if (!IsLocalPlayer(other)) return;

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
