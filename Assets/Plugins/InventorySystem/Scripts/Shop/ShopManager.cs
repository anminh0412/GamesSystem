// =============================================================================
// ShopManager.cs
// NPC/vendor shop — implements IShop with dynamic pricing.
// =============================================================================
// Attach to an NPC or world vendor. Configure stock via ShopStockSO.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem
{
    /// <summary>
    /// Manages a single vendor's stock, pricing, and buy/sell transactions.
    /// </summary>
    public class ShopManager : MonoBehaviour, IShop
    {
        #region Inspector

        [Header("Stock")]
        [Tooltip("Drag the ShopStockSO asset for this vendor here.")]
        [SerializeField] private ShopStockSO _stock;

        [Header("Events")]
        public UnityEvent<InventoryItem, float> OnItemBoughtEvent;
        public UnityEvent<InventoryItem, float> OnItemSoldEvent;

        #endregion

        #region Runtime stock tracking (for limited-stock items)

        // key = item.ItemID, value = remaining quantity (-1 = unlimited)
        private Dictionary<string, int> _availableStock = new Dictionary<string, int>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitialiseStock();
        }

        private void InitialiseStock()
        {
            _availableStock.Clear();
            if (_stock == null) return;

            foreach (var entry in _stock.Stock)
            {
                if (entry.Item == null) continue;
                _availableStock[entry.Item.ItemID] = entry.Stock;
            }
        }

        #endregion

        #region IShop

        public string ShopID => _stock != null ? _stock.ShopID : "Unknown";

        // ── Buy ───────────────────────────────────────────────────────────────

        public bool BuyItem(ItemSO item, int amount, IInventory playerInventory, string currencyID)
        {
            if (item == null || playerInventory == null) return false;

            var entry = FindEntry(item.ItemID);
            if (entry == null)
            {
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Item '{item.ItemName}' not found in shop '{ShopID}'.");
                return false;
            }

            // Check stock
            if (_availableStock.TryGetValue(item.ItemID, out int stock) && stock >= 0 && stock < amount)
            {
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Not enough stock for '{item.ItemName}'.");
                return false;
            }

            float totalCost = GetBuyPrice(item) * amount;
            var   currency  = _stock?.Currency;
            if (currency == null)
            {
                Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} Shop '{ShopID}' has no currency configured.");
                return false;
            }

            var currencyMgr = ServiceLocator.TryGet<CurrencyManager>();
            if (currencyMgr != null && !currencyMgr.Spend(currency.CurrencyID, (long)totalCost))
                return false;

            int leftover = playerInventory.AddItem(item, amount);
            if (leftover > 0)
            {
                // Refund for items that didn't fit
                currencyMgr?.Add(currency.CurrencyID, (long)(GetBuyPrice(item) * leftover));
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Buy: {leftover} unit(s) couldn't fit in inventory, refunded.");
            }

            int bought = amount - leftover;
            if (bought > 0 && stock >= 0)
                _availableStock[item.ItemID] = stock - bought;

            InventoryEventBus.Publish(new ShopTransactionEvent
            {
                ShopID = ShopID,
                Item   = null, // item is a SO reference, not an instance
                Price  = GetBuyPrice(item) * bought,
                IsBuy  = true
            });
            OnItemBoughtEvent?.Invoke(null, totalCost);
            return bought > 0;
        }

        // ── Sell ──────────────────────────────────────────────────────────────

        public bool SellItem(InventoryItem item, IInventory playerInventory, string currencyID)
        {
            if (item == null || playerInventory == null) return false;
            if (item.Data.IsQuestItem || item.Data.IsUntradeable)
            {
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Cannot sell '{item.Data.ItemName}' (quest/untradeable).");
                return false;
            }

            float price = GetSellPrice(item.Data);

            if (!playerInventory.RemoveItemInstance(item))
                return false;

            var currencyMgr = ServiceLocator.TryGet<CurrencyManager>();
            var currency    = _stock?.Currency;
            if (currency != null && currencyMgr != null)
                currencyMgr.Add(currency.CurrencyID, (long)price);

            InventoryEventBus.Publish(new ShopTransactionEvent
            {
                ShopID = ShopID,
                Item   = item,
                Price  = price,
                IsBuy  = false
            });
            OnItemSoldEvent?.Invoke(item, price);
            return true;
        }

        // ── Pricing ───────────────────────────────────────────────────────────

        public float GetBuyPrice(ItemSO item)
        {
            if (item == null) return 0f;
            var entry = FindEntry(item.ItemID);
            if (entry == null) return item.BaseValue;

            float basePrice = entry.PriceOverride > 0f ? entry.PriceOverride : item.BaseValue;
            float rarityMul = GetRarityMultiplier(item.Rarity);
            return basePrice * rarityMul * entry.PriceMultiplier;
        }

        public float GetSellPrice(ItemSO item)
        {
            if (item == null) return 0f;
            float sellRatio = _stock != null ? _stock.SellRatio : InventorySystemConstants.DEFAULT_SELL_RATIO;
            return GetBuyPrice(item) * sellRatio;
        }

        #endregion

        #region Helpers

        private ShopEntry FindEntry(string itemID)
        {
            if (_stock == null) return null;
            foreach (var entry in _stock.Stock)
                if (entry.Item != null && entry.Item.ItemID == itemID)
                    return entry;
            return null;
        }

        private static float GetRarityMultiplier(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common    => 1.0f,
            ItemRarity.Uncommon  => 1.5f,
            ItemRarity.Rare      => 2.5f,
            ItemRarity.Epic      => 5.0f,
            ItemRarity.Legendary => 10.0f,
            ItemRarity.Artifact  => 20.0f,
            _                    => 1.0f
        };

        #endregion

        #region Stock Query

        /// <summary>Returns how many units of an item the shop has left (-1 = unlimited).</summary>
        public int GetAvailableStock(string itemID)
        {
            _availableStock.TryGetValue(itemID, out int stock);
            return stock;
        }

        /// <summary>Read-only view of all shop entries from the stock SO.</summary>
        public IReadOnlyList<ShopEntry> AllEntries => _stock?.Stock;

        #endregion
    }
}
