// =============================================================================
// IInventory.cs
// Core inventory container contract.
// =============================================================================

using System.Collections.Generic;

namespace InventorySystem
{
    /// <summary>
    /// Defines the public API for any inventory container
    /// (player bag, chest, vendor stock, etc.).
    /// </summary>
    public interface IInventory
    {
        /// <summary>Unique container identifier (e.g. "PlayerBag", "Chest_01").</summary>
        string InventoryID { get; }

        /// <summary>Human-readable display name shown in UI.</summary>
        string DisplayName { get; }

        /// <summary>All runtime item instances currently stored.</summary>
        IReadOnlyList<InventoryItem> Items { get; }

        /// <summary>Maximum weight the container can hold (0 = unlimited).</summary>
        float MaxWeight { get; }

        /// <summary>Current total weight of all contained items.</summary>
        float CurrentWeight { get; }

        // ── CRUD ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Attempts to add <paramref name="amount"/> units of <paramref name="item"/>.
        /// Returns the number of units that could NOT be added (0 = full success).
        /// </summary>
        int AddItem(ItemSO item, int amount = 1);

        /// <summary>
        /// Removes <paramref name="amount"/> units identified by <paramref name="itemID"/>.
        /// Returns true if the removal fully succeeded.
        /// </summary>
        bool RemoveItem(string itemID, int amount = 1);

        /// <summary>Removes the exact runtime item instance.</summary>
        bool RemoveItemInstance(InventoryItem instance);

        /// <summary>Returns true if the container holds at least <paramref name="amount"/> of the item.</summary>
        bool HasItem(string itemID, int amount = 1);

        /// <summary>Returns the total count of a specific item across all slots.</summary>
        int GetItemCount(string itemID);

        /// <summary>Clears all items from the inventory.</summary>
        void Clear();

        // ── Query ─────────────────────────────────────────────────────────────

        /// <summary>Returns true when the container can accept no more items.</summary>
        bool IsFull { get; }
    }
}
