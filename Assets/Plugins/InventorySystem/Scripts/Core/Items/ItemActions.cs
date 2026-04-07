// =============================================================================
// ItemActions.cs
// Built-in IItemAction implementations: Use, Equip, Drop, Destroy.
// =============================================================================
// Add these to a list on any component that wants to display an item context
// menu. You can also create fully custom actions by implementing IItemAction.
// =============================================================================

using UnityEngine;

namespace InventorySystem
{
    // ── Use ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// "Use" action for ConsumableItemSO. Removes one unit and applies the effect.
    /// The actual effect application is delegated to the ConsumableEffect value —
    /// hook into the OnItemUsed event for gameplay-side handling.
    /// </summary>
    public class UseItemAction : IItemAction
    {
        public string ActionName => LocalizationBridge.GetUIString("btn_use");

        public bool CanExecute(InventoryItem item, IInventory inventory)
            => item?.Data is ConsumableItemSO && inventory.HasItem(item.Data.ItemID, 1);

        public void Execute(InventoryItem item, IInventory inventory)
        {
            if (!CanExecute(item, inventory)) return;
            inventory.RemoveItem(item.Data.ItemID, 1);
            InventoryEventBus.Publish(new ItemUsedEvent { Item = item });
            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Used: {item.Data.ItemName}");
        }
    }

    // ── Equip ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// "Equip" action — moves item from bag to equipment inventory.
    /// </summary>
    public class EquipItemAction : IItemAction
    {
        private readonly int _playerIndex;

        public EquipItemAction(int playerIndex = 0)
        {
            _playerIndex = playerIndex;
        }

        public string ActionName => LocalizationBridge.GetUIString("btn_equip");

        public bool CanExecute(InventoryItem item, IInventory inventory)
            => item?.Data is WeaponItemSO || item?.Data is ArmorItemSO;

        public void Execute(InventoryItem item, IInventory inventory)
        {
            if (!CanExecute(item, inventory)) return;

            var mgr       = ServiceLocator.TryGet<InventoryManager>();
            var equipment = mgr?.GetEquipmentInventory(_playerIndex);
            if (equipment == null) return;

            if (inventory is Inventory bag)
                bag.TransferTo(item, equipment, 1);

            InventoryEventBus.Publish(new ItemUsedEvent { Item = item, PlayerIndex = _playerIndex });
            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Equipped: {item.Data.ItemName}");
        }
    }

    // ── Drop ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// "Drop" action — removes the item from the inventory and spawns its world prefab.
    /// </summary>
    public class DropItemAction : IItemAction
    {
        private readonly Transform _dropOrigin;

        public DropItemAction(Transform dropOrigin)
        {
            _dropOrigin = dropOrigin;
        }

        public string ActionName => LocalizationBridge.GetUIString("btn_drop");

        public bool CanExecute(InventoryItem item, IInventory inventory)
            => item != null && !item.Data.IsQuestItem && !item.Data.IsUntradeable;

        public void Execute(InventoryItem item, IInventory inventory)
        {
            if (!CanExecute(item, inventory)) return;

            var prefab = item.Data.WorldPrefab;
            if (prefab != null && _dropOrigin != null)
            {
                var go = Object.Instantiate(prefab, _dropOrigin.position, _dropOrigin.rotation);
                var wi = go.GetComponent<WorldItem>()
                          ?? go.AddComponent<WorldItem>();
                wi.Initialise(item.Data, item.StackCount);
            }

            inventory.RemoveItemInstance(item);

            InventoryEventBus.Publish(new ItemDroppedEvent
            {
                Item          = item,
                WorldPosition = _dropOrigin != null ? _dropOrigin.position : Vector3.zero
            });
            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Dropped: {item.Data.ItemName}");
        }
    }

    // ── Destroy ───────────────────────────────────────────────────────────────

    /// <summary>
    /// "Destroy" action — permanently removes the item from the inventory.
    /// </summary>
    public class DestroyItemAction : IItemAction
    {
        public string ActionName => LocalizationBridge.GetUIString("btn_destroy");

        public bool CanExecute(InventoryItem item, IInventory inventory)
            => item != null && !item.Data.IsQuestItem;

        public void Execute(InventoryItem item, IInventory inventory)
        {
            if (!CanExecute(item, inventory)) return;
            inventory.RemoveItemInstance(item);
            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Destroyed: {item.Data.ItemName}");
        }
    }
}
