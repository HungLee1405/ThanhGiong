using UnityEngine;

public class VillageStorage : MonoBehaviour
{
    [Header("UI & Interaction")]
    public InteractionUI interactionUI;
    public string interactionMessage = "Nhấn R/E để gửi vật phẩm vào kho";

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

    private void Update()
    {
        if (!playerInRange || playerHandController == null || UnityEngine.InputSystem.Keyboard.current == null) return;

        if (UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryStoreItem();
        }
    }

    private void TryStoreItem()
    {
        ItemData heldData = playerHandController.GetHeldItemData();
        if (heldData == null)
        {
            if (interactionUI != null)
            {
                interactionUI.Show("Cần cầm vật phẩm trên tay để gửi vào kho");
            }
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
                if (interactionUI != null) interactionUI.Show("Kho làng không nhận " + heldData.itemName);
                return;
        }

        if (playerHandController.TryConsumeHeldItem(1))
        {
            Debug.Log("Đã gửi " + heldData.itemName + " vào kho.");
            if (interactionUI != null) interactionUI.Show("Đã gửi " + heldData.itemName);
            
            OnStorageChanged?.Invoke();
            ReportQuestProgress(heldData.itemId, amountStored);
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
        
        playerHandController = other.GetComponentInChildren<PlayerHandController>();
        if (playerHandController == null) playerHandController = other.GetComponentInParent<PlayerHandController>();
        
        playerInRange = true;
        if (interactionUI != null) interactionUI.Show(interactionMessage);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        playerHandController = null;
        if (interactionUI != null) interactionUI.Hide();
    }
}
