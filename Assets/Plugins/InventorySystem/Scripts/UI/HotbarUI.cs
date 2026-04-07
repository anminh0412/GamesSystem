// =============================================================================
// HotbarUI.cs
// Hotbar HUD strip — shows 1–9 quick-access item slots.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

namespace InventorySystem
{
    /// <summary>
    /// Renders the player's hotbar HUD. Each slot is a button that:
    ///   • Shows the bound item icon + stack count
    ///   • Highlights the active slot
    ///   • Accepts drag-and-drop from InventoryUI to bind items
    ///   • Left-click → activate slot (use item)
    ///   • Right-click → clear slot binding
    /// </summary>
    public class HotbarUI : MonoBehaviour
    {
        #region Inspector

        [Header("Prefab")]
        [Tooltip("Prefab for one hotbar slot. Must have Image, Button, and child TMP_Texts.")]
        [SerializeField] private GameObject _slotPrefab;

        [Header("Container")]
        [SerializeField] private Transform _slotsParent;

        [Header("Player")]
        [SerializeField] [Range(0, 3)] public int _playerIndex = 0;

        [Header("Visual")]
        [SerializeField] private Color _activeColor   = new Color(1f, 0.85f, 0.1f, 0.9f);
        [SerializeField] private Color _inactiveColor = new Color(1f, 1f, 1f, 0.25f);

        #endregion

        #region Runtime

        private HotbarData _hotbar;
        private readonly List<HotbarSlotWidget> _widgets = new List<HotbarSlotWidget>();

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            var mgr = ServiceLocator.TryGet<InventoryManager>();
            if (mgr == null) return;

            _hotbar = mgr.GetHotbar(_playerIndex);
            if (_hotbar == null) return;

            _hotbar.OnSlotChanged       += RefreshSlot;
            _hotbar.OnActiveSlotChanged += RefreshActive;

            BuildSlots();
        }

        private void OnDestroy()
        {
            if (_hotbar == null) return;
            _hotbar.OnSlotChanged       -= RefreshSlot;
            _hotbar.OnActiveSlotChanged -= RefreshActive;
        }

        #endregion

        #region Build

        private void BuildSlots()
        {
            foreach (var w in _widgets)
                if (w.Root) Destroy(w.Root.gameObject);
            _widgets.Clear();

            for (int i = 0; i < _hotbar.SlotCount; i++)
            {
                var go     = Instantiate(_slotPrefab, _slotsParent);
                var widget = new HotbarSlotWidget(go, i, this);
                _widgets.Add(widget);
                RefreshSlot(i);
            }

            RefreshActive(_hotbar.ActiveSlot);
        }

        #endregion

        #region Refresh

        private void RefreshSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _widgets.Count) return;
            var widget = _widgets[slotIndex];
            var item   = _hotbar.GetSlot(slotIndex);
            widget.SetItem(item);
        }

        private void RefreshActive(int activeSlot)
        {
            for (int i = 0; i < _widgets.Count; i++)
                _widgets[i].SetActive(i == activeSlot, _activeColor, _inactiveColor);
        }

        #endregion

        #region Slot Interaction

        internal void OnSlotClick(int slotIndex)
        {
            if (_hotbar == null) return;
            _hotbar.ActiveSlot = slotIndex;

            var item = _hotbar.GetSlot(slotIndex);
            if (item != null)
                InventoryEventBus.Publish(new ItemUsedEvent { Item = item, PlayerIndex = _playerIndex });
        }

        internal void OnSlotRightClick(int slotIndex)
        {
            _hotbar?.SetSlot(slotIndex, null);
        }

        /// <summary>
        /// Called when an inventory item is dropped onto a hotbar slot.
        /// Binds the item without moving it from the inventory.
        /// </summary>
        internal void OnDropOnSlot(int slotIndex, InventoryItem item)
        {
            _hotbar?.SetSlot(slotIndex, item);
        }

        #endregion
    }

    // ── Per-slot widget helper ────────────────────────────────────────────────

    /// <summary>Manages the visual state of one hotbar button.</summary>
    internal class HotbarSlotWidget : MonoBehaviour,
        IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public RectTransform Root       { get; private set; }
        public int           SlotIndex  { get; private set; }

        private HotbarUI  _owner;
        private Image     _iconImage;
        private TMP_Text  _countText;
        private TMP_Text  _keyText;
        private Image     _border;

        public HotbarSlotWidget(GameObject go, int index, HotbarUI owner)
        {
            // Copy component onto same GameObject
            Root       = go.GetComponent<RectTransform>();
            SlotIndex  = index;
            _owner     = owner;

            _iconImage = go.GetComponentInChildren<Image>();
            var texts  = go.GetComponentsInChildren<TMP_Text>();
            foreach (var t in texts)
            {
                if (t.name.Contains("Count")) _countText = t;
                if (t.name.Contains("Key"))   _keyText   = t;
            }

            // Key label (1-9)
            if (_keyText) _keyText.text = (index + 1).ToString();

            // Wire button
            var btn = go.GetComponent<Button>();
            if (btn) btn.onClick.AddListener(() => _owner.OnSlotClick(SlotIndex));

            // Attach this MonoBehaviour to the GO for event callbacks
            go.AddComponent<HotbarSlotEventProxy>().Init(this);
        }

        public void SetItem(InventoryItem item)
        {
            if (_iconImage)
            {
                _iconImage.sprite  = item?.Data.Icon;
                _iconImage.enabled = item?.Data.Icon != null;
            }

            bool showCount = item != null && item.Data.IsStackable && item.StackCount > 1;
            if (_countText)
            {
                _countText.text    = showCount ? item.StackCount.ToString() : "";
                _countText.enabled = showCount;
            }
        }

        public void SetActive(bool active, Color activeColor, Color inactiveColor)
        {
            if (_border) _border.color = active ? activeColor : inactiveColor;
        }

        // IPointerClickHandler
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Right)
                _owner.OnSlotRightClick(SlotIndex);
        }

        // IDropHandler — receive drag from InventoryUI
        public void OnDrop(PointerEventData e)
        {
            // The drag state is set on InventoryUI.s_drag (static)
            // We can't access static from here cleanly, so we use the EventBus
            // A simple workaround: read the last dragged item from a global
            var dragItem = DragItemTracker.LastDraggedItem;
            if (dragItem != null)
                _owner.OnDropOnSlot(SlotIndex, dragItem);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            var item = ServiceLocator.TryGet<InventoryManager>()
                ?.GetHotbar(_owner._playerIndex)?.GetSlot(SlotIndex);
            if (item != null)
                TooltipUI.Instance?.Show(item);
        }

        public void OnPointerExit(PointerEventData e)
        {
            TooltipUI.Instance?.Hide();
        }
    }

    // ── Proxy to forward Unity event interfaces from widget GO ────────────────

    /// <summary>MonoBehaviour proxy — forwards pointer events to the widget.</summary>
    internal class HotbarSlotEventProxy : MonoBehaviour,
        IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private HotbarSlotWidget _widget;
        public void Init(HotbarSlotWidget w) { _widget = w; }

        public void OnPointerClick(PointerEventData e)  => _widget?.OnPointerClick(e);
        public void OnDrop(PointerEventData e)           => _widget?.OnDrop(e);
        public void OnPointerEnter(PointerEventData e)   => _widget?.OnPointerEnter(e);
        public void OnPointerExit(PointerEventData e)    => _widget?.OnPointerExit(e);
    }

    // ── Global drag item tracker (used by hotbar drop handler) ────────────────

    /// <summary>
    /// Holds a reference to the most recently dragged InventoryItem.
    /// Set by InventoryUI.OnSlotBeginDrag, cleared on EndDrag.
    /// </summary>
    public static class DragItemTracker
    {
        public static InventoryItem LastDraggedItem { get; private set; }
        public static void Set(InventoryItem item)   => LastDraggedItem = item;
        public static void Clear()                   => LastDraggedItem = null;
    }
}
