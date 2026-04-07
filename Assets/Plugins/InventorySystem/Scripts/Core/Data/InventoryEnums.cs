// =============================================================================
// InventoryEnums.cs
// All shared enumerations for the Advanced Inventory System.
// =============================================================================

namespace InventorySystem
{
    // ── Item Classification ───────────────────────────────────────────────────

    /// <summary>High-level item category used for filtering and UI grouping.</summary>
    public enum ItemCategory
    {
        None        = 0,
        Weapon      = 1,
        Armor       = 2,
        Consumable  = 3,
        Resource    = 4,
        QuestItem   = 5,
        Currency    = 6,
        Ammunition  = 7,
        Tool        = 8,
        Key         = 9,
        Blueprint   = 10,
        Gem         = 11,
        Misc        = 99
    }

    /// <summary>Quality / rarity tier. Affects loot colour, pricing, and UI borders.</summary>
    public enum ItemRarity
    {
        Common    = 0,
        Uncommon  = 1,
        Rare      = 2,
        Epic      = 3,
        Legendary = 4,
        Artifact  = 5
    }

    // ── Weapon Subtypes ───────────────────────────────────────────────────────

    public enum WeaponType
    {
        None,
        Sword,
        Axe,
        Mace,
        Dagger,
        Spear,
        Bow,
        Crossbow,
        Staff,
        Wand,
        Shield,
        Fists
    }

    public enum DamageType
    {
        Physical,
        Fire,
        Ice,
        Lightning,
        Poison,
        Arcane,
        Holy,
        Shadow
    }

    // ── Armor Subtypes ────────────────────────────────────────────────────────

    public enum ArmorSlot
    {
        None,
        Head,
        Chest,
        Legs,
        Feet,
        Hands,
        Shoulder,
        Belt,
        Ring,
        Necklace,
        Back
    }

    // ── Consumable Subtypes ───────────────────────────────────────────────────

    public enum ConsumableEffect
    {
        None,
        RestoreHealth,
        RestoreMana,
        RestoreStamina,
        BoostStrength,
        BoostDefense,
        BoostSpeed,
        Antidote,
        RevealMap,
        Teleport
    }

    // ── Inventory ─────────────────────────────────────────────────────────────

    /// <summary>Container type — controls UI layout and transfer rules.</summary>
    public enum InventoryType
    {
        PlayerBag,
        PlayerEquipment,
        Chest,
        Vendor,
        Crafting,
        Hotbar,
        Loot,
        Mail
    }

    /// <summary>Result of an add-item operation.</summary>
    public enum AddItemResult
    {
        Success,
        PartialSuccess,   // Some units added, some left over
        InventoryFull,
        WeightExceeded,
        InvalidItem,
        NotAllowed        // Container does not accept this item type
    }

    // ── Item Shape ────────────────────────────────────────────────────────────

    /// <summary>Rotation state in 90-degree increments.</summary>
    public enum ShapeRotation
    {
        Rot0   = 0,
        Rot90  = 1,
        Rot180 = 2,
        Rot270 = 3
    }

    // ── Crafting ──────────────────────────────────────────────────────────────

    public enum CraftingResult
    {
        Success,
        MissingIngredients,
        IncompatibleStation,
        QueueFull,
        AlreadyCrafting
    }

    public enum CraftJobStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    // ── Upgrade ───────────────────────────────────────────────────────────────

    public enum UpgradeResult
    {
        Success,
        MaxLevelReached,
        MissingMaterials,
        InsufficientCurrency,
        ItemNotUpgradable
    }

    // ── Shop ──────────────────────────────────────────────────────────────────

    public enum ShopTransactionResult
    {
        Success,
        InsufficientFunds,
        InventoryFull,
        ItemNotAvailable,
        CannotSellHere
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    public enum SaveFormat
    {
        Json,
        Binary
    }

    public enum SaveResult
    {
        Success,
        Failed,
        SlotOutOfRange
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public enum InputDeviceType
    {
        KeyboardMouse,
        Gamepad,
        Touch
    }
}
