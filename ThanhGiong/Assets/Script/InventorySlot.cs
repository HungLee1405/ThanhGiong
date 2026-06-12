[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int amount;

    public bool IsEmpty()
    {
        return itemData == null || amount <= 0;
    }

    public void Clear()
    {
        itemData = null;
        amount = 0;
    }
}