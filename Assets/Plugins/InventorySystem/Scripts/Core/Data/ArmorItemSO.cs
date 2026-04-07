// =============================================================================
// ArmorItemSO.cs
// Armor / wearable item — helmets, chests, rings, necklaces, etc.
// =============================================================================

using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(
        fileName = "New Armor",
        menuName  = "Inventory System/Items/Armor",
        order     = 13)]
    public class ArmorItemSO : ItemSO
    {
        #region Armor Stats

        [Header("Armor Stats")]
        [SerializeField] private ArmorSlot _armorSlot     = ArmorSlot.Chest;

        [Tooltip("Flat physical defense value.")]
        [Min(0f)] [SerializeField] private float _baseDefense    = 5f;

        [Tooltip("Elemental resistances (Fire, Ice, Lightning, Poison …) 0–100%.")]
        [SerializeField] private float _fireResistance      = 0f;
        [SerializeField] private float _iceResistance       = 0f;
        [SerializeField] private float _lightningResistance = 0f;
        [SerializeField] private float _poisonResistance    = 0f;

        [Tooltip("Durability points (0 = indestructible).")]
        [Min(0)] [SerializeField] private int _maxDurability = 100;

        [Tooltip("Current upgrade level.")]
        [Min(0)] [SerializeField] private int _upgradeLevel  = 0;

        [Tooltip("Maximum upgrade level.")]
        [Min(0)] [SerializeField] private int _maxUpgradeLevel = 10;

        [Header("Requirements")]
        [Min(1)] [SerializeField] private int _requiredLevel = 1;

        #endregion

        #region Public Properties

        public ArmorSlot ArmorSlot          => _armorSlot;
        public float     BaseDefense        => _baseDefense;
        public float     FireResistance     => _fireResistance;
        public float     IceResistance      => _iceResistance;
        public float     LightningResistance => _lightningResistance;
        public float     PoisonResistance   => _poisonResistance;
        public int       MaxDurability      => _maxDurability;
        public int       UpgradeLevel       => _upgradeLevel;
        public int       MaxUpgradeLevel    => _maxUpgradeLevel;
        public int       RequiredLevel      => _requiredLevel;

        /// <summary>Effective defense factoring in upgrade level.</summary>
        public float EffectiveDefense => _baseDefense * (1f + _upgradeLevel * 0.1f);

        #endregion
    }
}
