using UnityEngine;

public class VillageStorage : MonoBehaviour
{
    [Header("UI & Interaction")]
    public InteractionUI interactionUI;
    public string interactionMessage = "Nhấn giữ R/E để gửi vật phẩm vào kho";
    public float storeTime = 1f;

    [Header("Storage Data (Saved in Runtime)")]
    public int ironOreAmount = 0;
    public int bambooAmount = 0;
    public int waterAmount = 0;
    public int riceAmount = 0;

    public event System.Action OnStorageChanged;
    
    public int GetAmount(string itemId)
    {
        switch (itemId)
        {
            case "iron_ore": return ironOreAmount;
            case "bamboo": return bambooAmount;
            case "water": return waterAmount;
            case "rice": return riceAmount;
            default: return 0;
        }
    }

    private PlayerHandController playerHandController;
    private PlayerInventory playerInventory;
    private bool playerInRange = false;

    private bool isStoring = false;
    private float storeTimer = 0f;

    private void Update()
    {
        if (!playerInRange || playerHandController == null || UnityEngine.InputSystem.Keyboard.current == null) return;

        bool isPressingStore = UnityEngine.InputSystem.Keyboard.current.rKey.isPressed || UnityEngine.InputSystem.Keyboard.current.eKey.isPressed;
        bool releasedStore = UnityEngine.InputSystem.Keyboard.current.rKey.wasReleasedThisFrame || UnityEngine.InputSystem.Keyboard.current.eKey.wasReleasedThisFrame;

        if (isPressingStore)
        {
            StartStoring();
        }

        if (releasedStore)
        {
            CancelStoring();
        }
    }

    private void StartStoring()
    {
        ItemData heldData = playerHandController.GetHeldItemData();
        if (heldData == null)
        {
            if (interactionUI != null)
            {
                interactionUI.Show("Cần cầm vật phẩm trên tay để gửi vào kho");
            }
            CancelStoring();
            return;
        }

        // Kiểm tra vật phẩm hợp lệ trước
        switch (heldData.itemId)
        {
            case "iron_ore":
            case "bamboo":
            case "water":
            case "rice":
                break;
            default:
                if (interactionUI != null) interactionUI.Show("Kho làng không nhận " + heldData.itemName);
                CancelStoring();
                return;
        }

        if (!isStoring)
        {
            isStoring = true;
            storeTimer = 0f;
            if (interactionUI != null) interactionUI.Show("Đang gửi " + heldData.itemName + "...");
        }

        storeTimer += Time.deltaTime;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(storeTimer / storeTime);
        }

        if (storeTimer >= storeTime)
        {
            FinishStoring();
        }
    }

    private void FinishStoring()
    {
        ItemData heldData = playerHandController.GetHeldItemData();
        if (heldData == null)
        {
            CancelStoring();
            return;
        }

        int amountStored = 0;

        switch (heldData.itemId)
        {
            case "iron_ore":
                ironOreAmount++; amountStored = 1; break;
            case "bamboo":
                bambooAmount++; amountStored = 1; break;
            case "water":
                waterAmount++; amountStored = 1; break;
            case "rice":
                riceAmount++; amountStored = 1; break;
            default:
                CancelStoring();
                return;
        }

        if (playerHandController.TryConsumeHeldItem(1))
        {
            Debug.Log("Đã gửi " + heldData.itemName + " vào kho.");
            
            OnStorageChanged?.Invoke();
            ReportQuestProgress(heldData.itemId, amountStored);
        }

        // Reset để phải nhấn giữ lại cho món tiếp theo
        isStoring = false;
        storeTimer = 0f;
        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
            interactionUI.Show(interactionMessage);
        }
    }

    private void CancelStoring()
    {
        if (!isStoring) return;

        isStoring = false;
        storeTimer = 0f;

        if (interactionUI != null)
        {
            interactionUI.SetProgress(0f);
            if (playerInRange)
            {
                interactionUI.Show(interactionMessage);
            }
            else
            {
                interactionUI.Hide();
            }
        }
    }

    private void ReportQuestProgress(string itemId, int amount)
    {
        QuestManager qm = FindFirstObjectByType<QuestManager>();
        if (qm == null) return;

        if (itemId == "iron_ore") qm.AddProgress(QuestStepType.CollectIron, itemId, amount);
        else if (itemId == "bamboo") qm.AddProgress(QuestStepType.CollectBamboo, itemId, amount);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        BindPlayer(other);
        
        playerInRange = true;
        if (interactionUI != null) interactionUI.Show(interactionMessage);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Sửa lỗi player lần đầu vào vùng nhưng component chưa được nhận dạng
        if (playerHandController == null || playerInventory == null)
        {
            BindPlayer(other);
        }

        playerInRange = true;
        
        if (interactionUI != null && !isStoring)
        {
            interactionUI.Show(interactionMessage);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        playerHandController = null;
        playerInventory = null;
        
        CancelStoring();

        if (interactionUI != null) interactionUI.Hide();
    }

    private void BindPlayer(Collider other)
    {
        playerInventory = other.GetComponent<PlayerInventory>();
        if (playerInventory == null) playerInventory = other.GetComponentInParent<PlayerInventory>();
        if (playerInventory == null) playerInventory = other.GetComponentInChildren<PlayerInventory>();

        playerHandController = other.GetComponent<PlayerHandController>();
        if (playerHandController == null) playerHandController = other.GetComponentInParent<PlayerHandController>();
        if (playerHandController == null) playerHandController = other.GetComponentInChildren<PlayerHandController>();
    }
}
