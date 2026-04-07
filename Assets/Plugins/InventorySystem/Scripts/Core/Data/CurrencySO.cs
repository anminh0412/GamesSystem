// =============================================================================
// CurrencySO.cs
// Defines a currency type (Gold, Gem, Token, etc.).
// =============================================================================

using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(
        fileName = "New Currency",
        menuName  = "Inventory System/Currency",
        order     = 20)]
    public class CurrencySO : ScriptableObject
    {
        #region Identity

        [Header("Identity")]
        [Tooltip("Unique currency identifier (e.g. 'gold', 'gem', 'token'). Never change after release.")]
        [SerializeField] private string _currencyID   = "";

        [Tooltip("Display name shown in UI.")]
        [SerializeField] private string _currencyName = "Gold";

        [Tooltip("Short abbreviation shown in tight UI areas.")]
        [SerializeField] private string _symbol = "G";

        #endregion

        #region Visuals

        [Header("Visuals")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color  _color = Color.yellow;

        #endregion

        #region Limits

        [Header("Limits")]
        [Tooltip("Maximum amount a single player can hold. 0 = unlimited.")]
        [Min(0)]
        [SerializeField] private long _maxAmount = 0;

        [Tooltip("Starting amount given to a new player.")]
        [Min(0)]
        [SerializeField] private long _startingAmount = 0;

        [Tooltip("Is this a premium (real-money) currency?")]
        [SerializeField] private bool _isPremium = false;

        #endregion

        #region Public Properties

        public string CurrencyID    => _currencyID;
        public string CurrencyName  => _currencyName;
        public string Symbol        => _symbol;
        public Sprite Icon          => _icon;
        public Color  Color         => _color;
        public long   MaxAmount     => _maxAmount;
        public long   StartingAmount => _startingAmount;
        public bool   IsPremium     => _isPremium;

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Generate New Currency ID")]
        private void GenerateID()
        {
            _currencyID = System.Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_currencyID))
                Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} Currency '{_currencyName}' has no ID.", this);
        }
#endif

        #endregion
    }
}
