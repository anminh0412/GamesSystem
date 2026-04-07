// =============================================================================
// IItemAction.cs
// Pluggable action executed when a player interacts with an item.
// =============================================================================

namespace InventorySystem
{
    /// <summary>
    /// Implement this interface to define a custom action (Use, Equip, Drop, etc.)
    /// that appears in the item context menu. Actions are composable — an item
    /// can expose multiple actions simultaneously.
    /// </summary>
    public interface IItemAction
    {
        /// <summary>Label shown in the context menu (e.g. "Use", "Equip", "Drop").</summary>
        string ActionName { get; }

        /// <summary>
        /// Whether this action is currently valid for the given item.
        /// Used to grey-out or hide the menu entry.
        /// </summary>
        bool CanExecute(InventoryItem item, IInventory inventory);

        /// <summary>Execute the action.</summary>
        void Execute(InventoryItem item, IInventory inventory);
    }
}
