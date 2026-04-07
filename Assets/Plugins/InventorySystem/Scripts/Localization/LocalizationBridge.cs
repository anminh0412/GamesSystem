// =============================================================================
// LocalizationBridge.cs
// Thin abstraction over Unity's Localization Package.
// =============================================================================
// If the Unity Localization package is NOT installed, this class falls back to
// returning the raw English strings stored on the ItemSO itself.
// To enable full localization: install the Localization package and add
// UNITY_LOCALIZATION to your scripting define symbols.
// =============================================================================

using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Provides localised strings for the inventory UI without hard-coupling to
    /// the Unity Localization package. Add table entries that match ItemSO.ItemID
    /// prefixed with "item_name_" and "item_desc_" (e.g. "item_name_abc123").
    /// </summary>
    public static class LocalizationBridge
    {
        // Table names in the Localization package
        private const string ITEM_NAME_TABLE = "InventoryItemNames";
        private const string ITEM_DESC_TABLE = "InventoryItemDescs";
        private const string UI_TABLE        = "InventoryUI";

#if UNITY_LOCALIZATION
        private static UnityEngine.Localization.Settings.LocalizationSettings Settings
            => UnityEngine.Localization.Settings.LocalizationSettings.Instance;
#endif

        // ── Item Strings ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns the localised name of an item. Falls back to ItemSO.ItemName
        /// if the localization package is absent or the entry is missing.
        /// </summary>
        public static string GetItemName(ItemSO item)
        {
            if (item == null) return string.Empty;

#if UNITY_LOCALIZATION
            try
            {
                string key = $"item_name_{item.ItemID}";
                var table = Settings.GetStringDatabase().GetTable(ITEM_NAME_TABLE);
                var entry = table?.GetEntry(key);
                if (entry != null) return entry.GetLocalizedString();
            }
            catch { /* fall through */ }
#endif
            return item.ItemName;
        }

        /// <summary>
        /// Returns the localised description of an item.
        /// </summary>
        public static string GetItemDescription(ItemSO item)
        {
            if (item == null) return string.Empty;

#if UNITY_LOCALIZATION
            try
            {
                string key = $"item_desc_{item.ItemID}";
                var table = Settings.GetStringDatabase().GetTable(ITEM_DESC_TABLE);
                var entry = table?.GetEntry(key);
                if (entry != null) return entry.GetLocalizedString();
            }
            catch { /* fall through */ }
#endif
            return item.Description;
        }

        // ── UI Strings ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a localised UI string by key (e.g. "btn_use", "btn_equip").
        /// Falls back to a formatted version of the key itself.
        /// </summary>
        public static string GetUIString(string key)
        {
#if UNITY_LOCALIZATION
            try
            {
                var table = Settings.GetStringDatabase().GetTable(UI_TABLE);
                var entry = table?.GetEntry(key);
                if (entry != null) return entry.GetLocalizedString();
            }
            catch { /* fall through */ }
#endif
            // Fallback: convert "btn_use" → "Use"
            if (string.IsNullOrEmpty(key)) return key;
            string clean = key.Replace("btn_", "").Replace("lbl_", "").Replace("_", " ");
            return char.ToUpper(clean[0]) + clean[1..];
        }

        // ── Rarity Labels ─────────────────────────────────────────────────────

        public static string GetRarityName(ItemRarity rarity)
        {
            string key = $"rarity_{rarity.ToString().ToLower()}";
            return GetUIString(key);
        }
    }
}
