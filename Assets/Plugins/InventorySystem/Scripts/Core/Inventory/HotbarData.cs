// =============================================================================
// HotbarData.cs
// Runtime hotbar — stores references to inventory items on numbered slots.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Holds the binding between hotbar slot indices and InventoryItem instances.
    /// An item must already exist in an inventory before it can be bound to a hotbar.
    /// </summary>
    public class HotbarData
    {
        #region Data

        private readonly InventoryItem[] _slots;
        public int PlayerIndex { get; }
        public int SlotCount   => _slots.Length;

        #endregion

        #region Events

        /// <summary>Fired when any hotbar slot changes. Arg = slot index.</summary>
        public event Action<int> OnSlotChanged;

        /// <summary>Fired when the active hotbar selection changes.</summary>
        public event Action<int> OnActiveSlotChanged;

        #endregion

        #region Active Selection

        private int _activeSlot = 0;

        public int ActiveSlot
        {
            get => _activeSlot;
            set
            {
                int clamped = Mathf.Clamp(value, 0, _slots.Length - 1);
                if (_activeSlot == clamped) return;
                _activeSlot = clamped;
                OnActiveSlotChanged?.Invoke(_activeSlot);
            }
        }

        public InventoryItem ActiveItem => _slots[_activeSlot];

        #endregion

        #region Constructor

        public HotbarData(int playerIndex, int slotCount = InventorySystemConstants.DEFAULT_HOTBAR_SLOTS)
        {
            PlayerIndex = playerIndex;
            _slots      = new InventoryItem[Mathf.Clamp(slotCount, 1, InventorySystemConstants.MAX_HOTBAR_SLOTS)];
        }

        #endregion

        #region API

        /// <summary>Returns the item bound to <paramref name="slot"/>, or null.</summary>
        public InventoryItem GetSlot(int slot)
        {
            if (!IsValid(slot)) return null;
            return _slots[slot];
        }

        /// <summary>Binds <paramref name="item"/> to <paramref name="slot"/>. Pass null to clear.</summary>
        public void SetSlot(int slot, InventoryItem item)
        {
            if (!IsValid(slot)) return;
            _slots[slot] = item;
            OnSlotChanged?.Invoke(slot);
        }

        /// <summary>Clears all hotbar slots.</summary>
        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i] = null;
                OnSlotChanged?.Invoke(i);
            }
        }

        /// <summary>
        /// Finds the first slot that references <paramref name="item"/> and clears it.
        /// Call when an item is dropped/destroyed to prevent dangling references.
        /// </summary>
        public void RemoveItem(InventoryItem item)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == item)
                    SetSlot(i, null);
            }
        }

        private bool IsValid(int slot) => slot >= 0 && slot < _slots.Length;

        #endregion

        #region Save Data

        public HotbarSaveData CaptureState()
        {
            var data = new HotbarSaveData
            {
                PlayerIndex  = PlayerIndex,
                SlotCount    = _slots.Length,
                ActiveSlot   = _activeSlot,
                BoundItems   = new List<HotbarSlotEntry>()
            };

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                    data.BoundItems.Add(new HotbarSlotEntry { SlotIndex = i, InstanceID = _slots[i].InstanceID });
            }

            return data;
        }

        /// <summary>
        /// Restores hotbar bindings by looking up items from a resolved inventory.
        /// Call after inventory has been fully restored.
        /// </summary>
        public void RestoreState(HotbarSaveData data, Inventory inventory)
        {
            _activeSlot = data.ActiveSlot;
            Clear();

            foreach (var entry in data.BoundItems)
            {
                if (!IsValid(entry.SlotIndex)) continue;
                var item = inventory.GetByInstanceID(entry.InstanceID);
                if (item != null)
                    _slots[entry.SlotIndex] = item;
            }
        }

        #endregion
    }

    // ── Save data POCOs ───────────────────────────────────────────────────────

    [Serializable]
    public class HotbarSlotEntry
    {
        public int    SlotIndex;
        public string InstanceID;
    }

    [Serializable]
    public class HotbarSaveData
    {
        public int                 PlayerIndex;
        public int                 SlotCount;
        public int                 ActiveSlot;
        public List<HotbarSlotEntry> BoundItems;
    }
}
