// =============================================================================
// ItemDatabaseSO.cs
// Central registry of all ItemSO assets.
// =============================================================================
// Drop all ItemSO assets into this database. Systems that need to resolve an
// ItemID back to an ItemSO (e.g. save/load) use this as a lookup table.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(
        fileName = "ItemDatabase",
        menuName  = "Inventory System/Item Database",
        order     = 1)]
    public class ItemDatabaseSO : ScriptableObject
    {
        #region Data

        [Header("All Items")]
        [Tooltip("Drag every ItemSO asset here. The database is rebuilt automatically in the Editor.")]
        [SerializeField] private List<ItemSO> _items = new List<ItemSO>();

        // Runtime lookup built on first access
        private Dictionary<string, ItemSO> _lookup;

        #endregion

        #region Initialisation

        /// <summary>
        /// Builds the ID→Item lookup dictionary.
        /// Called automatically on first use and can be called manually after hot-reload.
        /// </summary>
        public void Initialise()
        {
            _lookup = new Dictionary<string, ItemSO>(_items.Count);
            foreach (var item in _items)
            {
                if (item == null) continue;
                if (string.IsNullOrEmpty(item.ItemID))
                {
                    Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} Item '{item.name}' has no ID — skipped.", item);
                    continue;
                }
                if (_lookup.ContainsKey(item.ItemID))
                {
                    Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} Duplicate ID '{item.ItemID}' for item '{item.name}' — skipped.", item);
                    continue;
                }
                _lookup[item.ItemID] = item;
            }

            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} ItemDatabase initialised with {_lookup.Count} items.");
        }

        private void EnsureInitialised()
        {
            if (_lookup == null) Initialise();
        }

        #endregion

        #region Lookup

        /// <summary>
        /// Returns the ItemSO with the given <paramref name="itemID"/>, or null if not found.
        /// </summary>
        public ItemSO GetItem(string itemID)
        {
            EnsureInitialised();
            _lookup.TryGetValue(itemID, out ItemSO item);
            return item;
        }

        /// <summary>Returns the first item that matches the given type T.</summary>
        public T GetItem<T>(string itemID) where T : ItemSO
        {
            return GetItem(itemID) as T;
        }

        /// <summary>Returns true when the database contains an entry for this ID.</summary>
        public bool Contains(string itemID)
        {
            EnsureInitialised();
            return _lookup.ContainsKey(itemID);
        }

        /// <summary>All items of type T registered in the database.</summary>
        public IEnumerable<T> GetAllOfType<T>() where T : ItemSO
        {
            EnsureInitialised();
            foreach (var item in _items)
                if (item is T typed)
                    yield return typed;
        }

        /// <summary>All items in the given category.</summary>
        public IEnumerable<ItemSO> GetByCategory(ItemCategory category)
        {
            EnsureInitialised();
            foreach (var item in _items)
                if (item != null && item.Category == category)
                    yield return item;
        }

        /// <summary>Full list of registered items (read-only).</summary>
        public IReadOnlyList<ItemSO> AllItems => _items;

        /// <summary>Total number of registered items.</summary>
        public int Count => _items.Count;

        #endregion

        #region Editor

#if UNITY_EDITOR
        /// <summary>
        /// Scans the entire project for ItemSO assets and rebuilds the list.
        /// Invoked from the custom Editor button.
        /// </summary>
        [ContextMenu("Auto-Populate From Project")]
        public void AutoPopulate()
        {
            _items.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemSO");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ItemSO item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemSO>(path);
                if (item != null)
                    _items.Add(item);
            }
            _lookup = null; // force rebuild
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Auto-populated {_items.Count} items.");
        }
#endif

        #endregion
    }
}
