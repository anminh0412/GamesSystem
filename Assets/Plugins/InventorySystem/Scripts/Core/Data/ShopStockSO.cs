// =============================================================================
// ShopStockSO.cs
// Defines the stock of an NPC vendor shop.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    [Serializable]
    public class ShopEntry
    {
        [Tooltip("The item sold by this vendor.")]
        public ItemSO Item;

        [Tooltip("How many units available. -1 = unlimited stock.")]
        public int Stock = -1;

        [Tooltip("Override base price. 0 = use item's BaseValue with rarity multiplier.")]
        [Min(0f)]
        public float PriceOverride = 0f;

        [Tooltip("Additional percentage markup/discount on top of base price. (1.0 = no change)")]
        [Min(0f)]
        public float PriceMultiplier = 1f;

        [Tooltip("Minimum player level to see / buy this item.")]
        [Min(1)]
        public int RequiredLevel = 1;
    }

    // =========================================================================

    [CreateAssetMenu(
        fileName = "New Shop Stock",
        menuName  = "Inventory System/Shop Stock",
        order     = 50)]
    public class ShopStockSO : ScriptableObject
    {
        #region Data

        [Header("Shop Identity")]
        [SerializeField] private string _shopID       = "";
        [SerializeField] private string _shopName     = "General Store";

        [Header("Currency")]
        [Tooltip("Currency accepted by this shop.")]
        [SerializeField] private CurrencySO _currency;

        [Header("Sell Ratio")]
        [Tooltip("Fraction of buy price the shop pays when buying from the player.")]
        [Range(0f, 1f)]
        [SerializeField] private float _sellRatio = InventorySystemConstants.DEFAULT_SELL_RATIO;

        [Header("Stock")]
        [SerializeField] private List<ShopEntry> _stock = new List<ShopEntry>();

        #endregion

        #region Public Properties

        public string      ShopID    => _shopID;
        public string      ShopName  => _shopName;
        public CurrencySO  Currency  => _currency;
        public float       SellRatio => _sellRatio;
        public IReadOnlyList<ShopEntry> Stock => _stock;

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Generate Shop ID")]
        private void GenerateID()
        {
            _shopID = System.Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        #endregion
    }
}
