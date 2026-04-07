// =============================================================================
// WeaponItemSO.cs
// Weapon item — swords, bows, staffs, etc.
// =============================================================================

using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(
        fileName = "New Weapon",
        menuName  = "Inventory System/Items/Weapon",
        order     = 12)]
    public class WeaponItemSO : ItemSO
    {
        #region Weapon Stats

        [Header("Weapon Stats")]
        [SerializeField] private WeaponType _weaponType  = WeaponType.Sword;
        [SerializeField] private DamageType _damageType  = DamageType.Physical;

        [Tooltip("Base damage per hit.")]
        [Min(0f)] [SerializeField] private float _baseDamage    = 10f;

        [Tooltip("Attack speed multiplier (1.0 = normal).")]
        [Min(0.1f)] [SerializeField] private float _attackSpeed = 1f;

        [Tooltip("Range in meters.")]
        [Min(0f)] [SerializeField] private float _range         = 1.5f;

        [Tooltip("Critical hit chance 0–1.")]
        [Range(0f, 1f)] [SerializeField] private float _critChance = 0.05f;

        [Tooltip("Critical hit damage multiplier.")]
        [Min(1f)] [SerializeField] private float _critMultiplier = 2f;

        [Tooltip("Durability points (0 = indestructible / no durability system).")]
        [Min(0)] [SerializeField] private int _maxDurability = 100;

        [Tooltip("Current upgrade level (0 = base).")]
        [Min(0)] [SerializeField] private int _upgradeLevel = 0;

        [Tooltip("Maximum upgrade level this weapon supports.")]
        [Min(0)] [SerializeField] private int _maxUpgradeLevel = 10;

        [Header("Requirements")]
        [Tooltip("Minimum character level to equip.")]
        [Min(1)] [SerializeField] private int _requiredLevel = 1;

        #endregion

        #region Public Properties

        public WeaponType WeaponType      => _weaponType;
        public DamageType DamageType      => _damageType;
        public float      BaseDamage      => _baseDamage;
        public float      AttackSpeed     => _attackSpeed;
        public float      Range           => _range;
        public float      CritChance      => _critChance;
        public float      CritMultiplier  => _critMultiplier;
        public int        MaxDurability   => _maxDurability;
        public int        UpgradeLevel    => _upgradeLevel;
        public int        MaxUpgradeLevel => _maxUpgradeLevel;
        public int        RequiredLevel   => _requiredLevel;

        /// <summary>Effective damage factoring in upgrade level.</summary>
        public float EffectiveDamage => _baseDamage * (1f + _upgradeLevel * 0.1f);

        #endregion
    }
}
