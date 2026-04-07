// =============================================================================
// SplitStackUI.cs
// Popup dialog for splitting a stack into two.
// =============================================================================
// Triggered by InventoryUI when the player Shift-clicks (or long-presses) a
// stackable item with StackCount > 1.
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InventorySystem
{
    /// <summary>
    /// Modal dialog that lets the player choose how many units to split off.
    /// Calls back into InventoryUI.CommitSplit() on confirmation.
    /// </summary>
    public class SplitStackUI : MonoBehaviour
    {
        #region Singleton

        public static SplitStackUI Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Layout")]
        [SerializeField] private GameObject   _panel;
        [SerializeField] private CanvasGroup  _canvasGroup;

        [Header("Controls")]
        [SerializeField] private Slider       _slider;
        [SerializeField] private TMP_Text     _sliderValueText; // "Split: 12"
        [SerializeField] private TMP_Text     _itemNameText;
        [SerializeField] private TMP_Text     _maxCountText;    // "/ 64"
        [SerializeField] private Button       _confirmButton;
        [SerializeField] private Button       _cancelButton;

        [Header("Behaviour")]
        [SerializeField] private float _fadeSpeed = 16f;

        #endregion

        #region State

        private InventoryItem _item;
        private IInventory    _inventory;
        private Action<InventoryItem, int> _onConfirm;
        private bool _isVisible;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Instance = this;

            _confirmButton?.onClick.AddListener(OnConfirm);
            _cancelButton?.onClick.AddListener(OnCancel);
            _slider?.onValueChanged.AddListener(OnSliderChanged);

            HideInstant();
        }

        private void Update()
        {
            float target = _isVisible ? 1f : 0f;
            if (_canvasGroup)
                _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target,
                                                       Time.unscaledDeltaTime * _fadeSpeed);

            if (_canvasGroup)
            {
                _canvasGroup.blocksRaycasts = _isVisible;
                _canvasGroup.interactable   = _isVisible;
            }

            if (_panel)
                _panel.SetActive(_canvasGroup == null || _canvasGroup.alpha > 0.01f);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Opens the split dialog for <paramref name="item"/>.
        /// <paramref name="onConfirm"/> is called with (item, splitAmount) on OK.
        /// </summary>
        public void Show(InventoryItem item, IInventory inventory,
                         Action<InventoryItem, int> onConfirm)
        {
            if (item == null || item.StackCount <= 1) return;

            _item      = item;
            _inventory = inventory;
            _onConfirm = onConfirm;

            // Configure slider: min=1, max=StackCount-1 (keep at least 1 in source)
            if (_slider)
            {
                _slider.minValue = 1f;
                _slider.maxValue = item.StackCount - 1f;
                _slider.value    = Mathf.Floor(item.StackCount * 0.5f);
                _slider.wholeNumbers = true;
            }

            if (_itemNameText) _itemNameText.text = item.Data.ItemName;
            if (_maxCountText) _maxCountText.text = $"/ {item.StackCount}";

            UpdateSliderLabel((int)(_slider?.value ?? 1));

            _isVisible = true;
            if (_panel) _panel.SetActive(true);
        }

        /// <summary>Closes the dialog without splitting.</summary>
        public void Hide()
        {
            _isVisible = false;
        }

        public void HideInstant()
        {
            _isVisible = false;
            if (_canvasGroup) _canvasGroup.alpha = 0f;
            if (_panel) _panel.SetActive(false);
        }

        #endregion

        #region Handlers

        private void OnSliderChanged(float value)
        {
            UpdateSliderLabel((int)value);
        }

        private void UpdateSliderLabel(int amount)
        {
            if (_sliderValueText)
                _sliderValueText.text = $"Split: {amount}";
        }

        private void OnConfirm()
        {
            if (_item == null || _slider == null) { Hide(); return; }

            int splitAmount = (int)_slider.value;
            if (splitAmount > 0 && splitAmount < _item.StackCount)
                _onConfirm?.Invoke(_item, splitAmount);

            Hide();
        }

        private void OnCancel()
        {
            _item      = null;
            _inventory = null;
            _onConfirm = null;
            Hide();
        }

        #endregion
    }
}
