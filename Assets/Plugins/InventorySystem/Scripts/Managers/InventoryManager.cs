// =============================================================================
// InventoryManager.cs
// Root manager MonoBehaviour — bootstraps and owns all AIS subsystems.
// =============================================================================
// Place exactly ONE InventoryManager in your scene (or on a DontDestroyOnLoad
// GameObject). It registers itself with ServiceLocator on Awake and tears down
// on OnDestroy.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Central manager that owns player inventories and coordinates subsystems.
    /// Supports multiple players (split-screen) via a player index.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Database")]
        [Tooltip("Drag your ItemDatabase ScriptableObject here.")]
        [SerializeField] private ItemDatabaseSO _itemDatabase;

        [Header("Default Inventory Config")]
        [Tooltip("Default grid size for a new player bag (columns).")]
        [SerializeField] [Range(1, 20)] private int _defaultColumns = 8;

        [Tooltip("Default grid size for a new player bag (rows).")]
        [SerializeField] [Range(1, 20)] private int _defaultRows = 5;

        [Tooltip("Maximum carry weight. 0 = unlimited.")]
        [SerializeField] [Min(0f)] private float _defaultMaxWeight = 0f;

        [Header("Multi-Player")]
        [Tooltip("Number of players to initialise inventories for. 1 = single player.")]
        [SerializeField] [Range(1, 4)] private int _playerCount = 1;

        [Header("Persistence")]
        [Tooltip("Automatically save on application pause (mobile focus-loss).")]
        [SerializeField] private bool _saveOnPause = true;

        #endregion

        #region Private State

        // Key = playerIndex
        private readonly Dictionary<int, Inventory> _playerInventories   = new Dictionary<int, Inventory>();
        private readonly Dictionary<int, Inventory> _playerEquipment     = new Dictionary<int, Inventory>();
        private readonly Dictionary<int, HotbarData> _playerHotbars      = new Dictionary<int, HotbarData>();

        // Shared world containers (chests, vendor stock, etc.)
        private readonly Dictionary<string, Inventory> _worldInventories  = new Dictionary<string, Inventory>();

        #endregion

        #region Singleton / Service Locator

        public static InventoryManager Instance { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Enforce single instance
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} Duplicate InventoryManager destroyed.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Register with Service Locator
            ServiceLocator.Register<InventoryManager>(this);

            if (_itemDatabase != null)
            {
                _itemDatabase.Initialise();
                ServiceLocator.Register<ItemDatabaseSO>(_itemDatabase);
            }
            else
            {
                Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} InventoryManager: no ItemDatabase assigned!", this);
            }

            InitialisePlayerInventories();

            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} InventoryManager ready. Players: {_playerCount}");
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<InventoryManager>();
            Instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && _saveOnPause)
                SaveManager.SaveAll(0); // auto-save to slot 0
        }

        #endregion

        #region Initialisation

        private void InitialisePlayerInventories()
        {
            for (int i = 0; i < _playerCount; i++)
            {
                var bag = new Inventory(
                    $"PlayerBag_{i}",
                    $"Player {i + 1} Bag",
                    _defaultColumns, _defaultRows,
                    InventoryType.PlayerBag,
                    _defaultMaxWeight);

                var equipment = new Inventory(
                    $"PlayerEquip_{i}",
                    $"Player {i + 1} Equipment",
                    3, 4,
                    InventoryType.PlayerEquipment);

                _playerInventories[i] = bag;
                _playerEquipment[i]   = equipment;
                _playerHotbars[i]     = new HotbarData(i);

                // Hook events → event bus
                bag.OnItemAdded   += (item, amt) => InventoryEventBus.Publish(new ItemAddedEvent   { Inventory = bag, Item = item, Amount = amt });
                bag.OnItemRemoved += (id,  amt)  => InventoryEventBus.Publish(new ItemRemovedEvent  { Inventory = bag, ItemID = id, Amount = amt });
            }
        }

        #endregion

        #region Player Inventory Access

        /// <summary>Returns the main bag for <paramref name="playerIndex"/>.</summary>
        public Inventory GetPlayerInventory(int playerIndex = 0)
        {
            if (_playerInventories.TryGetValue(playerIndex, out var inv)) return inv;
            Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} No inventory for player {playerIndex}.");
            return null;
        }

        /// <summary>Returns the equipment container for <paramref name="playerIndex"/>.</summary>
        public Inventory GetEquipmentInventory(int playerIndex = 0)
        {
            if (_playerEquipment.TryGetValue(playerIndex, out var inv)) return inv;
            return null;
        }

        /// <summary>Returns the hotbar data for <paramref name="playerIndex"/>.</summary>
        public HotbarData GetHotbar(int playerIndex = 0)
        {
            _playerHotbars.TryGetValue(playerIndex, out var hb);
            return hb;
        }

        #endregion

        #region World Inventory Access

        /// <summary>
        /// Registers a world inventory (chest, vendor, etc.) so it can be found by ID.
        /// Called automatically by <see cref="InventoryContainer"/> on Awake.
        /// </summary>
        public void RegisterWorldInventory(Inventory inventory)
        {
            if (inventory == null) return;
            _worldInventories[inventory.InventoryID] = inventory;
        }

        public void UnregisterWorldInventory(string inventoryID)
        {
            _worldInventories.Remove(inventoryID);
        }

        /// <summary>Returns a world inventory by its ID, or null.</summary>
        public Inventory GetWorldInventory(string inventoryID)
        {
            _worldInventories.TryGetValue(inventoryID, out var inv);
            return inv;
        }

        #endregion

        #region Convenience Helpers

        /// <summary>
        /// Gives <paramref name="amount"/> units of <paramref name="itemID"/> to the player.
        /// Returns leftover units that didn't fit (0 = success).
        /// </summary>
        public int GiveItem(string itemID, int amount = 1, int playerIndex = 0)
        {
            var db  = ServiceLocator.Get<ItemDatabaseSO>();
            var bag = GetPlayerInventory(playerIndex);
            if (db == null || bag == null) return amount;

            var itemSO = db.GetItem(itemID);
            if (itemSO == null)
            {
                Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} GiveItem: item '{itemID}' not found in database.");
                return amount;
            }

            return bag.AddItem(itemSO, amount);
        }

        /// <summary>
        /// Removes <paramref name="amount"/> units of <paramref name="itemID"/> from the player.
        /// Returns true if fully removed.
        /// </summary>
        public bool TakeItem(string itemID, int amount = 1, int playerIndex = 0)
        {
            var bag = GetPlayerInventory(playerIndex);
            return bag?.RemoveItem(itemID, amount) ?? false;
        }

        /// <summary>Returns total count across bag + equipment for a given item.</summary>
        public int GetTotalItemCount(string itemID, int playerIndex = 0)
        {
            int count = 0;
            count += GetPlayerInventory(playerIndex)?.GetItemCount(itemID) ?? 0;
            count += GetEquipmentInventory(playerIndex)?.GetItemCount(itemID) ?? 0;
            return count;
        }

        #endregion
    }
}
