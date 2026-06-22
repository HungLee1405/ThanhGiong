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
    
    private CookingRecipe recipe;
    private CookingMenuUI menuUI;

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

        if (cookButton != null)
        {
            cookButton.interactable = canCook;
            cookButton.onClick.RemoveAllListeners();
            cookButton.onClick.AddListener(() => menuUI.StartCooking(recipe));
        }
    }
}
