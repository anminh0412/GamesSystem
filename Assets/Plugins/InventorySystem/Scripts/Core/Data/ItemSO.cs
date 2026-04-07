// =============================================================================
// ItemSO.cs
// Base ScriptableObject for ALL items in the Advanced Inventory System.
// =============================================================================
// Derive from this class to create Consumable, Weapon, Armor, etc.
// Use [CreateAssetMenu] on each subclass to expose it in the Project window.
// =============================================================================

using UnityEngine;

#if UNITY_LOCALIZATION
using UnityEngine.Localization;
#endif

namespace InventorySystem
{
    /// <summary>
    /// Foundation ScriptableObject that every item in the system inherits from.
    /// Contains all data a typical RPG item requires, structured for Inspector
    /// editing and easily extensible via subclasses.
    /// </summary>
    [CreateAssetMenu(
        fileName = "New Item",
        menuName  = "Inventory System/Items/Base Item",
        order     = 10)]
    public class ItemSO : ScriptableObject, IItem
    {
        #region Identity

        [Header("Identity")]
        [Tooltip("Globally-unique identifier. Click the Generate button or type a GUID manually.")]
        [SerializeField] private string _itemID = "";

        [Tooltip("Display name shown in UI (overridden by localisation at runtime if enabled).")]
        [SerializeField] private string _itemName = "New Item";

        [Tooltip("Flavour / gameplay description (overridden by localisation at runtime if enabled).")]
        [TextArea(2, 5)]
        [SerializeField] private string _description = "";

        #endregion

        #region Localisation (optional)

#if UNITY_LOCALIZATION
        [Header("Localisation (Unity Localisation Package)")]
        [Tooltip("Localised string entry for the item name. Overrides _itemName at runtime.")]
        [SerializeField] private LocalizedString _localizedName;

        [Tooltip("Localised string entry for the description. Overrides _description at runtime.")]
        [SerializeField] private LocalizedString _localizedDescription;
#endif

        #endregion

        #region Visuals

        [Header("Visuals")]
        [Tooltip("2D icon displayed in inventory slots and hotbar.")]
        [SerializeField] private Sprite _icon;

        [Tooltip("Optional 3D prefab spawned in the world (for drops, previews, etc.).")]
        [SerializeField] private GameObject _worldPrefab;

        [Tooltip("Optional 3D prefab shown in equipment preview panels.")]
        [SerializeField] private GameObject _previewPrefab;

        #endregion

        #region Classification

        [Header("Classification")]
        [SerializeField] private ItemCategory _category  = ItemCategory.Misc;
        [SerializeField] private ItemRarity   _rarity    = ItemRarity.Common;

        #endregion

        #region Physical Properties

        [Header("Physical")]
        [Tooltip("Weight per single unit (kg). 0 = weightless.")]
        [Min(0f)]
        [SerializeField] private float _weight = 0f;

        [Tooltip("Base monetary value in the default currency.")]
        [Min(0f)]
        [SerializeField] private float _baseValue = 1f;

        #endregion

        #region Stacking

        [Header("Stacking")]
        [SerializeField] private bool _isStackable  = true;

        [Tooltip("Maximum units per stack slot. Ignored when IsStackable = false.")]
        [Min(1)]
        [SerializeField] private int  _maxStackSize = InventorySystemConstants.DEFAULT_MAX_STACK;

        #endregion

        #region Grid Shape

        [Header("Grid Shape (Tetris-style)")]
        [Tooltip("Cells this item occupies on the inventory grid.")]
        [SerializeField] private ItemShape _shape = new ItemShape();

        #endregion

        #region Flags

        [Header("Flags")]
        [Tooltip("Quest items cannot be dropped or sold.")]
        [SerializeField] private bool _isQuestItem = false;

        [Tooltip("Item is permanently bound to the player who first picks it up.")]
        [SerializeField] private bool _isBoundOnPickup = false;

        [Tooltip("Item cannot be traded or placed in shared stash.")]
        [SerializeField] private bool _isUntradeable = false;

        [Tooltip("Destroyed on use (e.g. a key used to open a lock).")]
        [SerializeField] private bool _consumedOnUse = false;

        #endregion

        #region IItem — Public Properties

        public string      ItemID       => _itemID;
        public ItemCategory Category    => _category;
        public ItemRarity   Rarity      => _rarity;
        public float        Weight      => _weight;
        public bool         IsStackable => _isStackable;
        public int          MaxStackSize => _isStackable ? _maxStackSize : InventorySystemConstants.SINGLE_STACK;
        public ItemShape    Shape       => _shape;
        public Sprite       Icon        => _icon;

        public string ItemName
        {
            get
            {
#if UNITY_LOCALIZATION
                if (_localizedName != null && !_localizedName.IsEmpty)
                    return _localizedName.GetLocalizedString();
#endif
                return _itemName;
            }
        }

        public string Description
        {
            get
            {
#if UNITY_LOCALIZATION
                if (_localizedDescription != null && !_localizedDescription.IsEmpty)
                    return _localizedDescription.GetLocalizedString();
#endif
                return _description;
            }
        }

        #endregion

        #region Additional Public Properties

        public GameObject WorldPrefab    => _worldPrefab;
        public GameObject PreviewPrefab  => _previewPrefab;
        public float      BaseValue      => _baseValue;
        public bool       IsQuestItem    => _isQuestItem;
        public bool       IsBoundOnPickup => _isBoundOnPickup;
        public bool       IsUntradeable  => _isUntradeable;
        public bool       ConsumedOnUse  => _consumedOnUse;

        #endregion

        #region Utility

        /// <summary>
        /// Returns the rarity colour as defined in <see cref="InventorySystemConstants"/>.
        /// </summary>
        public Color GetRarityColor()
        {
            string hex = _rarity switch
            {
                ItemRarity.Common    => InventorySystemConstants.COLOR_COMMON,
                ItemRarity.Uncommon  => InventorySystemConstants.COLOR_UNCOMMON,
                ItemRarity.Rare      => InventorySystemConstants.COLOR_RARE,
                ItemRarity.Epic      => InventorySystemConstants.COLOR_EPIC,
                ItemRarity.Legendary => InventorySystemConstants.COLOR_LEGENDARY,
                ItemRarity.Artifact  => InventorySystemConstants.COLOR_ARTIFACT,
                _                    => InventorySystemConstants.COLOR_COMMON
            };

            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        public override string ToString() => $"[{_rarity}] {ItemName} (ID: {_itemID})";

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        /// <summary>
        /// Called from a custom Inspector button to auto-generate a stable GUID.
        /// </summary>
        [ContextMenu("Generate New Item ID")]
        private void GenerateID()
        {
            _itemID = System.Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Generated ID for '{_itemName}': {_itemID}");
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_itemID))
                Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} Item '{_itemName}' has no ID. " +
                                 "Right-click → 'Generate New Item ID'.", this);

            if (!_isStackable)
                _maxStackSize = InventorySystemConstants.SINGLE_STACK;
        }
#endif

        #endregion
    }
}
