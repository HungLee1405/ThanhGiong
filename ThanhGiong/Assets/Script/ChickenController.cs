using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections.Generic;

public class ChickenController : MonoBehaviour
{
    private static readonly Dictionary<string, ChickenController> SharedChickens = new Dictionary<string, ChickenController>();
    private const float RecaptureDelay = 0.75f;

    [Header("Settings")]
    public float wanderRadius = 5f;
    public float fleeDistance = 4f;
    public float wanderInterval = 3f;
    public float walkSpeed = 1f;
    public float runSpeed = 3f;

    [Header("Interaction")]
    public float catchTime = 2f;
    public InteractionUI interactionUI;
    public ItemData chickenItemData;

    private NavMeshAgent agent;
    private Transform player;
    private PlayerInventory playerInventory;
    private PlayerHandController playerHandController;
    private QuestManager questManager;

    private float timer;
    private bool playerInRange;
    private bool isCatching;
    private bool waitingForSharedCatch;
    private float catchTimer;
    public bool isCaught = false;
    public bool isDelivered = false;
    private Vector3 spawnPosition;
    private string networkChickenId;
    private Vector3 sharedTargetPosition;
    private Quaternion sharedTargetRotation;
    private float nextSharedTransformPublishTime;
    private float nextCatchAllowedTime;
    private bool hasSharedTransformState;

    private const float SharedTransformPublishInterval = 0.12f;
    private const float SharedTransformLerpSpeed = 12f;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderInterval;
        spawnPosition = transform.position;
        networkChickenId = BuildNetworkChickenId();
        RegisterSharedChicken();

        questManager = FindFirstObjectByType<QuestManager>();

        FindLocalPlayer();
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(networkChickenId))
        {
            networkChickenId = BuildNetworkChickenId();
        }

        RegisterSharedChicken();
    }

    private bool CanCatch()
    {
        if (questManager == null)
        {
            questManager = FindFirstObjectByType<QuestManager>();
        }
        return questManager != null && questManager.IsStepActive(QuestStepType.CatchChicken);
    }

    private void Update()
    {
        if (ShouldFollowSharedOnlineTransform())
        {
            HandleSharedTransformFollow();
        }
        else
        {
            HandleAI();
            PublishSharedTransformIfNeeded();
        }

        HandleInteraction();
    }

    private void HandleAI()
    {
        Transform threatPlayer = FindChickenThreatTarget();

        if (threatPlayer == null && (player == null || !player.gameObject.activeInHierarchy))
        {
            FindLocalPlayer();
        }

        if (threatPlayer == null)
        {
            threatPlayer = player;
        }

        if (threatPlayer == null || agent == null || !agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, threatPlayer.position);

        if (distanceToPlayer < fleeDistance)
        {
            // Chạy trốn
            agent.speed = runSpeed;
            Vector3 fleeDirection = (transform.position - threatPlayer.position).normalized;
            Vector3 fleeTarget = transform.position + fleeDirection * fleeDistance;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleeTarget, out hit, fleeDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
        else
        {
            // Đi dạo
            agent.speed = walkSpeed;
            timer += Time.deltaTime;

            if (timer >= wanderInterval)
            {
                // Đi dạo quanh vị trí spawn thay vì quanh vị trí hiện tại để không đi quá xa
                Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                randomDirection += spawnPosition;
                
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                
                timer = 0;
            }
        }
    }

    private void HandleInteraction()
    {
        if (PauseMenuManager.isPaused) return;
        if (NetworkLobbyCoordinator.IsOnlineLobbyActive) return;
        if (waitingForSharedCatch) return;
        if (Keyboard.current == null || isCaught) return;
        if (Time.time < nextCatchAllowedTime) return;

        if (!playerInRange)
        {
            if (isCatching) CancelCatching();
            return;
        }

        if (!CanCatch())
        {
            if (isCatching) CancelCatching();
            if (interactionUI != null) interactionUI.Hide();
            return;
        }

        if (!isCatching && interactionUI != null && interactionUI.root != null && !interactionUI.root.activeSelf)
        {
            interactionUI.Show("Nhấn giữ E để bắt gà");
            interactionUI.SetProgress(0f);
        }

        if (Keyboard.current.eKey.isPressed)
        {
            if (!isCatching)
            {
                isCatching = true;
                catchTimer = 0f;
            }

            catchTimer += Time.deltaTime;

            if (interactionUI != null)
            {
                interactionUI.SetProgress(catchTimer / catchTime);
            }

            if (catchTimer >= catchTime)
            {
                CatchSuccess();
            }
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame)
        {
            CancelCatching();
        }
    }

    private void CatchSuccess()
    {
        if (playerInventory == null || isCaught) return;

        if (ShouldUseSharedOnlineCatch())
        {
            TryCatchSharedChicken();
            return;
        }

        bool success = playerInventory.AddItem(chickenItemData, 1);
        if (success)
        {
            isCaught = true;
            if (playerHandController != null)
            {
                playerHandController.carriedChicken = this;
            }

            if (interactionUI != null)
            {
                interactionUI.SetProgress(0f);
                interactionUI.Hide();
            }

            SelectCaughtChickenSlot(chickenItemData.itemId);

            ResetAfterCatch();
        }
        else
        {
            if (interactionUI != null)
            {
                interactionUI.Show("Túi đồ đầy!");
                interactionUI.SetProgress(0f);
            }
            isCatching = false;
        }
    }

    private void TryCatchSharedChicken()
    {
        if (playerInventory == null || chickenItemData == null)
        {
            CancelCatching();
            return;
        }

        if (!playerInventory.CanAddItem(chickenItemData, 1))
        {
            if (interactionUI != null)
            {
                interactionUI.Show("Túi đồ đầy!");
                interactionUI.SetProgress(0f);
            }

            isCatching = false;
            catchTimer = 0f;
            return;
        }

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening)
            return;

        isCatching = false;
        catchTimer = 0f;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
        }

        if (manager.IsServer)
        {
            if (TryReserveSharedCatch(networkChickenId, out string itemId))
            {
                GrantSharedCatchReward(itemId);
            }
            else if (interactionUI != null)
            {
                interactionUI.Hide();
            }
            return;
        }

        waitingForSharedCatch = true;
        if (interactionUI != null)
        {
            interactionUI.Show("Đang bắt gà...");
        }

        SharedQuestNetwork.RequestChickenCatch(networkChickenId);
    }

    private void GrantSharedCatchReward(string itemId)
    {
        ItemData rewardItem = FindItemData(itemId);
        if (playerInventory == null || playerHandController == null)
        {
            FindLocalPlayer();
        }

        if (playerInventory == null || rewardItem == null)
            return;

        bool success = playerInventory.AddItem(rewardItem, 1);
        if (!success)
        {
            if (interactionUI != null)
            {
                interactionUI.Show("Túi đồ đầy!");
            }
            return;
        }

        isCaught = true;
        waitingForSharedCatch = false;

        if (playerHandController != null)
        {
            playerHandController.carriedChicken = this;
        }

        SelectCaughtChickenSlot(rewardItem.itemId);

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
            interactionUI.Hide();
        }

        ResetAfterCatch();
    }

    private void SelectCaughtChickenSlot(string itemId)
    {
        if (playerInventory == null || playerHandController == null)
        {
            FindLocalPlayer();
        }

        if (playerInventory == null || playerHandController == null)
            return;

        int slotIndex = playerInventory.FindItemSlot(itemId);
        if (slotIndex < 0)
            return;

        playerHandController.SelectSlot(slotIndex);
        playerHandController.RefreshHeldItem();

        PlayerInventoryUI inventoryUI = playerHandController.playerInventoryUI;
        if (inventoryUI == null)
        {
            inventoryUI = FindFirstObjectByType<PlayerInventoryUI>();
            playerHandController.playerInventoryUI = inventoryUI;
        }

        if (inventoryUI != null)
        {
            inventoryUI.RebindToLocalInventory();
            inventoryUI.UpdateUI();
            inventoryUI.HighlightSlot(slotIndex);
        }
    }

    private void CancelCatching()
    {
        isCatching = false;
        catchTimer = 0f;
        if (interactionUI != null && playerInRange && CanCatch())
        {
            interactionUI.SetProgress(0f);
            interactionUI.Show("Nhấn giữ E để bắt gà");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && IsLocalPlayer(other))
        {
            BindPlayer(other);
            playerInRange = true;
            if (CanCatch())
            {
                if (interactionUI != null)
                {
                    interactionUI.Show("Nhấn giữ E để bắt gà");
                    interactionUI.SetProgress(0f);
                }
            }
            else
            {
                if (interactionUI != null)
                {
                    interactionUI.Hide();
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && IsLocalPlayer(other))
        {
            if (playerInventory == null || playerHandController == null)
            {
                BindPlayer(other);
            }

            playerInRange = true;

            if (CanCatch() && interactionUI != null && !isCatching)
            {
                interactionUI.Show("Nhấn giữ E để bắt gà");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && IsLocalPlayer(other))
        {
            playerInRange = false;
            CancelCatching();
            if (interactionUI != null)
            {
                interactionUI.Hide();
            }
        }
    }

    public void ResetToSpawn()
    {
        if (isDelivered) return;

        if (agent != null)
        {
            agent.Warp(spawnPosition);
            agent.SetDestination(spawnPosition);
        }
        else
        {
            transform.position = spawnPosition;
        }
        isCaught = false;
        waitingForSharedCatch = false;
        gameObject.SetActive(true);
    }

    private void ResetAfterCatch()
    {
        isCaught = false;
        waitingForSharedCatch = false;
        isCatching = false;
        catchTimer = 0f;
        nextCatchAllowedTime = Time.time + RecaptureDelay;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(spawnPosition);
            agent.SetDestination(spawnPosition);
        }
        else
        {
            transform.position = spawnPosition;
        }
    }

    private void ApplySharedCaught(bool caught)
    {
        waitingForSharedCatch = false;
        isCatching = false;
        catchTimer = 0f;
        isCaught = caught;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
            interactionUI.Hide();
        }

        if (caught)
        {
            ResetAfterCatch();
        }
        else if (!isDelivered)
        {
            gameObject.SetActive(true);
        }
    }

    private bool ShouldUseSharedOnlineCatch()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager != null && manager.IsListening;
    }

    private bool ShouldFollowSharedOnlineTransform()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager != null && manager.IsListening && !manager.IsServer;
    }

    private bool ShouldPublishSharedOnlineTransform()
    {
        NetworkManager manager = NetworkManager.Singleton;
        return manager != null && manager.IsListening && manager.IsServer;
    }

    private void PublishSharedTransformIfNeeded()
    {
        if (!ShouldPublishSharedOnlineTransform())
            return;

        if (Time.unscaledTime < nextSharedTransformPublishTime)
            return;

        nextSharedTransformPublishTime = Time.unscaledTime + SharedTransformPublishInterval;
        SharedQuestNetwork.PublishChickenTransformState(
            networkChickenId,
            transform.position,
            transform.eulerAngles.y,
            isCaught || !gameObject.activeInHierarchy);
    }

    private void HandleSharedTransformFollow()
    {
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
        }

        if (!hasSharedTransformState)
            return;

        transform.position = Vector3.Lerp(
            transform.position,
            sharedTargetPosition,
            Time.deltaTime * SharedTransformLerpSpeed);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            sharedTargetRotation,
            Time.deltaTime * SharedTransformLerpSpeed);
    }

    private string BuildNetworkChickenId()
    {
        Vector3 position = transform.position;
        return string.Format(
            "{0}:chicken:{1}:{2}:{3}:{4}",
            gameObject.scene.name,
            gameObject.name,
            Mathf.RoundToInt(position.x * 100f),
            Mathf.RoundToInt(position.y * 100f),
            Mathf.RoundToInt(position.z * 100f));
    }

    private void RegisterSharedChicken()
    {
        if (!string.IsNullOrEmpty(networkChickenId))
        {
            SharedChickens[networkChickenId] = this;
        }
    }

    public static bool TryReserveSharedCatch(string chickenId, out string itemId)
    {
        itemId = "";

        if (string.IsNullOrEmpty(chickenId) ||
            !SharedChickens.TryGetValue(chickenId, out ChickenController chicken) ||
            chicken == null ||
            chicken.chickenItemData == null ||
            chicken.isDelivered ||
            Time.time < chicken.nextCatchAllowedTime)
        {
            return false;
        }

        itemId = chicken.chickenItemData.itemId;
        chicken.ResetAfterCatch();
        SharedQuestNetwork.PublishChickenState(chickenId, false);
        return true;
    }

    public static void ApplySharedCatchResult(string chickenId, string itemId, bool success)
    {
        if (string.IsNullOrEmpty(chickenId) ||
            !SharedChickens.TryGetValue(chickenId, out ChickenController chicken) ||
            chicken == null)
        {
            return;
        }

        chicken.waitingForSharedCatch = false;

        if (!success)
        {
            if (chicken.interactionUI != null)
            {
                chicken.interactionUI.Hide();
            }
            return;
        }

        chicken.GrantSharedCatchReward(itemId);
    }

    public static void ApplySharedState(string chickenId, bool caught)
    {
        if (string.IsNullOrEmpty(chickenId) ||
            !SharedChickens.TryGetValue(chickenId, out ChickenController chicken) ||
            chicken == null)
        {
            return;
        }

        chicken.ApplySharedCaught(caught);
    }

    public static void ApplySharedTransformState(string chickenId, Vector3 position, float rotationY, bool hidden)
    {
        if (string.IsNullOrEmpty(chickenId) ||
            !SharedChickens.TryGetValue(chickenId, out ChickenController chicken) ||
            chicken == null)
        {
            return;
        }

        bool shouldSnap = !chicken.hasSharedTransformState ||
            (chicken.transform.position - position).sqrMagnitude > 9f;

        chicken.waitingForSharedCatch = false;
        chicken.isCaught = false;
        chicken.hasSharedTransformState = true;
        chicken.sharedTargetPosition = position;
        chicken.sharedTargetRotation = Quaternion.Euler(0f, rotationY, 0f);

        if (hidden)
        {
            if (chicken.interactionUI != null)
            {
                chicken.interactionUI.Hide();
            }

            chicken.ResetAfterCatch();
            return;
        }

        if (!chicken.gameObject.activeSelf && !chicken.isDelivered)
        {
            chicken.gameObject.SetActive(true);
        }

        if (chicken.agent != null && chicken.agent.enabled)
        {
            chicken.agent.enabled = false;
        }

        if (shouldSnap)
        {
            chicken.transform.position = position;
            chicken.transform.rotation = chicken.sharedTargetRotation;
        }
    }

    public static void PublishKnownSharedStatesToClient(ulong clientId)
    {
        foreach (ChickenController chicken in SharedChickens.Values)
        {
            if (chicken == null)
                continue;

            SharedQuestNetwork.SendChickenTransformState(
                clientId,
                chicken.networkChickenId,
                chicken.transform.position,
                chicken.transform.eulerAngles.y,
                false);

            SharedQuestNetwork.SendChickenState(clientId, chicken.networkChickenId, false);
        }
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

    private void BindPlayer(Collider other)
    {
        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
        if (movement != null)
        {
            player = movement.transform;
        }

        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null) inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null) inventory = other.GetComponentInChildren<PlayerInventory>();

        PlayerHandController handController = other.GetComponent<PlayerHandController>();
        if (handController == null) handController = other.GetComponentInParent<PlayerHandController>();
        if (handController == null) handController = other.GetComponentInChildren<PlayerHandController>();

        if (inventory != null) playerInventory = inventory;
        if (handController != null) playerHandController = handController;
    }

    private void FindLocalPlayer()
    {
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (PlayerMovement movement in players)
        {
            if (movement == null || !movement.gameObject.activeInHierarchy)
                continue;

            NetworkManager manager = NetworkManager.Singleton;
            if (manager != null && manager.IsListening && movement.IsSpawned && !movement.IsOwner)
                continue;

            player = movement.transform;
            playerInventory = movement.GetComponent<PlayerInventory>();
            playerHandController = movement.GetComponent<PlayerHandController>();
            return;
        }
    }

    private Transform FindChickenThreatTarget()
    {
        NetworkManager manager = NetworkManager.Singleton;
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        Transform nearestPlayer = null;
        float nearestDistance = float.MaxValue;

        foreach (PlayerMovement movement in players)
        {
            if (movement == null || !movement.gameObject.activeInHierarchy)
                continue;

            if (manager != null && manager.IsListening && !manager.IsServer &&
                movement.IsSpawned && !movement.IsOwner)
            {
                continue;
            }

            float distance = (movement.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlayer = movement.transform;
            }
        }

        return nearestPlayer;
    }

    private bool IsLocalPlayer(Collider other)
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening) return true;

        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
        return movement == null || !movement.IsSpawned || movement.IsOwner;
    }
}
