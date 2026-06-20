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
        if (playerInventory != null)
            return;

        BindInventory(FindLocalPlayerInventory());
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

    private PlayerInventory FindLocalPlayerInventory()
    {
        PlayerInventory[] inventories = FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None);
        NetworkManager networkManager = NetworkManager.Singleton;

        for (int i = 0; i < inventories.Length; i++)
        {
            if (inventories[i] == null) continue;

            if (networkManager != null && networkManager.IsListening)
            {
                NetworkObject networkObject = inventories[i].GetComponent<NetworkObject>();

                if (networkObject != null && networkObject.IsOwner)
                {
                    return inventories[i];
                }

                continue;
            }

            if (inventories[i].CompareTag("Player"))
            {
                return inventories[i];
            }
        }

        return null;
    }
}
