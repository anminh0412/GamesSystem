// =============================================================================
// InventoryItem.cs
// Runtime representation of a single item stack inside an inventory.
// =============================================================================
// An InventoryItem wraps an immutable ItemSO with mutable runtime state:
//   • stack count
//   • current durability
//   • upgrade level
//   • grid placement (pivot + rotation)
//   • unique instance ID for save/load
// =============================================================================

using System;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Mutable runtime wrapper around a <see cref="ItemSO"/>.
    /// One InventoryItem object = one stack in one inventory slot.
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        #region Core Data

        /// <summary>
        /// Unique instance identifier, stable across save/load.
        /// Used by the save system and UI to track specific item objects.
        /// </summary>
        public string InstanceID { get; private set; }

        /// <summary>The immutable data asset for this item.</summary>
        public ItemSO Data { get; private set; }

        // ── Stack ─────────────────────────────────────────────────────────────

        private int _stackCount;

        /// <summary>Current stack size. Clamped to [1, Data.MaxStackSize].</summary>
        public int StackCount
        {
            get => _stackCount;
            set
            {
                int clamped = Mathf.Clamp(value, 0, Data != null ? Data.MaxStackSize : 1);
                if (_stackCount == clamped) return;
                _stackCount = clamped;
                OnStackChanged?.Invoke(this, _stackCount);
            }
        }

        // ── Durability ────────────────────────────────────────────────────────

        private int _currentDurability;

        /// <summary>Current durability. -1 means "no durability system".</summary>
        public int CurrentDurability
        {
            get => _currentDurability;
            set
            {
                int max = MaxDurability;
                _currentDurability = (max <= 0) ? -1 : Mathf.Clamp(value, 0, max);
            }
        }

        /// <summary>Max durability sourced from the item data (weapon/armor).</summary>
        public int MaxDurability
        {
            get
            {
                if (Data is WeaponItemSO w) return w.MaxDurability;
                if (Data is ArmorItemSO  a) return a.MaxDurability;
                return 0; // not applicable
            }
        }

        // ── Upgrade ───────────────────────────────────────────────────────────

        private int _upgradeLevel;

        /// <summary>Current upgrade level (0 = base).</summary>
        public int UpgradeLevel
        {
            get => _upgradeLevel;
            set
            {
                int max = MaxUpgradeLevel;
                _upgradeLevel = (max <= 0) ? 0 : Mathf.Clamp(value, 0, max);
                OnUpgradeLevelChanged?.Invoke(this, _upgradeLevel);
            }
        }

        public int MaxUpgradeLevel
        {
            get
            {
                if (Data is WeaponItemSO w) return w.MaxUpgradeLevel;
                if (Data is ArmorItemSO  a) return a.MaxUpgradeLevel;
                return 0;
            }
        }

        #endregion

        #region Grid Placement

        /// <summary>Top-left pivot cell in the inventory grid (column, row).</summary>
        public Vector2Int GridPosition { get; set; } = new Vector2Int(-1, -1);

        /// <summary>Current rotation state of this item on the grid.</summary>
        public ShapeRotation Rotation { get; set; } = ShapeRotation.Rot0;

        /// <summary>Returns true when the item has been placed in a grid slot.</summary>
        public bool IsPlaced => GridPosition.x >= 0 && GridPosition.y >= 0;

        #endregion

        #region Events

        /// <summary>Raised whenever the stack count changes. Args: item, newCount.</summary>
        public event Action<InventoryItem, int> OnStackChanged;

        /// <summary>Raised whenever the upgrade level changes. Args: item, newLevel.</summary>
        public event Action<InventoryItem, int> OnUpgradeLevelChanged;

        #endregion

        #region Constructors

        /// <summary>Creates a new item instance from a data asset.</summary>
        public InventoryItem(ItemSO data, int stackCount = 1)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "ItemSO cannot be null.");

            Data         = data;
            InstanceID   = Guid.NewGuid().ToString("N");
            _stackCount  = Mathf.Clamp(stackCount, 1, data.MaxStackSize);

            // Initialise durability
            _currentDurability = MaxDurability > 0 ? MaxDurability : -1;
            _upgradeLevel      = 0;
        }

        /// <summary>
        /// Restore constructor — used by the save system to recreate an existing
        /// item with its saved instance ID, stack count, and other runtime state.
        /// </summary>
        public InventoryItem(ItemSO data, string instanceID, int stackCount,
                             int upgradeLevel, int currentDurability)
        {
            Data               = data ?? throw new ArgumentNullException(nameof(data));
            InstanceID         = string.IsNullOrEmpty(instanceID) ? Guid.NewGuid().ToString("N") : instanceID;
            _stackCount        = Mathf.Clamp(stackCount, 1, data.MaxStackSize);
            _upgradeLevel      = upgradeLevel;
            _currentDurability = currentDurability;
        }

        #endregion

        #region Stack Operations

        /// <summary>
        /// Splits this stack. Returns a new InventoryItem with <paramref name="amount"/> units,
        /// deducting from this stack. Returns null if amount is invalid.
        /// </summary>
        public InventoryItem Split(int amount)
        {
            if (amount <= 0 || amount >= _stackCount) return null;

            _stackCount -= amount;
            OnStackChanged?.Invoke(this, _stackCount);

            var newItem           = new InventoryItem(Data, amount);
            newItem._upgradeLevel = _upgradeLevel;
            return newItem;
        }

        /// <summary>
        /// Merges <paramref name="other"/> into this stack. Returns any leftover
        /// units that didn't fit (0 = full merge).
        /// </summary>
        public int Merge(InventoryItem other)
        {
            if (other == null || other.Data.ItemID != Data.ItemID) return other?.StackCount ?? 0;

            int available = Data.MaxStackSize - _stackCount;
            int toAdd     = Mathf.Min(available, other.StackCount);

            _stackCount       += toAdd;
            other._stackCount -= toAdd;

            OnStackChanged?.Invoke(this, _stackCount);
            if (other._stackCount > 0)
                other.OnStackChanged?.Invoke(other, other._stackCount);

            return other._stackCount;
        }

        #endregion

        #region Shape Helpers

        /// <summary>
        /// Returns the world-grid cells this item occupies, given its current
        /// <see cref="GridPosition"/> and <see cref="Rotation"/>.
        /// </summary>
        public System.Collections.Generic.List<Vector2Int> GetOccupiedCells()
        {
            return Data.Shape.GetWorldCells(GridPosition, Rotation);
        }

        #endregion

        #region Utility

        public override string ToString()
            => $"[{Data.Rarity}] {Data.ItemName} x{_stackCount} (+{_upgradeLevel}) [{InstanceID[..8]}]";

        #endregion
    }
}
