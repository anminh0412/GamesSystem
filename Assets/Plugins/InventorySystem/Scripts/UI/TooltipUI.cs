// =============================================================================
// TooltipUI.cs
// Floating tooltip shown when hovering over an item.
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InventorySystem
{
    /// <summary>
    /// Singleton-style tooltip panel. Follows the cursor and displays item data.
    /// Place one instance in the scene under the top-most Canvas (overlay layer).
    /// </summary>
    public class TooltipUI : MonoBehaviour
    {
        #region Singleton

        public static TooltipUI Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Layout")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup   _canvasGroup;

        [Header("Text Fields")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _rarityText;
        [SerializeField] private TMP_Text _categoryText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private TMP_Text _weightText;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private TMP_Text _stackText;
        [SerializeField] private TMP_Text _upgradeText;
        [SerializeField] private TMP_Text _statsText;     // weapon/armor stats line

        [Header("Rarity Border")]
        [SerializeField] private Image _rarityBorderImage;

        [Header("Behaviour")]
        [Tooltip("Offset from the cursor position (in screen pixels).")]
        [SerializeField] private Vector2 _cursorOffset = new Vector2(16f, -16f);

        [Tooltip("Fade in/out speed (higher = snappier).")]
        [SerializeField] private float _fadeSpeed = 12f;

        [Tooltip("Seconds to wait before showing tooltip.")]
        [SerializeField] private float _showDelay = 0.4f;

        #endregion

        #region State

        private float _showTimer;
        private bool  _wantsToShow;
        private ItemSO        _currentItem;
        private InventoryItem _currentInstance;
        private Canvas        _canvas;
        private RectTransform _canvasRect;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Instance = this;
            _canvas     = GetComponentInParent<Canvas>();
            _canvasRect = _canvas?.GetComponent<RectTransform>();

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            Hide(instant: true);
        }

        private void Update()
        {
            if (_wantsToShow)
            {
                _showTimer -= Time.unscaledDeltaTime;
                if (_showTimer <= 0f)
                {
                    _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, 1f,
                                                           Time.unscaledDeltaTime * _fadeSpeed);
                }
            }
            else
            {
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, 0f,
                                                       Time.unscaledDeltaTime * _fadeSpeed * 2f);
            }

            if (_wantsToShow && _canvasGroup.alpha > 0f)
                FollowCursor();
        }

        #endregion

        #region Public API

        /// <summary>Show the tooltip for a given item instance.</summary>
        public void Show(InventoryItem instance)
        {
            if (instance == null) return;
            _currentInstance = instance;
            _currentItem     = instance.Data;
            _wantsToShow     = true;
            _showTimer       = _showDelay;
            PopulateFromInstance(instance);
        }

        /// <summary>Show tooltip for a bare ItemSO (e.g. shop listing).</summary>
        public void Show(ItemSO item)
        {
            if (item == null) return;
            _currentItem     = item;
            _currentInstance = null;
            _wantsToShow     = true;
            _showTimer       = _showDelay;
            PopulateFromSO(item);
        }

        /// <summary>Hide the tooltip.</summary>
        public void Hide(bool instant = false)
        {
            _wantsToShow = false;
            if (instant) _canvasGroup.alpha = 0f;
        }

        #endregion

        #region Population

        private void PopulateFromInstance(InventoryItem instance)
        {
            var data = instance.Data;
            PopulateFromSO(data);

            // Add runtime-specific info
            if (_stackText)
                _stackText.text = data.IsStackable ? $"Stack: {instance.StackCount}/{data.MaxStackSize}" : "";

            if (_upgradeText)
                _upgradeText.text = instance.UpgradeLevel > 0 ? $"+{instance.UpgradeLevel} Upgrade" : "";

            // Durability
            if (instance.MaxDurability > 0 && _statsText)
                AppendLine(ref _statsText, $"Durability: {instance.CurrentDurability}/{instance.MaxDurability}");
        }

        private void PopulateFromSO(ItemSO item)
        {
            // Name & rarity
            if (_nameText)
            {
                _nameText.text  = LocalizationBridge.GetItemName(item);
                _nameText.color = item.GetRarityColor();
            }

            if (_rarityText)
            {
                _rarityText.text  = LocalizationBridge.GetRarityName(item.Rarity);
                _rarityText.color = item.GetRarityColor();
            }

            if (_rarityBorderImage)
                _rarityBorderImage.color = item.GetRarityColor();

            // Category
            if (_categoryText) _categoryText.text = item.Category.ToString();

            // Description
            if (_descriptionText) _descriptionText.text = LocalizationBridge.GetItemDescription(item);

            // Weight
            if (_weightText) _weightText.text = item.Weight > 0f ? $"{item.Weight:F1} kg" : "";

            // Value
            if (_valueText) _valueText.text = item.BaseValue > 0f ? $"{item.BaseValue:F0} G" : "";

            // Stack
            if (_stackText) _stackText.text = "";
            if (_upgradeText) _upgradeText.text = "";

            // Type-specific stats
            if (_statsText) PopulateStats(item);
        }

        private void PopulateStats(ItemSO item)
        {
            _statsText.text = "";

            if (item is WeaponItemSO w)
            {
                _statsText.text =
                    $"DMG: {w.EffectiveDamage:F0}\n" +
                    $"SPD: {w.AttackSpeed:F1}\n" +
                    $"CRIT: {w.CritChance * 100:F0}% × {w.CritMultiplier:F1}";
            }
            else if (item is ArmorItemSO a)
            {
                _statsText.text = $"DEF: {a.EffectiveDefense:F0}";
                if (a.FireResistance > 0f)      _statsText.text += $"  Fire: {a.FireResistance:F0}%";
                if (a.IceResistance > 0f)       _statsText.text += $"  Ice: {a.IceResistance:F0}%";
                if (a.LightningResistance > 0f) _statsText.text += $"  Ltng: {a.LightningResistance:F0}%";
            }
            else if (item is ConsumableItemSO c)
            {
                _statsText.text = c.EffectDuration > 0f
                    ? $"{c.PrimaryEffect}: {c.EffectAmount:F0} for {c.EffectDuration:F0}s"
                    : $"{c.PrimaryEffect}: {c.EffectAmount:F0}";
            }
        }

        private void AppendLine(ref TMP_Text text, string line)
        {
            if (string.IsNullOrEmpty(text.text))
                text.text = line;
            else
                text.text += "\n" + line;
        }

        #endregion

        #region Cursor Following

        private void FollowCursor()
        {
            if (_canvas == null || _panel == null) return;

            Vector2 screenPos = Input.mousePosition;

            // Convert to canvas local space
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, _canvas.worldCamera, out Vector2 localPoint);

            Vector2 pivot = _panel.pivot;
            Vector2 size  = _panel.rect.size;

            // Flip horizontally if tooltip would overflow the right edge
            float canvasW = _canvasRect.rect.width;
            float xPos    = localPoint.x + _cursorOffset.x;
            if (xPos + size.x * (1f - pivot.x) > canvasW * 0.5f)
                xPos = localPoint.x - _cursorOffset.x - size.x;

            // Flip vertically if tooltip would overflow the bottom
            float canvasH = _canvasRect.rect.height;
            float yPos    = localPoint.y + _cursorOffset.y;
            if (yPos - size.y * pivot.y < -canvasH * 0.5f)
                yPos = localPoint.y + _cursorOffset.y + size.y;

            _panel.anchoredPosition = new Vector2(xPos, yPos);
        }

        #endregion
    }
}
