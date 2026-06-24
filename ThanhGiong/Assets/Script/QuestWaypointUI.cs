using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class QuestWaypointUI : MonoBehaviour
{
    [Header("3D Pointer Settings")]
    public Transform playerTransform;
    public Transform pointerPivot; 
    public float heightOffset = 0.2f; // Chỉnh âm nếu tâm nhân vật ở giữa người (ví dụ: -0.8)
    public float radiusOffset = 1.5f; // Khoảng cách mũi tên bay quanh người

    [Header("References")]
    private QuestManager questManager;
    private PlayerInventory playerInventory;
    
    private Transform currentTarget;

    private void Start()
    {
        questManager = FindFirstObjectByType<QuestManager>();

        // Khi online: luôn tìm lại local owner player, bỏ qua Inspector assign
        // (Inspector có thể trỏ vào offline player bị tắt khi online)
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsListening)
        {
            playerTransform = null; // Reset để FindLocalPlayerTransform() tìm lại
        }

        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            playerTransform = FindLocalPlayerTransform();
        }

        // Tìm inventory của local player
        if (playerTransform != null)
        {
            playerInventory = playerTransform.GetComponent<PlayerInventory>();
            if (playerInventory == null) playerInventory = playerTransform.GetComponentInChildren<PlayerInventory>();
        }

        // Tự động tạo mũi tên 3D dưới chân nếu chưa gán
        if (pointerPivot == null)
        {
            GameObject pivotObj = new GameObject("QuestPointerPivot");
            pointerPivot = pivotObj.transform;

            GameObject visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObj.name = "PointerVisual";
            visualObj.transform.SetParent(pointerPivot);
            
            // Làm cho cục Cube dài ra thành hình thanh chỉ đường
            visualObj.transform.localScale = new Vector3(0.3f, 0.05f, 0.8f);
            visualObj.transform.localPosition = Vector3.zero; // Nằm ngay vị trí pivot

            // Xóa collider để không cản trở di chuyển
            Destroy(visualObj.GetComponent<Collider>());
            
            // Đổi màu vàng
            Renderer r = visualObj.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Standard"));
                r.material.color = Color.yellow;
            }
        }
    }

    private void Update()
    {
        if (questManager == null) questManager = FindFirstObjectByType<QuestManager>();

        // Cập nhật lại playerTransform nếu chưa có HOẶC đang trỏ vào object không active
        // (trường hợp online: offline player bị tắt, cần tìm lại network player)
        if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
        {
            playerTransform = FindLocalPlayerTransform();

            if (playerTransform != null)
            {
                playerInventory = playerTransform.GetComponent<PlayerInventory>();
                if (playerInventory == null) playerInventory = playerTransform.GetComponentInChildren<PlayerInventory>();
            }
        }

        if (playerInventory == null && playerTransform != null)
        {
            playerInventory = playerTransform.GetComponent<PlayerInventory>();
            if (playerInventory == null) playerInventory = playerTransform.GetComponentInChildren<PlayerInventory>();
        }

        if (questManager == null || playerTransform == null || pointerPivot == null) return;

        UpdateTarget();

        if (currentTarget == null)
        {
            pointerPivot.gameObject.SetActive(false);
            return;
        }

        pointerPivot.gameObject.SetActive(true);
        UpdatePointerPositionAndRotation();
    }

    // Tìm transform của local player.
    // Offline: dùng FindGameObjectWithTag("Player") như cũ.
    // Online: duyệt tất cả PlayerMovement, lọc lấy IsOwner == true.
    private Transform FindLocalPlayerTransform()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
        {
            // Offline mode
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            return player != null ? player.transform : null;
        }

        // Online mode: tìm PlayerMovement là local owner
        PlayerMovement[] allMovements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (PlayerMovement movement in allMovements)
        {
            if (movement.IsSpawned && movement.IsOwner)
                return movement.transform;
        }

        // Fallback: chờ thêm (chưa spawn xong)
        return null;
    }

    private void UpdateTarget()
    {
        currentTarget = null;

        if (!questManager.HasActiveStep()) return;

        List<QuestStep> activeSteps = questManager.GetActiveSteps();
        
        foreach (var step in activeSteps)
        {
            if (step == null || step.IsCompleted()) continue;

            Transform target = TryGetTargetForStep(step);
            if (target != null)
            {
                currentTarget = target;
                break;
            }
        }
    }

    private Transform TryGetTargetForStep(QuestStep step)
    {
        if (step.stepType == QuestStepType.CatchChicken)
        {
            return null;
        }

        if (step.stepType == QuestStepType.TalkToNPC)
        {
            return FindNPCTransform(step.targetNPCId);
        }

        if (step.stepType == QuestStepType.CollectIron || step.stepType == QuestStepType.CollectBamboo)
        {
            if (step.currentAmount > 0)
            {
                return null;
            }

            if (playerInventory != null && playerInventory.HasItem(step.targetItemId, 1))
            {
                return FindVillageStorage();
            }

            return FindClosestResource(step.targetItemId);
        }

        if (step.stepType == QuestStepType.CollectWater || step.stepType == QuestStepType.CollectRice)
        {
            return FindClosestResource(step.targetItemId);
        }

        if (step.stepType == QuestStepType.CookRice)
        {
            CookingPot pot = FindFirstObjectByType<CookingPot>();
            if (pot != null) return pot.transform;
        }

        if (step.stepType == QuestStepType.FeedGiong)
        {
            return FindNPCTransform("giong_mother");
        }
        
        if (step.stepType == QuestStepType.ForgeWeapon)
        {
            return FindNPCTransform("blacksmith");
        }

        return null;
    }

    private Transform FindNPCTransform(string npcId)
    {
        NPCDialogue[] npcs = FindObjectsByType<NPCDialogue>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc.npcId == npcId)
            {
                return npc.transform;
            }
        }
        return null;
    }

    private Transform FindVillageStorage()
    {
        VillageStorage storage = FindFirstObjectByType<VillageStorage>();
        if (storage != null) return storage.transform;
        return null;
    }

    private Transform FindClosestResource(string targetItemId)
    {
        ResourcePickup[] pickups = FindObjectsByType<ResourcePickup>(FindObjectsSortMode.None);
        Transform closest = null;
        float minDistance = float.MaxValue;
        
        if (playerTransform == null) return null;
        Vector3 playerPos = playerTransform.position;

        foreach (var pickup in pickups)
        {
            if (pickup.itemData != null && pickup.itemData.itemId == targetItemId)
            {
                float dist = Vector3.Distance(playerPos, pickup.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = pickup.transform;
                }
            }
        }
        return closest;
    }

    private void UpdatePointerPositionAndRotation()
    {
        // Tính hướng từ người chơi đến mục tiêu
        Vector3 direction = currentTarget.position - playerTransform.position;
        direction.y = 0; // Giữ trên mặt phẳng ngang

        if (direction.sqrMagnitude > 0.01f)
        {
            direction.Normalize();

            // 1. Xoay mũi tên về phía mục tiêu
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            pointerPivot.rotation = Quaternion.Slerp(pointerPivot.rotation, targetRotation, Time.deltaTime * 10f);

            // 2. Di chuyển mũi tên ra xa người chơi một khoảng radiusOffset
            Vector3 targetPosition = playerTransform.position + direction * radiusOffset;
            targetPosition.y = playerTransform.position.y + heightOffset; 

            pointerPivot.position = targetPosition;
        }
    }
}
