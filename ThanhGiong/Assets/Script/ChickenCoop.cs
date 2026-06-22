using UnityEngine;

public class ChickenCoop : MonoBehaviour, IItemReceiver
{
    [Tooltip("ID của vật phẩm được phép bỏ vào (vd: chicken)")]
    public string acceptedItemId = "chicken";

    [Tooltip("Các object model gà sẽ được bật lên khi bỏ gà vào chuồng")]
    public GameObject[] chickenVisuals;
    
    private int chickenCount = 0;

    private void Start()
    {
        foreach (var visual in chickenVisuals)
        {
            if (visual != null) visual.SetActive(false);
        }
    }

    public bool ReceiveItem(ItemData itemData, int amount)
    {
        if (!CanReceiveItem(itemData, amount)) return false;

        chickenCount++;

        if (chickenCount <= chickenVisuals.Length)
        {
            if (chickenVisuals[chickenCount - 1] != null)
            {
                chickenVisuals[chickenCount - 1].SetActive(true);
            }
        }

        QuestManager questManager = FindFirstObjectByType<QuestManager>();
        if (questManager != null)
        {
            questManager.AddProgress(QuestStepType.CatchChicken, "chicken", 1);
        }

        return true;
    }

    public bool CanReceiveItem(ItemData itemData, int amount)
    {
        if (itemData == null || itemData.itemId != acceptedItemId) return false;
        return true;
    }

    public bool TryReceiveChicken(PlayerHandController hand)
    {
        if (hand == null || hand.carriedChicken == null) return false;
        ItemData heldItem = hand.GetHeldItemData();
        if (heldItem == null || !CanReceiveItem(heldItem, 1)) return false;

        hand.carriedChicken.isDelivered = true;
        if (hand.TryConsumeHeldItem(1))
        {
            hand.carriedChicken = null;
            ReceiveItem(heldItem, 1);
            return true;
        }
        else
        {
            hand.carriedChicken.isDelivered = false;
            return false;
        }
    }
}
