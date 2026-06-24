using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecipeRowUI : MonoBehaviour
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;
    public TextMeshProUGUI ingredientsText;
    public Button cookButton;
    public GameObject lockOverlay;
    public TextMeshProUGUI statusText;
    
    private CookingRecipe recipe;
    private CookingMenuUI menuUI;

    [Header("Colors")]
    public Color missingItemColor = new Color(0.8f, 0.4f, 0.4f);
    public Color readyColor = new Color(0.9f, 0.85f, 0.7f);
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f);

    public void Setup(CookingRecipe recipe, CookingMenuUI menuUI, PlayerInventory inventory)
    {
        this.recipe = recipe;
        this.menuUI = menuUI;
        
        if (iconImage != null) iconImage.sprite = recipe.icon;
        if (nameText != null) nameText.text = recipe.displayName;
        if (descText != null) descText.text = recipe.description;

        bool unlocked = menuUI.IsRecipeUnlocked(recipe);
        if (lockOverlay != null) lockOverlay.SetActive(!unlocked);

        bool canCook = unlocked;
        string reqText = "";
        foreach (var ing in recipe.ingredients)
        {
            int has = inventory.GetItemAmount(ing.item.itemId);
            reqText += $"{ing.item.itemName}: {has}/{ing.amount}\n";
            if (has < ing.amount) canCook = false;
        }
        if (ingredientsText != null) ingredientsText.text = reqText;

        if (statusText != null)
        {
            if (!unlocked) 
            {
                statusText.text = "Đã khóa";
                statusText.color = lockedColor;
            }
            else if (!canCook)
            {
                statusText.text = "Thiếu đồ";
                statusText.color = missingItemColor;
            }
            else
            {
                statusText.text = "Sẵn sàng";
                statusText.color = readyColor;
            }
        }

        if (cookButton != null)
        {
            cookButton.onClick.RemoveAllListeners();
            cookButton.onClick.AddListener(() => menuUI.SelectRecipe(recipe));
        }
        else 
        {
            Button selfBtn = GetComponent<Button>();
            if (selfBtn != null)
            {
                selfBtn.onClick.RemoveAllListeners();
                selfBtn.onClick.AddListener(() => menuUI.SelectRecipe(recipe));
            }
        }
    }
}
