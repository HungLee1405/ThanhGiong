using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public Sprite itemIcon;

    [Header("Stack")]
    public bool stackable = true;
    public int maxStack = 1;

    [Header("Visual")]
    public GameObject handPrefab;
}