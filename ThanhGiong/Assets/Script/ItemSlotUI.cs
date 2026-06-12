using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject root;
    public Image itemIcon;
    public TextMeshProUGUI amountText;
    public TextMeshProUGUI hotkeyText;

    public void SetSlot(InventoryItem inventoryItem, int slotNumber)
    {
        if (inventoryItem == null || inventoryItem.itemData == null || inventoryItem.amount <= 0)
        {
            ClearSlot(slotNumber);
            return;
        }

        if (root != null)
        {
            root.SetActive(true);
        }

        if (itemIcon != null)
        {
            itemIcon.gameObject.SetActive(true);
            itemIcon.sprite = inventoryItem.itemData.icon;
            itemIcon.preserveAspect = true;
        }

        if (amountText != null)
        {
            if (inventoryItem.itemData.stackable)
            {
                amountText.gameObject.SetActive(true);
                amountText.text = inventoryItem.amount.ToString();
            }
            else
            {
                amountText.gameObject.SetActive(false);
                amountText.text = "";
            }
        }

        if (hotkeyText != null)
        {
            hotkeyText.text = slotNumber.ToString();
        }
    }

    public void ClearSlot(int slotNumber)
    {
        if (root != null)
        {
            root.SetActive(true);
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.gameObject.SetActive(false);
        }

        if (amountText != null)
        {
            amountText.text = "";
            amountText.gameObject.SetActive(false);
        }

        if (hotkeyText != null)
        {
            hotkeyText.text = slotNumber.ToString();
        }
    }
}