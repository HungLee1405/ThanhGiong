using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

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

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerInventory = playerObj.GetComponent<PlayerInventory>();
            playerHandController = playerObj.GetComponent<PlayerHandController>();
        }
    }

    private void Update()
    {
        HandleAI();
        HandleInteraction();
    }

    private void HandleAI()
    {
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
        if (Keyboard.current == null || isCaught) return;

        if (!playerInRange)
        {
            if (isCatching) CancelCatching();
            return;
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
        if (interactionUI != null && playerInRange)
        {
            interactionUI.SetProgress(0f);
            interactionUI.Show("Nhấn giữ E để bắt gà");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (interactionUI != null)
            {
                interactionUI.Show("Nhấn giữ E để bắt gà");
                interactionUI.SetProgress(0f);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
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
}
