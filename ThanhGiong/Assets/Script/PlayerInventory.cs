using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Inventory")]
    public List<InventoryItem> items = new List<InventoryItem>();

    public System.Action OnInventoryChanged;

    public bool AddItem(ItemData itemData, int amount)
    {
        if (itemData == null || amount <= 0) return false;

        InventoryItem existingItem = FindItem(itemData.itemId);

        if (existingItem != null && itemData.stackable)
        {
            int newAmount = existingItem.amount + amount;

            if (newAmount > itemData.maxStack)
            {
                existingItem.amount = itemData.maxStack;
            }
            else
            {
                existingItem.amount = newAmount;
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        if (existingItem != null && !itemData.stackable)
        {
            return false;
        }

        items.Add(new InventoryItem(itemData, amount));
        OnInventoryChanged?.Invoke();

        return true;
    }

    public bool RemoveItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;

        InventoryItem existingItem = FindItem(itemId);

        if (existingItem == null) return false;
        if (existingItem.amount < amount) return false;

        existingItem.amount -= amount;

        if (existingItem.amount <= 0)
        {
            items.Remove(existingItem);
        }

        OnInventoryChanged?.Invoke();

        return true;
    }

    public bool HasItem(string itemId, int amount)
    {
        InventoryItem existingItem = FindItem(itemId);

        if (existingItem == null) return false;

        return existingItem.amount >= amount;
    }

    public int GetItemAmount(string itemId)
    {
        InventoryItem existingItem = FindItem(itemId);

        if (existingItem == null) return 0;

        return existingItem.amount;
    }

    public InventoryItem FindItem(string itemId)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].itemData != null && items[i].itemData.itemId == itemId)
            {
                return items[i];
            }
        }

        return null;
    }
}