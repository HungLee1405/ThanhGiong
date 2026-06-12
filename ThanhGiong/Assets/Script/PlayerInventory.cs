using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxSlots = 8;

    [Header("Inventory")]
    public List<InventoryItem> items = new List<InventoryItem>();

    public System.Action OnInventoryChanged;

    private void Awake()
    {
        EnsureSlotCount();
    }

    private void EnsureSlotCount()
    {
        while (items.Count < maxSlots)
        {
            items.Add(null);
        }

        while (items.Count > maxSlots)
        {
            items.RemoveAt(items.Count - 1);
        }
    }

    public bool AddItem(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0) return false;

        EnsureSlotCount();

        // Mỗi slot chỉ chứa 1 món.
        // Vì vậy cần kiểm tra còn đủ slot trống không.
        int emptySlotCount = CountEmptySlots();

        if (emptySlotCount < amount)
        {
            OnInventoryChanged?.Invoke();
            return false;
        }

        int remainingAmount = amount;

        for (int i = 0; i < items.Count; i++)
        {
            if (IsSlotEmpty(i))
            {
                items[i] = new InventoryItem(itemData, 1);
                remainingAmount--;

                if (remainingAmount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItemAtSlot(int slotIndex, int amount)
    {
        EnsureSlotCount();

        if (slotIndex < 0 || slotIndex >= items.Count) return false;
        if (amount <= 0) return false;

        InventoryItem item = items[slotIndex];

        if (item == null) return false;
        if (item.itemData == null) return false;
        if (item.amount < amount) return false;

        item.amount -= amount;

        if (item.amount <= 0)
        {
            // Quan trọng:
            // Không dùng RemoveAt(slotIndex), vì RemoveAt sẽ làm item phía sau bị dồn lên.
            items[slotIndex] = null;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(string itemId, int amount)
    {
        EnsureSlotCount();

        if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;
        if (!HasItem(itemId, amount)) return false;

        int remainingAmount = amount;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];

            if (item != null &&
                item.itemData != null &&
                item.itemData.itemId == itemId)
            {
                int removeAmount = Mathf.Min(item.amount, remainingAmount);

                item.amount -= removeAmount;
                remainingAmount -= removeAmount;

                if (item.amount <= 0)
                {
                    items[i] = null;
                }

                if (remainingAmount <= 0)
                {
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(string itemId, int amount)
    {
        return GetItemAmount(itemId) >= amount;
    }

    public int GetItemAmount(string itemId)
    {
        EnsureSlotCount();

        int total = 0;

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];

            if (item != null &&
                item.itemData != null &&
                item.itemData.itemId == itemId)
            {
                total += item.amount;
            }
        }

        return total;
    }

    public InventoryItem GetItemAtSlot(int slotIndex)
    {
        EnsureSlotCount();

        if (slotIndex < 0 || slotIndex >= items.Count)
        {
            return null;
        }

        return items[slotIndex];
    }

    public InventoryItem FindItem(string itemId)
    {
        EnsureSlotCount();

        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];

            if (item != null &&
                item.itemData != null &&
                item.itemData.itemId == itemId)
            {
                return item;
            }
        }

        return null;
    }

    public bool IsInventoryFull()
    {
        EnsureSlotCount();

        return CountEmptySlots() <= 0;
    }

    private int CountEmptySlots()
    {
        int count = 0;

        for (int i = 0; i < items.Count; i++)
        {
            if (IsSlotEmpty(i))
            {
                count++;
            }
        }

        return count;
    }

    private bool IsSlotEmpty(int index)
    {
        if (index < 0 || index >= items.Count) return false;

        InventoryItem item = items[index];

        return item == null || item.itemData == null || item.amount <= 0;
    }
}