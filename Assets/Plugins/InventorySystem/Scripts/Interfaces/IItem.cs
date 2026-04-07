// =============================================================================
// IItem.cs
// Core item contract — every item in the system must implement this.
// =============================================================================

using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Minimal contract that every inventory item must satisfy.
    /// Implemented by <see cref="ItemSO"/> and all derived ScriptableObjects.
    /// </summary>
    public interface IItem
    {
        /// <summary>Globally unique identifier (set via Inspector or GUID generator).</summary>
        string ItemID { get; }

        /// <summary>Display name (may be localised at runtime).</summary>
        string ItemName { get; }

        /// <summary>Flavour / gameplay description.</summary>
        string Description { get; }

        /// <summary>2D icon used in UI slots.</summary>
        Sprite Icon { get; }

        /// <summary>Item category (Weapon, Armor, Consumable, etc.).</summary>
        ItemCategory Category { get; }

        /// <summary>Rarity tier affecting colour and pricing.</summary>
        ItemRarity Rarity { get; }

        /// <summary>Single-unit weight in kg (used by weight-capacity inventories).</summary>
        float Weight { get; }

        /// <summary>Can multiple units occupy a single slot?</summary>
        bool IsStackable { get; }

        /// <summary>Maximum units per stack (1 if not stackable).</summary>
        int MaxStackSize { get; }

        /// <summary>Grid shape this item occupies (supports Tetris-style layouts).</summary>
        ItemShape Shape { get; }
    }
}
