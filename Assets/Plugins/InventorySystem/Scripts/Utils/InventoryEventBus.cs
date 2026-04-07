// =============================================================================
// InventoryEventBus.cs
// Typed publish-subscribe event bus for the Advanced Inventory System.
// =============================================================================
// Usage:
//   // Subscribe
//   InventoryEventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
//
//   // Publish
//   InventoryEventBus.Publish(new ItemAddedEvent { Item = item, Amount = 1 });
//
//   // Unsubscribe (always do this in OnDestroy)
//   InventoryEventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    // =========================================================================
    // Event payload structs — add new event types here as needed.
    // =========================================================================

    public struct ItemAddedEvent
    {
        public Inventory      Inventory;
        public InventoryItem  Item;
        public int            Amount;
    }

    public struct ItemRemovedEvent
    {
        public Inventory Inventory;
        public string    ItemID;
        public int       Amount;
    }

    public struct ItemMovedEvent
    {
        public Inventory     Inventory;
        public InventoryItem Item;
        public Vector2Int    FromCell;
        public Vector2Int    ToCell;
    }

    public struct ItemUsedEvent
    {
        public InventoryItem Item;
        public int           PlayerIndex;
    }

    public struct ItemDroppedEvent
    {
        public InventoryItem Item;
        public Vector3       WorldPosition;
    }

    public struct InventoryOpenedEvent
    {
        public Inventory Inventory;
        public int       PlayerIndex;
    }

    public struct InventoryClosedEvent
    {
        public Inventory Inventory;
        public int       PlayerIndex;
    }

    public struct CurrencyChangedEvent
    {
        public string CurrencyID;
        public long   OldBalance;
        public long   NewBalance;
        public int    PlayerIndex;
    }

    public struct CraftStartedEvent
    {
        public CraftingRecipeSO Recipe;
        public float            Duration;
    }

    public struct CraftCompletedEvent
    {
        public CraftingRecipeSO Recipe;
        public InventoryItem    ResultItem;
    }

    public struct UpgradeCompletedEvent
    {
        public InventoryItem Item;
        public int           NewLevel;
    }

    public struct ShopTransactionEvent
    {
        public string        ShopID;
        public InventoryItem Item;
        public float         Price;
        public bool          IsBuy; // true = player bought, false = player sold
    }

    public struct SaveCompletedEvent
    {
        public int  SlotIndex;
        public bool Success;
    }

    public struct LoadCompletedEvent
    {
        public int  SlotIndex;
        public bool Success;
    }

    // =========================================================================
    // Bus implementation
    // =========================================================================

    /// <summary>
    /// Static typed event bus. Subscribers are stored as typed delegates,
    /// avoiding boxing for value-type event payloads.
    /// </summary>
    public static class InventoryEventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers
            = new Dictionary<Type, List<Delegate>>();

        // ── Subscribe ─────────────────────────────────────────────────────────

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            list.Add(handler);
        }

        // ── Unsubscribe ───────────────────────────────────────────────────────

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        // ── Publish ───────────────────────────────────────────────────────────

        public static void Publish<T>(T payload)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;

            // Copy to avoid mutation during iteration
            var snapshot = new Delegate[list.Count];
            list.CopyTo(snapshot);

            foreach (var d in snapshot)
            {
                try
                {
                    ((Action<T>)d)?.Invoke(payload);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} EventBus handler error for {typeof(T).Name}: {ex}");
                }
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>Remove all handlers. Call on scene unload or application quit.</summary>
        public static void Clear()
        {
            _handlers.Clear();
        }
    }
}
