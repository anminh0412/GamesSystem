// =============================================================================
// UpgradeDataSO.cs
// Defines the upgrade path for an item — materials, cost, and stat deltas.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    // ── Per-level upgrade step ────────────────────────────────────────────────

    [Serializable]
    public class UpgradeStep
    {
        [Tooltip("Target level (e.g. 1 = upgrading from base to +1).")]
        public int Level = 1;

        [Header("Material Requirements")]
        public List<RecipeIngredient> RequiredMaterials = new List<RecipeIngredient>();

        [Header("Currency Cost")]
        public CurrencySO CurrencyType;
        [Min(0f)] public float CurrencyCost = 100f;

        [Header("Stat Modifiers")]
        [Tooltip("Additive stat changes applied when this level is reached.")]
        public float DamageBonus   = 0f;
        public float DefenseBonus  = 0f;
        public float DurabilityBonus = 0f;

        [Tooltip("Multiplicative modifier (e.g. 1.1 = +10%).")]
        [Min(1f)]
        public float StatMultiplier = 1f;

        [Tooltip("Chance that the upgrade succeeds (1.0 = guaranteed).")]
        [Range(0f, 1f)]
        public float SuccessChance = 1f;
    }

    // =========================================================================

    [CreateAssetMenu(
        fileName = "New Upgrade Data",
        menuName  = "Inventory System/Upgrade Data",
        order     = 40)]
    public class UpgradeDataSO : ScriptableObject
    {
        #region Data

        [Header("Upgrade Path")]
        [Tooltip("The item this upgrade path applies to.")]
        [SerializeField] private ItemSO _targetItem;

        [Tooltip("Sequential upgrade steps from +1 to max level.")]
        [SerializeField] private List<UpgradeStep> _steps = new List<UpgradeStep>();

        #endregion

        #region Public Properties

        public ItemSO              TargetItem => _targetItem;
        public IReadOnlyList<UpgradeStep> Steps => _steps;
        public int                 MaxLevel   => _steps.Count;

        #endregion

        #region Query

        /// <summary>Returns the UpgradeStep for going TO <paramref name="targetLevel"/>.</summary>
        public UpgradeStep GetStep(int targetLevel)
        {
            foreach (var step in _steps)
                if (step.Level == targetLevel)
                    return step;
            return null;
        }

        /// <summary>Checks if the player can afford an upgrade step from <paramref name="inventory"/>.</summary>
        public bool CanUpgrade(int currentLevel, IInventory inventory, CurrencyManager currencyManager)
        {
            var step = GetStep(currentLevel + 1);
            if (step == null) return false;

            // Check materials
            foreach (var mat in step.RequiredMaterials)
            {
                if (mat.Item == null) continue;
                if (!inventory.HasItem(mat.Item.ItemID, mat.Amount)) return false;
            }

            // Check currency
            if (step.CurrencyType != null && currencyManager != null)
            {
                if (currencyManager.GetBalance(step.CurrencyType.CurrencyID) < (long)step.CurrencyCost)
                    return false;
            }

            return true;
        }

        #endregion
    }
}
