using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Resources")]
    public int rice = 0;
    public int water = 0;
    public int cookedRice = 0;

    [Header("Limits")]
    public int maxRice = 5;
    public int maxWater = 5;
    public int maxCookedRice = 5;

    public bool AddRice(int amount)
    {
        if (rice >= maxRice) return false;

        rice = Mathf.Min(rice + amount, maxRice);
        Debug.Log("Gạo: " + rice);
        return true;
    }

    public bool AddWater(int amount)
    {
        if (water >= maxWater) return false;

        water = Mathf.Min(water + amount, maxWater);
        Debug.Log("Nước: " + water);
        return true;
    }

    public bool CanCookRice()
    {
        return rice >= 1 && water >= 1 && cookedRice < maxCookedRice;
    }

    public bool CookRice()
    {
        if (!CanCookRice()) return false;

        rice -= 1;
        water -= 1;
        cookedRice += 1;

        Debug.Log("Đã nấu xong cơm. Cơm hiện có: " + cookedRice);
        return true;
    }

    public bool UseCookedRice()
    {
        if (cookedRice <= 0) return false;

        cookedRice -= 1;
        Debug.Log("Đã dùng 1 cơm. Còn: " + cookedRice);
        return true;
    }
}