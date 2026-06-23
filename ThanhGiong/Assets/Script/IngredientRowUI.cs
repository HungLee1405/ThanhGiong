using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IngredientRowUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI amountText;
    public Color enoughColor = new Color(0.96f, 0.87f, 0.7f); // Wheat/Cream
    public Color missingColor = new Color(0.8f, 0.3f, 0.3f); // Reddish

    public void Setup(ItemData item, int amountNeeded, int amountHas)
    {
        if (iconImage != null && item.itemIcon != null)
        {
            iconImage.sprite = item.itemIcon;
            iconImage.gameObject.SetActive(true);
        }
        else if (iconImage != null)
        {
            iconImage.gameObject.SetActive(false);
        }

        if (nameText != null) nameText.text = item.itemName;
        if (amountText != null)
        {
            amountText.text = $"{amountHas} / {amountNeeded}";
            amountText.color = amountHas >= amountNeeded ? enoughColor : missingColor;
        }
    }
}
