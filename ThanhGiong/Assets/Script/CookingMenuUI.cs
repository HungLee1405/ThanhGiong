using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CookingMenuUI : MonoBehaviour
{
    [Header("UI Elements - Visual Container")]
    public GameObject visualContainer;
    public GameObject menuPanel; // Legacy mapping
    public GameObject darkOverlay;

    [Header("Main Panel Elements")]
    public GameObject mainPanel;
    public Button closeButton;
    
    [Header("Recipes List")]
    public List<CookingRecipe> availableRecipes;
    public GameObject recipeRowPrefab;
    public Transform recipeListContent;

    [Header("Detail Panel")]
    public Image recipeIcon;
    public TextMeshProUGUI recipeNameText;
    public TextMeshProUGUI recipeDescriptionText;
    public Transform ingredientListContent;
    public GameObject ingredientRowPrefab;
    public TextMeshProUGUI cookTimeText;
    public TextMeshProUGUI effectText;
    public TextMeshProUGUI lockedHintText;
    public Button cookButton;

    [Header("Progress Panel")]
    public GameObject progressPanel;
    public TextMeshProUGUI progressTitleText;
    public Image progressFillImage;
    public TextMeshProUGUI progressText;

    [Header("Pending Output Panel")]
    public GameObject pendingOutputPanel;
    public Image pendingOutputIcon;
    public TextMeshProUGUI pendingOutputText;
    public Button takeOutputButton;

    public static bool IsMenuOpen = false;

    private CookingPot currentPot;
    private PlayerInventory currentInventory;
    private CookingRecipe selectedRecipe;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(CloseMenu);
        if (cookButton != null) cookButton.onClick.AddListener(OnCookButtonClicked);
        if (takeOutputButton != null) takeOutputButton.onClick.AddListener(OnTakeOutputClicked);
    }

    private void Start()
    {
        HideAllPanels();
    }

    private void HideAllPanels()
    {
        if (visualContainer != null) visualContainer.SetActive(false);
        if (darkOverlay != null) darkOverlay.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
        if (progressPanel != null) progressPanel.SetActive(false);
        if (pendingOutputPanel != null) pendingOutputPanel.SetActive(false);
        
        if (menuPanel != null && menuPanel != mainPanel) menuPanel.SetActive(false);
    }

    public void OpenMenu(CookingPot pot, PlayerInventory inventory)
    {
        currentPot = pot;
        currentInventory = inventory;
        
        IsMenuOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (visualContainer != null) visualContainer.SetActive(true);
        if (darkOverlay != null) darkOverlay.SetActive(true);

        // Determine which panel to show based on pot state
        if (pot.HasPendingOutput)
        {
            ShowPendingOutputPanel();
        }
        else if (pot.IsCooking)
        {
            ShowProgressPanel();
        }
        else
        {
            ShowMainPanel();
        }
    }

    public void CloseMenu()
    {
        HideAllPanels();
        
        IsMenuOpen = false;

        bool keepCursor = PauseMenuManager.isPaused || NetworkLobbyCoordinator.IsOnlineLobbyActive;
        Cursor.lockState = keepCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = keepCursor;

        currentPot = null;
        currentInventory = null;
    }

    private void Update()
    {
        if (IsMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (IsMenuOpen && UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseMenu();
        }

        if (IsMenuOpen && currentPot != null)
        {
            if (currentPot.IsCooking && progressPanel != null && progressPanel.activeSelf)
            {
                if (progressFillImage != null) progressFillImage.fillAmount = currentPot.CookingProgress;
                if (progressText != null) progressText.text = Mathf.FloorToInt(currentPot.CookingProgress * 100f) + "%";
                if (progressTitleText != null) progressTitleText.text = "Đang nấu " + currentPot.CurrentRecipeName + "...";
            }
            else if (progressPanel != null && progressPanel.activeSelf)
            {
                if (currentPot.HasPendingOutput)
                {
                    // Cooking finished while menu was open, and inventory is full (pending output)
                    ShowPendingOutputPanel();
                }
                else if (!currentPot.IsCooking)
                {
                    // Cooking finished successfully and item was placed directly in inventory
                    CloseMenu();
                }
            }
        }
    }

    private void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (menuPanel != null && menuPanel != mainPanel) menuPanel.SetActive(true);
        
        if (progressPanel != null) progressPanel.SetActive(false);
        if (pendingOutputPanel != null) pendingOutputPanel.SetActive(false);
        
        RefreshUI();
    }

    private void ShowProgressPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (menuPanel != null && menuPanel != mainPanel) menuPanel.SetActive(false);
        
        if (progressPanel != null) progressPanel.SetActive(true);
        if (pendingOutputPanel != null) pendingOutputPanel.SetActive(false);
    }

    private void ShowPendingOutputPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (menuPanel != null && menuPanel != mainPanel) menuPanel.SetActive(false);
        
        if (progressPanel != null) progressPanel.SetActive(false);
        if (pendingOutputPanel != null) pendingOutputPanel.SetActive(true);

        if (pendingOutputIcon != null && currentPot.PendingOutputItem != null)
        {
            pendingOutputIcon.sprite = currentPot.PendingOutputItem.itemIcon;
            pendingOutputIcon.gameObject.SetActive(true);
        }
        if (pendingOutputText != null && currentPot.PendingOutputItem != null)
        {
            pendingOutputText.text = "Nhận " + currentPot.PendingOutputAmount + " " + currentPot.PendingOutputItem.itemName;
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

        bool first = true;
        foreach (var recipe in availableRecipes)
        {
            if (recipe == null) continue;

            GameObject rowGo = Instantiate(recipeRowPrefab, recipeListContent);
            RecipeRowUI rowUI = rowGo.GetComponent<RecipeRowUI>();
            if (rowUI != null)
            {
                rowUI.Setup(recipe, this, currentInventory);
            }

            if (first)
            {
                SelectRecipe(recipe);
                first = false;
            }
        }
    }

    public void SelectRecipe(CookingRecipe recipe)
    {
        selectedRecipe = recipe;
        if (recipeIcon != null) { recipeIcon.sprite = recipe.icon; recipeIcon.gameObject.SetActive(true); }
        if (recipeNameText != null) recipeNameText.text = recipe.displayName;
        if (recipeDescriptionText != null) recipeDescriptionText.text = recipe.description;
        if (cookTimeText != null) cookTimeText.text = "Thời gian nấu: " + recipe.cookTime + "s";
        if (effectText != null) effectText.text = "Thành phẩm: " + (recipe.outputItem != null ? recipe.outputItem.itemName : "N/A");
        
        bool unlocked = IsRecipeUnlocked(recipe);
        if (lockedHintText != null)
        {
            lockedHintText.gameObject.SetActive(!unlocked);
            lockedHintText.text = recipe.lockedHint;
        }
        if (recipeIcon != null)
        {
            var color = recipeIcon.color;
            color.a = unlocked ? 1f : 0.5f;
            recipeIcon.color = color;
        }

        bool canCook = unlocked;

        if (ingredientListContent != null)
        {
            foreach (Transform child in ingredientListContent)
            {
                Destroy(child.gameObject);
            }

            if (ingredientRowPrefab != null && currentInventory != null)
            {
                foreach (var ing in recipe.ingredients)
                {
                    GameObject go = Instantiate(ingredientRowPrefab, ingredientListContent);
                    IngredientRowUI rowUI = go.GetComponent<IngredientRowUI>();
                    int has = currentInventory.GetItemAmount(ing.item.itemId);
                    if (rowUI != null)
                    {
                        rowUI.Setup(ing.item, ing.amount, has);
                    }
                    if (has < ing.amount) canCook = false;
                }
            }
        }

        if (cookButton != null)
        {
            cookButton.interactable = canCook;
        }
    }

    private void OnCookButtonClicked()
    {
        if (selectedRecipe != null)
        {
            StartCooking(selectedRecipe);
        }
    }

    private void OnTakeOutputClicked()
    {
        if (currentPot != null && currentPot.HasPendingOutput)
        {
            currentPot.TryTakePendingOutput();
            CloseMenu();
        }
    }

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
        ShowProgressPanel();
    }

    public bool IsRecipeUnlocked(CookingRecipe recipe)
    {
        QuestManager qm = FindFirstObjectByType<QuestManager>();
        if (qm == null) return true;

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
