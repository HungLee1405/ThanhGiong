using UnityEngine;
using System.Collections.Generic;

public class CookingMenuUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject menuPanel;
    
    [Header("Recipes")]
    public List<CookingRecipe> availableRecipes;
    
    [Header("UI Prefabs")]
    public GameObject recipeRowPrefab;
    public Transform recipeListContent;

    public static bool IsMenuOpen = false;

    private CookingPot currentPot;
    private PlayerInventory currentInventory;

    private void Start()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }

    public void OpenMenu(CookingPot pot, PlayerInventory inventory)
    {
        currentPot = pot;
        currentInventory = inventory;
        
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
        
        IsMenuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshUI();
    }

    public void CloseMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
        
        IsMenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentPot = null;
        currentInventory = null;
    }

    private void Update()
    {
        if (menuPanel != null && menuPanel.activeSelf && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMenu();
        }
    }

    public void RefreshUI()
    {
        if (recipeListContent == null || recipeRowPrefab == null)
        {
            Debug.LogWarning("Chưa gán recipeRowPrefab hoặc recipeListContent trong Inspector.");
            return;
        }

        foreach (Transform child in recipeListContent)
        {
            Destroy(child.gameObject);
        }

        if (currentInventory == null) return;

        foreach (var recipe in availableRecipes)
        {
            if (recipe == null) continue;

            GameObject rowGo = Instantiate(recipeRowPrefab, recipeListContent);
            RecipeRowUI rowUI = rowGo.GetComponent<RecipeRowUI>();
            if (rowUI != null)
            {
                rowUI.Setup(recipe, this, currentInventory);
            }
        }
    }

    // Called by UI Button
    public void StartCooking(CookingRecipe recipe)
    {
        if (currentPot == null || currentInventory == null) return;

        if (!IsRecipeUnlocked(recipe))
        {
            Debug.Log(recipe.lockedHint);
            return;
        }

        foreach (var ingredient in recipe.ingredients)
        {
            if (!currentInventory.HasItem(ingredient.item.itemId, ingredient.amount))
            {
                Debug.Log("Không đủ nguyên liệu: " + ingredient.item.itemName);
                return;
            }
        }

        currentPot.StartDataDrivenCooking(recipe);
        CloseMenu();
    }

    public bool IsRecipeUnlocked(CookingRecipe recipe)
    {
        QuestManager qm = FindFirstObjectByType<QuestManager>();
        if (qm == null) return true;

        // Không khóa lại khi qua ngày mới vì điều kiện đã đạt
        if (recipe.requireCompletedStep)
        {
            if (qm.HasCompletedStepType(recipe.requiredStepType)) return true;
        }
        else if (!string.IsNullOrEmpty(recipe.unlockQuestType))
        {
            if (System.Enum.TryParse(recipe.unlockQuestType, out QuestStepType type))
            {
                if (qm.HasCompletedStepType(type)) return true;
            }
        }

        if (qm.currentDay < recipe.unlockDay) return false;

        return true;
    }
}
