// =============================================================================
// IShop.cs
// Contract for any shop/vendor container.
// =============================================================================

namespace InventorySystem
{
    /// <summary>
    /// Implemented by shop managers and NPC vendor components.
    /// Exposes Buy/Sell with dynamic pricing.
    /// </summary>
    public interface IShop
    {
        /// <summary>Unique shop identifier (used for save/load).</summary>
        string ShopID { get; }

        /// <summary>Buy an item from the shop into the player inventory.</summary>
        /// <returns>True if the transaction succeeded.</returns>
        bool BuyItem(ItemSO item, int amount, IInventory playerInventory, string currencyID);

        /// <summary>Sell an item from the player inventory to the shop.</summary>
        /// <returns>True if the transaction succeeded.</returns>
        bool SellItem(InventoryItem item, IInventory playerInventory, string currencyID);

        /// <summary>
        /// Calculate the buy price of <paramref name="item"/> from this shop.
        /// Considers rarity multipliers and shop-specific markups.
        /// </summary>
        float GetBuyPrice(ItemSO item);

        /// <summary>
        /// Calculate the sell price the shop will pay for <paramref name="item"/>.
        /// </summary>
        float GetSellPrice(ItemSO item);
    }
}
