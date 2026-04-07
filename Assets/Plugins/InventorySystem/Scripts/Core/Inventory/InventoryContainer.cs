// =============================================================================
// InventoryContainer.cs
// MonoBehaviour wrapper around a runtime Inventory — for chests, crates, etc.
// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem
{
    /// <summary>
    /// Attaches a named Inventory to any world object (chest, barrel, loot bag).
    /// Registers itself with InventoryManager on Awake so UI can open it by ID.
    /// </summary>
    public class InventoryContainer : MonoBehaviour
    {
        #region Inspector

        [Header("Container Config")]
        [Tooltip("Unique ID for this container (must be stable — used in save/load).")]
        [SerializeField] private string _containerID   = "";
        [SerializeField] private string _displayName   = "Chest";

        [SerializeField] [Range(1, 20)] private int _columns = 5;
        [SerializeField] [Range(1, 20)] private int _rows    = 4;

        [SerializeField] private InventoryType _type = InventoryType.Chest;

        [Tooltip("Max weight capacity. 0 = unlimited.")]
        [SerializeField] [Min(0f)] private float _maxWeight = 0f;

        [Header("Pre-filled Items")]
        [Tooltip("Items placed in this container on first open (loot table).")]
        [SerializeField] private LootEntry[] _startingItems = {};

        [Header("Events")]
        public UnityEvent OnContainerOpened;
        public UnityEvent OnContainerClosed;

        #endregion

        #region Runtime

        public Inventory Inventory { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (string.IsNullOrEmpty(_containerID))
                _containerID = System.Guid.NewGuid().ToString("N");

            Inventory = new Inventory(_containerID, _displayName, _columns, _rows, _type, _maxWeight);

            var manager = ServiceLocator.TryGet<InventoryManager>();
            manager?.RegisterWorldInventory(Inventory);

            PopulateStartingItems();
        }

        private void OnDestroy()
        {
            var manager = ServiceLocator.TryGet<InventoryManager>();
            manager?.UnregisterWorldInventory(_containerID);
        }

        #endregion

        #region Loot Population

        private bool _populated = false;

        private void PopulateStartingItems()
        {
            if (_populated || _startingItems == null) return;

            var db = ServiceLocator.TryGet<ItemDatabaseSO>();
            foreach (var entry in _startingItems)
            {
                if (entry.Item == null) continue;
                // Random quantity within range
                int qty = Random.Range(entry.MinAmount, entry.MaxAmount + 1);
                if (qty > 0)
                    Inventory.AddItem(entry.Item, qty);
            }
            _populated = true;
        }

        #endregion

        #region Interaction

        public void Open()
        {
            OnContainerOpened?.Invoke();
            InventoryEventBus.Publish(new InventoryOpenedEvent { Inventory = Inventory });
        }

        public void Close()
        {
            OnContainerClosed?.Invoke();
            InventoryEventBus.Publish(new InventoryClosedEvent { Inventory = Inventory });
        }

        #endregion
    }

    // ── Loot table entry ──────────────────────────────────────────────────────

    [System.Serializable]
    public class LootEntry
    {
        public ItemSO Item;
        [Min(1)] public int MinAmount = 1;
        [Min(1)] public int MaxAmount = 1;
    }
}
