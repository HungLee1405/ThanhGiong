using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Cooking Recipe", menuName = "Cooking/Recipe")]
public class CookingRecipe : ScriptableObject
{
    public string recipeId;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Ingredients")]
    public List<RecipeIngredient> ingredients;

    [Header("Output")]
    public ItemData outputItem;
    public int outputAmount = 1;
    public float cookTime = 5f;
    public float hungerRestore = 10f;

    [Header("Unlock Condition")]
    public int unlockDay = 1;
    public bool requireCompletedStep = false;
    public QuestStepType requiredStepType;

    [Tooltip("Keep for backwards compatibility in inspector")]
    public string unlockQuestType;
    public string lockedHint = "Chưa mở khóa công thức này";
}

[System.Serializable]
public class RecipeIngredient
{
    public ItemData item;
    public int amount;
}
