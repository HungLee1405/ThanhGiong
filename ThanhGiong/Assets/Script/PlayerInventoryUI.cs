using UnityEngine;

public class PlayerInventoryUI : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory playerInventory;

    [Header("Slots")]
    public ItemSlotUI[] slots;

    private int highlightedSlotIndex = -1;

    private void Start()
    {
        if (playerInventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerInventory = player.GetComponent<PlayerInventory>();
            }
        }

        if (playerInventory != null)
        {
            playerInventory.OnInventoryChanged += UpdateUI;
        }

        UpdateUI();
        HighlightSlot(-1);
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
}