// =============================================================================
// Inventory.cs
// The primary inventory container — implements IInventory and ISaveable.
// =============================================================================
// One Inventory object represents a single container (player bag, chest, etc.).
// It owns an InventoryGrid for spatial layout and a flat item list for weight
// and count queries.
//
// Usage:
//   var inventory = new Inventory("PlayerBag", "Player Bag", 8, 5);
//   inventory.AddItem(swordSO, 1);
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Core runtime inventory container.
    /// Manages an <see cref="InventoryGrid"/> (Tetris layout) and a flat item list.
    /// </summary>
    public class Inventory : IInventory, ISaveable
    {
        #region Fields

        private readonly List<InventoryItem> _items = new List<InventoryItem>();
        private readonly InventoryGrid       _grid;
        private readonly InventoryType       _type;

        #endregion

        #region IInventory Properties

        public string InventoryID   { get; private set; }
        public string DisplayName   { get; private set; }
        public float  MaxWeight     { get; private set; }
        public InventoryType Type   => _type;
        public InventoryGrid Grid   => _grid;

        public float CurrentWeight
        {
            get
            {
                float w = 0f;
                foreach (var item in _items)
                    w += item.Data.Weight * item.StackCount;
                return w;
            }
        }

        public bool IsFull
        {
            get
            {
                if (MaxWeight > 0f && CurrentWeight >= MaxWeight) return true;
                return _grid.EmptyCellCount() == 0;
            }
        }

        public IReadOnlyList<InventoryItem> Items => _items;

        #endregion

        #region Events

        /// <summary>Raised when an item is successfully added. Args: item, amountAdded.</summary>
        public event Action<InventoryItem, int> OnItemAdded;

        /// <summary>Raised when items are removed. Args: itemID, amountRemoved.</summary>
        public event Action<string, int> OnItemRemoved;

        /// <summary>Raised when an item's stack count changes.</summary>
        public event Action<InventoryItem> OnStackChanged;

        /// <summary>Raised when grid layout changes (resize, move, rotate).</summary>
        public event Action OnLayoutChanged;

        /// <summary>Raised when the inventory is cleared.</summary>
        public event Action OnCleared;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new inventory container.
        /// </summary>
        /// <param name="inventoryID">Stable unique identifier (used in save files).</param>
        /// <param name="displayName">Human-readable name for UI.</param>
        /// <param name="columns">Grid width.</param>
        /// <param name="rows">Grid height.</param>
        /// <param name="type">Container type (controls UI and transfer rules).</param>
        /// <param name="maxWeight">Weight limit in kg. 0 = unlimited.</param>
        public Inventory(string inventoryID, string displayName,
                         int columns = 8, int rows = 5,
                         InventoryType type = InventoryType.PlayerBag,
                         float maxWeight = 0f)
        {
            InventoryID = inventoryID;
            DisplayName = displayName;
            MaxWeight   = maxWeight;
            _type       = type;
            _grid       = new InventoryGrid(columns, rows);
        }

        #endregion

        #region IInventory — Add

        /// <summary>
        /// Adds <paramref name="amount"/> units of <paramref name="item"/> to this inventory.
        /// Returns the number of units that could NOT be added (0 = full success).
        /// Stackable items are merged into existing partial stacks before new instances are created.
        /// </summary>
        public int AddItem(ItemSO item, int amount = 1)
        {
            if (item == null)
            {
                Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} AddItem: null ItemSO.");
                return amount;
            }
            if (amount <= 0) return 0;

            int remaining = amount;

            // 1. Try to merge into existing partial stacks (stackable items only)
            if (item.IsStackable)
                remaining = MergeIntoExisting(item, remaining);

            // 2. Create new InventoryItem instances for the rest
            while (remaining > 0)
            {
                int stackSize = Mathf.Min(remaining, item.MaxStackSize);
                var newItem   = new InventoryItem(item, stackSize);

                var pivot = _grid.FindFirstFit(newItem, ShapeRotation.Rot0);
                if (pivot.x < 0)
                {
                    Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Inventory '{DisplayName}' is full. {remaining} unit(s) not added.");
                    break; // grid is full
                }

                if (MaxWeight > 0f && CurrentWeight + item.Weight * stackSize > MaxWeight)
                {
                    Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Weight limit reached in '{DisplayName}'.");
                    break;
                }

                _grid.Place(newItem, pivot, ShapeRotation.Rot0);
                _items.Add(newItem);

                newItem.OnStackChanged += HandleStackChanged;

                OnItemAdded?.Invoke(newItem, stackSize);
                remaining -= stackSize;
            }

            return remaining; // leftover not added
        }

        private int MergeIntoExisting(ItemSO item, int remaining)
        {
            foreach (var existing in _items)
            {
                if (remaining <= 0) break;
                if (existing.Data.ItemID != item.ItemID) continue;
                if (existing.StackCount >= item.MaxStackSize) continue;

                int space = item.MaxStackSize - existing.StackCount;
                int toAdd = Mathf.Min(space, remaining);
                existing.StackCount += toAdd;
                remaining -= toAdd;

                OnItemAdded?.Invoke(existing, toAdd);
            }
            return remaining;
        }

        #endregion

        #region IInventory — Remove

        /// <summary>
        /// Removes <paramref name="amount"/> units of the item identified by
        /// <paramref name="itemID"/>. Deducts from stacks smallest-first.
        /// Returns true when ALL requested units were removed.
        /// </summary>
        public bool RemoveItem(string itemID, int amount = 1)
        {
            if (amount <= 0) return true;

            int remaining = amount;

            // Iterate a copy so we can safely remove from _items
            for (int i = _items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var item = _items[i];
                if (item.Data.ItemID != itemID) continue;

                int toRemove = Mathf.Min(item.StackCount, remaining);
                item.StackCount -= toRemove;
                remaining       -= toRemove;

                if (item.StackCount <= 0)
                    DestroyItem(item);
            }

            if (remaining < amount)
                OnItemRemoved?.Invoke(itemID, amount - remaining);

            return remaining == 0;
        }

        /// <summary>Removes the exact <paramref name="instance"/> from the inventory.</summary>
        public bool RemoveItemInstance(InventoryItem instance)
        {
            if (!_items.Contains(instance)) return false;
            DestroyItem(instance);
            OnItemRemoved?.Invoke(instance.Data.ItemID, instance.StackCount);
            return true;
        }

        private void DestroyItem(InventoryItem item)
        {
            _grid.Remove(item);
            _items.Remove(item);
            item.OnStackChanged -= HandleStackChanged;
        }

        #endregion

        #region IInventory — Query

        public bool HasItem(string itemID, int amount = 1)
            => GetItemCount(itemID) >= amount;

        public int GetItemCount(string itemID)
        {
            int total = 0;
            foreach (var item in _items)
                if (item.Data.ItemID == itemID)
                    total += item.StackCount;
            return total;
        }

        /// <summary>Returns all item instances with the given item ID.</summary>
        public List<InventoryItem> GetAllInstances(string itemID)
        {
            var result = new List<InventoryItem>();
            foreach (var item in _items)
                if (item.Data.ItemID == itemID)
                    result.Add(item);
            return result;
        }

        /// <summary>Returns the runtime instance with the given instance ID, or null.</summary>
        public InventoryItem GetByInstanceID(string instanceID)
        {
            foreach (var item in _items)
                if (item.InstanceID == instanceID)
                    return item;
            return null;
        }

        #endregion

        #region Clear & Resize

        public void Clear()
        {
            foreach (var item in _items)
                item.OnStackChanged -= HandleStackChanged;

            _items.Clear();
            _grid.Clear();
            OnCleared?.Invoke();
        }

        /// <summary>
        /// Resizes the underlying grid at runtime. Items that no longer fit are
        /// returned as a list — the caller is responsible for re-inserting them
        /// or notifying the player.
        /// </summary>
        public List<InventoryItem> Resize(int newColumns, int newRows)
        {
            var displaced = _grid.Resize(newColumns, newRows);
            foreach (var d in displaced)
                _items.Remove(d);

            OnLayoutChanged?.Invoke();
            return displaced;
        }

        #endregion

        #region Move & Rotate (grid operations)

        /// <summary>
        /// Moves a placed item to a new pivot and rotation within this inventory.
        /// Returns false if the target position is invalid.
        /// </summary>
        public bool MoveItem(InventoryItem item, Vector2Int newPivot, ShapeRotation newRotation)
        {
            if (!_items.Contains(item)) return false;
            bool moved = _grid.Move(item, newPivot, newRotation);
            if (moved) OnLayoutChanged?.Invoke();
            return moved;
        }

        /// <summary>
        /// Rotates a placed item by 90° CW. Returns false if the new orientation
        /// does not fit.
        /// </summary>
        public bool RotateItem(InventoryItem item)
        {
            if (!_items.Contains(item)) return false;
            var nextRot = (ShapeRotation)(((int)item.Rotation + 1) % InventorySystemConstants.ROTATION_STATES);
            return MoveItem(item, item.GridPosition, nextRot);
        }

        #endregion

        #region Transfer Between Inventories

        /// <summary>
        /// Transfers <paramref name="amount"/> units of <paramref name="item"/> from this
        /// inventory to <paramref name="target"/>. Returns leftover that didn't fit.
        /// </summary>
        public int TransferTo(InventoryItem item, Inventory target, int amount = -1)
        {
            if (item == null || target == null) return 0;
            if (!_items.Contains(item)) return 0;

            int toTransfer = amount < 0 ? item.StackCount : Mathf.Min(amount, item.StackCount);
            int leftover   = target.AddItem(item.Data, toTransfer);
            int transferred = toTransfer - leftover;

            if (transferred > 0)
                RemoveItem(item.Data.ItemID, transferred);

            return leftover;
        }

        #endregion

        #region Event Handlers

        private void HandleStackChanged(InventoryItem item, int newCount)
        {
            OnStackChanged?.Invoke(item);
        }

        #endregion

        #region ISaveable

        public string SaveKey => $"Inventory_{InventoryID}";

        public object CaptureState()
        {
            var data = new InventorySaveData
            {
                InventoryID = InventoryID,
                DisplayName = DisplayName,
                Columns     = _grid.Columns,
                Rows        = _grid.Rows,
                MaxWeight   = MaxWeight,
                Items       = new List<InventoryItemSaveData>(_items.Count)
            };

            foreach (var item in _items)
            {
                data.Items.Add(new InventoryItemSaveData
                {
                    ItemID           = item.Data.ItemID,
                    InstanceID       = item.InstanceID,
                    StackCount       = item.StackCount,
                    UpgradeLevel     = item.UpgradeLevel,
                    CurrentDurability = item.CurrentDurability,
                    GridCol          = item.GridPosition.x,
                    GridRow          = item.GridPosition.y,
                    Rotation         = (int)item.Rotation
                });
            }

            return data;
        }

        public void RestoreState(object data)
        {
            if (data is not InventorySaveData saveData) return;

            Clear();
            _grid.Resize(saveData.Columns, saveData.Rows);

            var db = ServiceLocator.Get<ItemDatabaseSO>();
            if (db == null)
            {
                Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} ItemDatabase not found in ServiceLocator.");
                return;
            }

            foreach (var savedItem in saveData.Items)
            {
                var itemSO = db.GetItem(savedItem.ItemID);
                if (itemSO == null)
                {
                    Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} Item '{savedItem.ItemID}' not found in database — skipped.");
                    continue;
                }

                var item = new InventoryItem(itemSO, savedItem.InstanceID,
                                             savedItem.StackCount,
                                             savedItem.UpgradeLevel,
                                             savedItem.CurrentDurability);

                var pivot    = new Vector2Int(savedItem.GridCol, savedItem.GridRow);
                var rotation = (ShapeRotation)savedItem.Rotation;

                if (!_grid.Place(item, pivot, rotation))
                {
                    // Fallback: try to find any empty spot
                    pivot = _grid.FindFirstFit(item, rotation);
                    if (pivot.x < 0)
                    {
                        Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} No space for '{itemSO.ItemName}' after load.");
                        continue;
                    }
                    _grid.Place(item, pivot, rotation);
                }

                _items.Add(item);
                item.OnStackChanged += HandleStackChanged;
            }
        }

        #endregion

        #region Utility

        public override string ToString()
            => $"Inventory[{InventoryID}] '{DisplayName}' {_grid.Columns}×{_grid.Rows} — {_items.Count} items";

        #endregion
    }

    // =========================================================================
    // Save Data POCOs (Plain Old C# Objects, JSON-friendly)
    // =========================================================================

    [Serializable]
    public class InventoryItemSaveData
    {
        public string ItemID;
        public string InstanceID;
        public int    StackCount;
        public int    UpgradeLevel;
        public int    CurrentDurability;
        public int    GridCol;
        public int    GridRow;
        public int    Rotation;
    }

    [Serializable]
    public class InventorySaveData
    {
        public string InventoryID;
        public string DisplayName;
        public int    Columns;
        public int    Rows;
        public float  MaxWeight;
        public List<InventoryItemSaveData> Items;
    }
}
