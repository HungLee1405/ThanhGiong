using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class ChickenController : MonoBehaviour
{
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
    private float catchTimer;
    public bool isCaught = false;
    public bool isDelivered = false;
    private Vector3 spawnPosition;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderInterval;
        spawnPosition = transform.position;

        questManager = FindFirstObjectByType<QuestManager>();

        FindLocalPlayer();
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
        HandleAI();
        HandleInteraction();
    }

    private void HandleAI()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            FindLocalPlayer();
        }

        if (player == null || agent == null || !agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < fleeDistance)
        {
            // Chạy trốn
            agent.speed = runSpeed;
            Vector3 fleeDirection = (transform.position - player.position).normalized;
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
        if (Keyboard.current == null || isCaught) return;

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

            if (playerHandController != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    InventoryItem item = playerInventory.GetItemAtSlot(i);
                    if (item != null && item.itemData == chickenItemData)
                    {
                        playerHandController.SelectSlot(i);
                        break;
                    }
                }
            }

            gameObject.SetActive(false);
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
        gameObject.SetActive(true);
    }

    private void BindPlayer(Collider other)
    {
        if (playerInventory == null)
        {
            playerInventory = other.GetComponent<PlayerInventory>();
            if (playerInventory == null) playerInventory = other.GetComponentInParent<PlayerInventory>();
            if (playerInventory == null) playerInventory = other.GetComponentInChildren<PlayerInventory>();
        }

        if (playerHandController == null)
        {
            playerHandController = other.GetComponent<PlayerHandController>();
            if (playerHandController == null) playerHandController = other.GetComponentInParent<PlayerHandController>();
            if (playerHandController == null) playerHandController = other.GetComponentInChildren<PlayerHandController>();
        }
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

    private bool IsLocalPlayer(Collider other)
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening) return true;

        PlayerMovement movement = other.GetComponentInParent<PlayerMovement>();
        return movement == null || !movement.IsSpawned || movement.IsOwner;
    }
}
