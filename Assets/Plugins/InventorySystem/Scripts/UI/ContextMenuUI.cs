// =============================================================================
// ContextMenuUI.cs
// Context menu shown on right-click / long-press for item actions.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace InventorySystem
{
    /// <summary>
    /// Floating context menu that lists all valid <see cref="IItemAction"/>s
    /// for the tapped/right-clicked item.
    /// </summary>
    public class ContextMenuUI : MonoBehaviour
    {
        #region Singleton

        public static ContextMenuUI Instance { get; private set; }

        #endregion

        #region Inspector

        [Header("Layout")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private CanvasGroup   _canvasGroup;
        [SerializeField] private Transform     _buttonContainer;

        [Header("Button Prefab")]
        [Tooltip("Prefab must have a Button + TMP_Text child named 'Label'.")]
        [SerializeField] private GameObject _buttonPrefab;

        [Header("Behaviour")]
        [SerializeField] private float _fadeSpeed = 16f;

        #endregion

        #region State

        private bool  _isVisible;
        private readonly List<GameObject> _activeButtons = new List<GameObject>();
        private Canvas        _canvas;
        private RectTransform _canvasRect;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Instance    = this;
            _canvas     = GetComponentInParent<Canvas>();
            _canvasRect = _canvas?.GetComponent<RectTransform>();

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            HideInstant();
        }

        private void Update()
        {
            float target = _isVisible ? 1f : 0f;
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target,
                                                   Time.unscaledDeltaTime * _fadeSpeed);
            _canvasGroup.blocksRaycasts   = _isVisible;
            _canvasGroup.interactable     = _isVisible;

            // Close on any click outside
            if (_isVisible && Input.GetMouseButtonDown(0))
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(_panel, Input.mousePosition, _canvas?.worldCamera))
                    Hide();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Shows the context menu at the given screen position for the given item.
        /// The actions list is built from <paramref name="actions"/>.
        /// </summary>
        public void Show(InventoryItem item, IInventory inventory,
                         List<IItemAction> actions, Vector2 screenPos)
        {
            if (item == null || actions == null || actions.Count == 0) return;

            ClearButtons();

            foreach (var action in actions)
            {
                if (!action.CanExecute(item, inventory)) continue;

                var go      = Instantiate(_buttonPrefab, _buttonContainer);
                var label   = go.GetComponentInChildren<TMP_Text>();
                var button  = go.GetComponent<Button>();

                if (label)  label.text = action.ActionName;

                // Capture variables for the lambda
                var capturedAction    = action;
                var capturedItem      = item;
                var capturedInventory = inventory;

                button.onClick.AddListener(() =>
                {
                    capturedAction.Execute(capturedItem, capturedInventory);
                    Hide();
                });

                _activeButtons.Add(go);
            }

            if (_activeButtons.Count == 0) return;

            PositionAt(screenPos);
            _isVisible = true;
        }

        /// <summary>Hides the menu with fade-out.</summary>
        public void Hide() => _isVisible = false;

        /// <summary>Hides the menu instantly (no fade).</summary>
        public void HideInstant()
        {
            _isVisible         = false;
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
        }

        #endregion

        #region Private Helpers

        private void ClearButtons()
        {
            foreach (var go in _activeButtons)
                Destroy(go);
            _activeButtons.Clear();
        }

        private void PositionAt(Vector2 screenPos)
        {
            if (_canvas == null || _panel == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRect, screenPos, _canvas.worldCamera, out Vector2 localPt);

            // Clamp so it doesn't go off-screen
            Vector2 halfCanvas = _canvasRect.rect.size * 0.5f;
            Vector2 panelSize  = _panel.rect.size;

            float x = Mathf.Clamp(localPt.x, -halfCanvas.x, halfCanvas.x - panelSize.x);
            float y = Mathf.Clamp(localPt.y, -halfCanvas.y + panelSize.y, halfCanvas.y);

            _panel.anchoredPosition = new Vector2(x, y);
        }

        #endregion
    }
}
