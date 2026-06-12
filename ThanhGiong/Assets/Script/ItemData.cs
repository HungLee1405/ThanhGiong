using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Thanh Giong/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemId;
    public string itemName;
    public Sprite icon;

    [Header("Settings")]
    public bool stackable = true;
    public int maxStack = 99;
}