// =============================================================================
// InventorySystemConstants.cs
// Advanced Inventory System (AIS) — Constants & Configuration
// =============================================================================
// Single source of truth for all magic values, keys, and event names.
// =============================================================================

namespace InventorySystem
{
    /// <summary>
    /// Central constants repository for the Advanced Inventory System.
    /// Modify values here to affect the entire system without hunting through scripts.
    /// </summary>
    public static class InventorySystemConstants
    {
        #region Version

        public const string SYSTEM_VERSION = "1.0.0";
        public const string SYSTEM_NAME    = "Advanced Inventory System";

        #endregion

        #region Grid & Shape

        /// <summary>Default pixel size of each inventory grid cell (width = height).</summary>
        public const float CELL_SIZE = 64f;

        /// <summary>Pixel gap between cells.</summary>
        public const float CELL_PADDING = 2f;

        /// <summary>Maximum grid columns supported.</summary>
        public const int MAX_GRID_COLUMNS = 20;

        /// <summary>Maximum grid rows supported.</summary>
        public const int MAX_GRID_ROWS = 20;

        /// <summary>Maximum cells a single item shape can occupy.</summary>
        public const int MAX_SHAPE_CELLS = 16;

        /// <summary>Number of rotation states (0, 90, 180, 270).</summary>
        public const int ROTATION_STATES = 4;

        #endregion

        #region Item Stack

        public const int DEFAULT_MAX_STACK = 99;
        public const int SINGLE_STACK      = 1;

        #endregion

        #region Hotbar

        public const int MAX_HOTBAR_SLOTS = 9;
        public const int DEFAULT_HOTBAR_SLOTS = 9;

        #endregion

        #region Crafting

        public const int MAX_CRAFT_QUEUE = 10;
        public const float MIN_CRAFT_TIME = 0f;

        #endregion

        #region Save System

        public const int MAX_SAVE_SLOTS = 5;
        public const string SAVE_FILE_EXTENSION = ".ais";
        public const string SAVE_FOLDER_NAME    = "InventorySaves";
        public const string SAVE_KEY_PREFIX     = "AIS_";

        // PlayerPrefs keys
        public const string PREFS_LAST_SLOT = "AIS_LastSaveSlot";

        #endregion

        #region UI Layer / Tag Names

        public const string INVENTORY_CANVAS_TAG    = "InventoryCanvas";
        public const string TOOLTIP_TAG             = "InventoryTooltip";
        public const string DRAG_LAYER_NAME         = "InventoryDrag";

        #endregion

        #region Input Action Map Names

        public const string ACTION_MAP_INVENTORY   = "Inventory";
        public const string ACTION_OPEN_INVENTORY  = "OpenInventory";
        public const string ACTION_CLOSE_INVENTORY = "CloseInventory";
        public const string ACTION_HOTBAR_1        = "Hotbar1";
        public const string ACTION_HOTBAR_2        = "Hotbar2";
        public const string ACTION_HOTBAR_3        = "Hotbar3";
        public const string ACTION_HOTBAR_4        = "Hotbar4";
        public const string ACTION_HOTBAR_5        = "Hotbar5";
        public const string ACTION_HOTBAR_6        = "Hotbar6";
        public const string ACTION_HOTBAR_7        = "Hotbar7";
        public const string ACTION_HOTBAR_8        = "Hotbar8";
        public const string ACTION_HOTBAR_9        = "Hotbar9";
        public const string ACTION_ROTATE_ITEM     = "RotateItem";
        public const string ACTION_SPLIT_STACK     = "SplitStack";
        public const string ACTION_NAVIGATE        = "Navigate";
        public const string ACTION_CONFIRM         = "Confirm";
        public const string ACTION_CANCEL          = "Cancel";

        #endregion

        #region Event Names (for event bus / messaging)

        public const string EVT_ITEM_ADDED         = "OnItemAdded";
        public const string EVT_ITEM_REMOVED       = "OnItemRemoved";
        public const string EVT_ITEM_MOVED         = "OnItemMoved";
        public const string EVT_ITEM_USED          = "OnItemUsed";
        public const string EVT_ITEM_EQUIPPED      = "OnItemEquipped";
        public const string EVT_ITEM_DROPPED       = "OnItemDropped";
        public const string EVT_STACK_CHANGED      = "OnStackChanged";
        public const string EVT_INVENTORY_OPENED   = "OnInventoryOpened";
        public const string EVT_INVENTORY_CLOSED   = "OnInventoryClosed";
        public const string EVT_CURRENCY_CHANGED   = "OnCurrencyChanged";
        public const string EVT_CRAFT_STARTED      = "OnCraftStarted";
        public const string EVT_CRAFT_COMPLETED    = "OnCraftCompleted";
        public const string EVT_CRAFT_FAILED       = "OnCraftFailed";
        public const string EVT_UPGRADE_COMPLETED  = "OnUpgradeCompleted";
        public const string EVT_SHOP_BOUGHT        = "OnShopItemBought";
        public const string EVT_SHOP_SOLD          = "OnShopItemSold";
        public const string EVT_SAVE_COMPLETED     = "OnSaveCompleted";
        public const string EVT_LOAD_COMPLETED     = "OnLoadCompleted";

        #endregion

        #region Rarity Colors (HTML hex)

        public const string COLOR_COMMON    = "#FFFFFF"; // White
        public const string COLOR_UNCOMMON  = "#1EFF00"; // Green
        public const string COLOR_RARE      = "#0070DD"; // Blue
        public const string COLOR_EPIC      = "#A335EE"; // Purple
        public const string COLOR_LEGENDARY = "#FF8000"; // Orange
        public const string COLOR_ARTIFACT  = "#E6CC80"; // Gold

        #endregion

        #region Pricing

        /// <summary>
        /// Default sell price ratio relative to buy price (e.g. 0.5 = 50% of buy price).
        /// </summary>
        public const float DEFAULT_SELL_RATIO = 0.5f;

        #endregion

        #region Debug

        public const string LOG_PREFIX     = "[AIS]";
        public const string LOG_WARN_PREFIX = "[AIS][WARN]";
        public const string LOG_ERR_PREFIX  = "[AIS][ERROR]";

        #endregion
    }
}
