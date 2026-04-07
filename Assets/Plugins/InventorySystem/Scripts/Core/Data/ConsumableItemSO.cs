// =============================================================================
// ConsumableItemSO.cs
// Consumable item — potions, food, scrolls, etc.
// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem
{
    [CreateAssetMenu(
        fileName = "New Consumable",
        menuName  = "Inventory System/Items/Consumable",
        order     = 11)]
    public class ConsumableItemSO : ItemSO
    {
        #region Consumable Data

        [Header("Consumable Properties")]
        [Tooltip("Primary effect applied when this item is used.")]
        [SerializeField] private ConsumableEffect _primaryEffect = ConsumableEffect.RestoreHealth;

        [Tooltip("Magnitude of the primary effect (e.g. 50 HP restored).")]
        [SerializeField] private float _effectAmount = 50f;

        [Tooltip("Duration of the effect in seconds (0 = instant).")]
        [Min(0f)]
        [SerializeField] private float _effectDuration = 0f;

        [Tooltip("Cooldown in seconds before this item can be used again (per player).")]
        [Min(0f)]
        [SerializeField] private float _cooldown = 1f;

        [Tooltip("Animation/VFX prefab spawned on the player when consumed.")]
        [SerializeField] private GameObject _useFXPrefab;

        [Tooltip("SFX clip played on use.")]
        [SerializeField] private AudioClip _useSFX;

        #endregion

        #region Public Properties

        public ConsumableEffect PrimaryEffect  => _primaryEffect;
        public float            EffectAmount   => _effectAmount;
        public float            EffectDuration => _effectDuration;
        public float            Cooldown       => _cooldown;
        public GameObject       UseFXPrefab    => _useFXPrefab;
        public AudioClip        UseSFX         => _useSFX;

        #endregion
    }
}
