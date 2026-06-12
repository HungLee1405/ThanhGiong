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

    [Header("Highlight")]
    public GameObject highlightObject;

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
            itemIcon.enabled = true;
            itemIcon.sprite = inventoryItem.itemData.itemIcon;
            itemIcon.preserveAspect = true;

            Color color = itemIcon.color;
            color.a = 1f;
            itemIcon.color = color;
        }

        if (amountText != null)
        {
            amountText.gameObject.SetActive(false);
            amountText.text = "";
        }

        if (hotkeyText != null)
        {
            hotkeyText.gameObject.SetActive(true);
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
            itemIcon.enabled = false;
            itemIcon.gameObject.SetActive(false);
        }

        if (amountText != null)
        {
            amountText.text = "";
            amountText.gameObject.SetActive(false);
        }

        if (hotkeyText != null)
        {
            hotkeyText.gameObject.SetActive(true);
            hotkeyText.text = slotNumber.ToString();
        }
    }

    public void SetHighlight(bool active)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(active);
        }
    }
}