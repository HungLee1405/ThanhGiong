using UnityEngine;
using Unity.Netcode;

public class PlayerInventoryUI : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory playerInventory;

    [Header("Slots")]
    public ItemSlotUI[] slots;

    private int highlightedSlotIndex = -1;

    private void Start()
    {
        // Khi online: luôn tìm lại local owner inventory, bỏ qua Inspector assign
        // (Inspector có thể trỏ vào offline player bị tắt khi online)
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && networkManager.IsListening)
        {
            playerInventory = null; // Reset để FindLocalPlayerInventory() tìm lại
        }

        PlayerInventory inventoryToBind = playerInventory;

        if (playerInventory == null)
        {
            inventoryToBind = FindLocalPlayerInventory();
        }

        playerInventory = null;
        BindInventory(inventoryToBind);

        UpdateUI();
        HighlightSlot(-1);
    }

    private void Update()
    {
        // Bind lại nếu chưa có HOẶC đang trỏ vào object không active (offline player bị tắt)
        if (playerInventory == null ||
            (playerInventory.gameObject != null && !playerInventory.gameObject.activeInHierarchy))
        {
            playerInventory = null; // force rebind
            BindInventory(FindLocalPlayerInventory());
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= UpdateUI;
        }
    }

    public void UpdateUI()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            InventoryItem item = null;

            if (playerInventory != null)
            {
                item = playerInventory.GetItemAtSlot(i);
            }

            slots[i].SetSlot(item, i + 1);
            slots[i].SetHighlight(i == highlightedSlotIndex);
        }
    }

    public void HighlightSlot(int slotIndex)
    {
        highlightedSlotIndex = slotIndex;

        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].SetHighlight(i == highlightedSlotIndex);
            }
        }
    }

    public void ClearHighlight()
    {
        HighlightSlot(-1);
    }

    private void BindInventory(PlayerInventory inventory)
    {
        if (inventory == null || playerInventory == inventory)
            return;

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged -= UpdateUI;
        }

        playerInventory = inventory;
        playerInventory.OnInventoryChanged += UpdateUI;
        UpdateUI();
    }

    public void RebindToLocalInventory()
    {
        BindInventory(FindLocalPlayerInventory());
        UpdateUI();
    }

    private PlayerInventory FindLocalPlayerInventory()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        // Online mode: t\u00ecm qua PlayerMovement l\u00e0 local owner
        if (networkManager != null && networkManager.IsListening)
        {
            PlayerMovement[] movements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (PlayerMovement movement in movements)
            {
                if (!movement.IsSpawned || !movement.IsOwner) continue;

                PlayerInventory inv = movement.GetComponent<PlayerInventory>();
                if (inv == null) inv = movement.GetComponentInChildren<PlayerInventory>();
                if (inv != null) return inv;
            }
            return null;
        }

        // Offline mode: t\u00ecm qua tag "Player"
        PlayerInventory[] inventories = FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None);
        foreach (PlayerInventory inventory in inventories)
        {
            if (inventory != null && inventory.gameObject.CompareTag("Player"))
                return inventory;
        }

        return null;
    }
}
