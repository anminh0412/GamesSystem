// =============================================================================
// CraftingRecipeSO.cs
// Defines a crafting recipe: ingredients → result item.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    // ── Ingredient entry ──────────────────────────────────────────────────────

    /// <summary>One ingredient slot in a crafting recipe.</summary>
    [Serializable]
    public class RecipeIngredient
    {
        [Tooltip("The item required.")]
        public ItemSO Item;

        [Tooltip("Amount of this item required.")]
        [Min(1)]
        public int Amount = 1;
    }

    // =========================================================================

    [CreateAssetMenu(
        fileName = "New Recipe",
        menuName  = "Inventory System/Crafting Recipe",
        order     = 30)]
    public class CraftingRecipeSO : ScriptableObject
    {
        #region Recipe Identity

        [Header("Identity")]
        [SerializeField] private string _recipeID   = "";
        [SerializeField] private string _recipeName = "New Recipe";

        [TextArea(1, 3)]
        [SerializeField] private string _description = "";

        #endregion

        #region Station Requirement

        [Header("Station Requirement")]
        [Tooltip("Leave empty to allow crafting anywhere. Match against ICraftable.StationType.")]
        [SerializeField] private string _requiredStationType = "";

        [Tooltip("Minimum player level required to unlock this recipe.")]
        [Min(1)]
        [SerializeField] private int _requiredLevel = 1;

        #endregion

        #region Ingredients

        [Header("Ingredients")]
        [SerializeField] private List<RecipeIngredient> _ingredients = new List<RecipeIngredient>();

        #endregion

        #region Result

        [Header("Result")]
        [Tooltip("Item produced when the recipe succeeds.")]
        [SerializeField] private ItemSO _resultItem;

        [Tooltip("How many units of the result item are produced.")]
        [Min(1)]
        [SerializeField] private int _resultAmount = 1;

        #endregion

        #region Timing & Cost

        [Header("Timing & Cost")]
        [Tooltip("Time in seconds to complete this craft (0 = instant).")]
        [Min(0f)]
        [SerializeField] private float _craftTime = 0f;

        [Tooltip("Currency cost to initiate the craft (optional).")]
        [SerializeField] private CurrencySO _currencyCost;

        [Min(0f)]
        [SerializeField] private float _currencyAmount = 0f;

        #endregion

        #region Public Properties

        public string                     RecipeID             => _recipeID;
        public string                     RecipeName           => _recipeName;
        public string                     Description          => _description;
        public string                     RequiredStationType  => _requiredStationType;
        public int                        RequiredLevel        => _requiredLevel;
        public IReadOnlyList<RecipeIngredient> Ingredients     => _ingredients;
        public ItemSO                     ResultItem           => _resultItem;
        public int                        ResultAmount         => _resultAmount;
        public float                      CraftTime            => _craftTime;
        public CurrencySO                 CurrencyCost         => _currencyCost;
        public float                      CurrencyAmount       => _currencyAmount;

        #endregion

        #region Validation

        /// <summary>
        /// Checks whether <paramref name="inventory"/> contains all required ingredients.
        /// Returns true if the craft can proceed.
        /// </summary>
        public bool CanCraft(IInventory inventory)
        {
            if (_resultItem == null) return false;

            foreach (var ingredient in _ingredients)
            {
                if (ingredient.Item == null) continue;
                if (!inventory.HasItem(ingredient.Item.ItemID, ingredient.Amount))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Consumes all ingredients from <paramref name="inventory"/>.
        /// Call only after <see cref="CanCraft"/> returns true.
        /// </summary>
        public void ConsumeIngredients(IInventory inventory)
        {
            foreach (var ingredient in _ingredients)
            {
                if (ingredient.Item == null) continue;
                inventory.RemoveItem(ingredient.Item.ItemID, ingredient.Amount);
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Generate Recipe ID")]
        private void GenerateID()
        {
            _recipeID = System.Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        #endregion
    }
}
