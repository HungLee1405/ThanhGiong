using UnityEngine;

public class PlayerInventoryUI : MonoBehaviour
{
    [Header("References")]
    public PlayerInventory playerInventory;

    [Header("Slots")]
    public ItemSlotUI[] slots;

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

            if (playerInventory != null && i < playerInventory.items.Count)
            {
                slots[i].SetSlot(playerInventory.items[i], i + 1);
            }
            else
            {
                slots[i].ClearSlot(i + 1);
            }
        }
    }
}