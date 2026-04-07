// =============================================================================
// InventoryItemUI.cs
// Visual panel that represents one InventoryItem on the grid.
// =============================================================================
// Positioned and sized to cover every cell the item occupies.
// Displays icon, stack count badge, upgrade badge, and rarity border colour.
// Does NOT handle input — that goes through InventorySlotUI → InventoryUI.
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;                  // Remove this line if not using TextMeshPro
// using UnityEngine.UI;      // Uncomment and use Text if not using TMP

namespace InventorySystem
{
    /// <summary>
    /// Visual representation of a placed <see cref="InventoryItem"/> on the grid.
    /// </summary>
    public class InventoryItemUI : MonoBehaviour
    {
        #region Inspector References

        [Header("Visual Elements")]
        [SerializeField] private Image     _iconImage;
        [SerializeField] private Image     _rarityBorder;
        [SerializeField] private Image     _selectionOverlay;   // tinted overlay when selected
        [SerializeField] private Image     _dragGhostOverlay;   // semi-transparent during drag

        [Header("Badges")]
        [SerializeField] private GameObject _stackBadge;
        [SerializeField] private TMP_Text   _stackCountText;     // "×99"
        [SerializeField] private GameObject _upgradeBadge;
        [SerializeField] private TMP_Text   _upgradeLevelText;   // "+3"

        #endregion

        #region Runtime Data

        /// <summary>The inventory item this UI element represents.</summary>
        public InventoryItem Item { get; private set; }

        private InventoryUI _owner;
        private RectTransform _rect;

        #endregion

        #region Colours / state

        private static readonly Color SELECTED_COLOR = new Color(1f, 0.92f, 0.3f, 0.25f);
        private static readonly Color DRAG_COLOR     = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color NORMAL_COLOR   = new Color(1f, 1f, 1f, 0f);

        #endregion

        #region Initialisation

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();

            // Ensure overlay images start invisible
            if (_selectionOverlay) _selectionOverlay.color = NORMAL_COLOR;
            if (_dragGhostOverlay) _dragGhostOverlay.color = NORMAL_COLOR;
        }

        /// <summary>
        /// Binds this UI element to a runtime item and positions it on the grid.
        /// Must be called right after the item is placed.
        /// </summary>
        public void Bind(InventoryItem item, InventoryUI owner)
        {
            Item   = item;
            _owner = owner;

            Refresh();

            // Subscribe to stack changes so the badge updates automatically
            item.OnStackChanged -= OnStackChanged;
            item.OnStackChanged += OnStackChanged;
        }

        /// <summary>Detaches event listeners. Call before returning to pool.</summary>
        public void Unbind()
        {
            if (Item != null)
                Item.OnStackChanged -= OnStackChanged;
            Item = null;
        }

        #endregion

        #region Refresh (sync visuals to data)

        /// <summary>Full visual refresh — call after any data change.</summary>
        public void Refresh()
        {
            if (Item == null) return;

            // Icon
            if (_iconImage)
            {
                _iconImage.sprite  = Item.Data.Icon;
                _iconImage.enabled = Item.Data.Icon != null;
            }

            // Rarity border tint
            if (_rarityBorder)
                _rarityBorder.color = Item.Data.GetRarityColor();

            // Stack badge
            bool showStack = Item.Data.IsStackable && Item.StackCount > 1;
            if (_stackBadge)   _stackBadge.SetActive(showStack);
            if (_stackCountText && showStack)
                _stackCountText.text = $"×{Item.StackCount}";

            // Upgrade badge
            bool showUpgrade = Item.UpgradeLevel > 0;
            if (_upgradeBadge)     _upgradeBadge.SetActive(showUpgrade);
            if (_upgradeLevelText && showUpgrade)
                _upgradeLevelText.text = $"+{Item.UpgradeLevel}";
        }

        /// <summary>
        /// Repositions and resizes this panel to cover all cells of the item's shape,
        /// based on its current GridPosition, Rotation, and the cell size from InventoryUI.
        /// </summary>
        public void UpdateLayout(float cellSize, float cellPadding)
        {
            if (Item == null || !Item.IsPlaced) return;

            var bounds = Item.Data.Shape.GetBounds(Item.Rotation);
            float padX = cellPadding * 0.5f;
            float padY = cellPadding * 0.5f;

            // Pivot: top-left of bounding box in grid space
            float left = Item.GridPosition.x * (cellSize + cellPadding);
            float top  = Item.GridPosition.y * (cellSize + cellPadding);

            float w = bounds.x * (cellSize + cellPadding) - cellPadding;
            float h = bounds.y * (cellSize + cellPadding) - cellPadding;

            _rect.anchorMin = Vector2.up;  // top-left anchor
            _rect.anchorMax = Vector2.up;
            _rect.pivot     = Vector2.up;

            _rect.anchoredPosition = new Vector2(left, -top);
            _rect.sizeDelta        = new Vector2(w, h);
        }

        #endregion

        #region Selection / Drag Visual

        public void SetSelected(bool selected)
        {
            if (_selectionOverlay)
                _selectionOverlay.color = selected ? SELECTED_COLOR : NORMAL_COLOR;
        }

        public void SetDragging(bool dragging)
        {
            if (_dragGhostOverlay)
                _dragGhostOverlay.color = dragging ? DRAG_COLOR : NORMAL_COLOR;

            // Lower alpha on icon to show "lifted" state
            if (_iconImage)
                _iconImage.color = dragging ? new Color(1f, 1f, 1f, 0.6f) : Color.white;
        }

        #endregion

        #region Event Handlers

        private void OnStackChanged(InventoryItem item, int newCount)
        {
            Refresh();
        }

        #endregion

        #region Utility

        public override string ToString() => $"ItemUI[{Item?.Data.ItemName}]";

        #endregion
    }
}
