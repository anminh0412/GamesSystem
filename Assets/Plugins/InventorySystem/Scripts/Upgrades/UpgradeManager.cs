// =============================================================================
// UpgradeManager.cs
// Handles item upgrade transactions: material checks, currency spend, RNG.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Singleton-style manager for the item upgrade system.
    /// Resolves UpgradeDataSO from a registered list and executes upgrade steps.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        #region Inspector

        [Header("Upgrade Data")]
        [Tooltip("All UpgradeDataSO assets available in the game.")]
        [SerializeField] private List<UpgradeDataSO> _upgradeData = new List<UpgradeDataSO>();

        #endregion

        #region Lookup

        // key = ItemID
        private Dictionary<string, UpgradeDataSO> _dataMap = new Dictionary<string, UpgradeDataSO>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ServiceLocator.Register<UpgradeManager>(this);
            BuildLookup();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<UpgradeManager>();
        }

        private void BuildLookup()
        {
            _dataMap.Clear();
            foreach (var data in _upgradeData)
            {
                if (data?.TargetItem == null) continue;
                _dataMap[data.TargetItem.ItemID] = data;
            }
        }

        #endregion

        #region Public API

        /// <summary>Returns the UpgradeDataSO for the given item, or null if not upgradable.</summary>
        public UpgradeDataSO GetUpgradeData(string itemID)
        {
            _dataMap.TryGetValue(itemID, out var data);
            return data;
        }

        /// <summary>
        /// Attempts to upgrade <paramref name="item"/> by one level.
        /// Deducts materials and currency on success.
        /// </summary>
        public UpgradeResult TryUpgrade(InventoryItem item, IInventory materialInventory)
        {
            if (item == null) return UpgradeResult.ItemNotUpgradable;

            var data = GetUpgradeData(item.Data.ItemID);
            if (data == null) return UpgradeResult.ItemNotUpgradable;

            int nextLevel = item.UpgradeLevel + 1;
            if (nextLevel > data.MaxLevel) return UpgradeResult.MaxLevelReached;

            var step = data.GetStep(nextLevel);
            if (step == null) return UpgradeResult.MaxLevelReached;

            var currencyMgr = ServiceLocator.TryGet<CurrencyManager>();

            // Validate materials
            foreach (var mat in step.RequiredMaterials)
            {
                if (mat.Item == null) continue;
                if (!materialInventory.HasItem(mat.Item.ItemID, mat.Amount))
                    return UpgradeResult.MissingMaterials;
            }

            // Validate currency
            if (step.CurrencyType != null && currencyMgr != null)
            {
                if (!currencyMgr.CanAfford(step.CurrencyType.CurrencyID, (long)step.CurrencyCost))
                    return UpgradeResult.InsufficientCurrency;
            }

            // RNG check
            if (Random.value > step.SuccessChance)
            {
                // Consume materials but fail upgrade
                ConsumeStepMaterials(step, materialInventory, currencyMgr);
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Upgrade of '{item.Data.ItemName}' to +{nextLevel} FAILED (chance={step.SuccessChance:P0}).");
                return UpgradeResult.MissingMaterials; // reuse closest result; UI can check separately
            }

            // Consume materials & currency
            ConsumeStepMaterials(step, materialInventory, currencyMgr);

            // Apply upgrade
            item.UpgradeLevel = nextLevel;

            InventoryEventBus.Publish(new UpgradeCompletedEvent { Item = item, NewLevel = nextLevel });
            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} '{item.Data.ItemName}' upgraded to +{nextLevel}.");
            return UpgradeResult.Success;
        }

        private static void ConsumeStepMaterials(UpgradeStep step, IInventory inventory, CurrencyManager currencyMgr)
        {
            foreach (var mat in step.RequiredMaterials)
            {
                if (mat.Item == null) continue;
                inventory.RemoveItem(mat.Item.ItemID, mat.Amount);
            }

            if (step.CurrencyType != null && currencyMgr != null)
                currencyMgr.Spend(step.CurrencyType.CurrencyID, (long)step.CurrencyCost);
        }

        /// <summary>
        /// Preview: returns the expected stat changes at <paramref name="targetLevel"/>
        /// without consuming any resources.
        /// </summary>
        public UpgradeStep PreviewStep(string itemID, int targetLevel)
        {
            var data = GetUpgradeData(itemID);
            return data?.GetStep(targetLevel);
        }

        #endregion
    }
}
