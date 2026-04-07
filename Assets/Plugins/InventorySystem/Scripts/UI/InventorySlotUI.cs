// =============================================================================
// InventorySlotUI.cs
// A single grid-cell button in the inventory.
// =============================================================================
// Each cell knows its (Column, Row) coordinate. It delegates ALL interactions
// (click, right-click, pointer-enter, drag) back to the owning InventoryUI so
// that interaction logic stays in one place.
// =============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InventorySystem
{
    /// <summary>
    /// One cell in the inventory grid. Transparent by default; becomes tinted
    /// when an item shape occupies it or when highlighted during drag-hover.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class InventorySlotUI : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        #region State

        /// <summary>Column index in the grid (set by InventoryUI on creation).</summary>
        public int Column { get; private set; }

        /// <summary>Row index in the grid (set by InventoryUI on creation).</summary>
        public int Row { get; private set; }

        /// <summary>Grid coordinate as a Vector2Int.</summary>
        public Vector2Int GridPosition => new Vector2Int(Column, Row);

        /// <summary>Back-reference to the owning grid UI.</summary>
        public InventoryUI Owner { get; private set; }

        // Cached component
        private Image _image;

        // Colours
        private static readonly Color COLOR_NORMAL   = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color COLOR_HOVER    = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color COLOR_VALID    = new Color(0.2f, 1f, 0.3f, 0.35f);
        private static readonly Color COLOR_INVALID  = new Color(1f, 0.2f, 0.2f, 0.35f);
        private static readonly Color COLOR_OCCUPIED = new Color(1f, 1f, 1f, 0.10f);

        private bool _isHighlighted;

        #endregion

        #region Initialisation

        /// <summary>Must be called by InventoryUI immediately after instantiation.</summary>
        public void Initialise(int col, int row, InventoryUI owner)
        {
            Column = col;
            Row    = row;
            Owner  = owner;

            _image = GetComponent<Image>();
            SetNormal();
        }

        #endregion

        #region Visual State

        public void SetNormal()    => _image.color = COLOR_NORMAL;
        public void SetHover()     => _image.color = COLOR_HOVER;
        public void SetValid()     => _image.color = COLOR_VALID;
        public void SetInvalid()   => _image.color = COLOR_INVALID;
        public void SetOccupied()  => _image.color = COLOR_OCCUPIED;

        #endregion

        #region IPointerClickHandler

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                Owner?.OnSlotLeftClick(this, eventData);
            else if (eventData.button == PointerEventData.InputButton.Right)
                Owner?.OnSlotRightClick(this, eventData);
        }

        #endregion

        #region IPointerEnterHandler / IPointerExitHandler

        public void OnPointerEnter(PointerEventData eventData)
        {
            Owner?.OnSlotPointerEnter(this, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Owner?.OnSlotPointerExit(this, eventData);
        }

        #endregion

        #region IDragHandler, IBeginDragHandler, IEndDragHandler

        public void OnBeginDrag(PointerEventData eventData)
        {
            Owner?.OnSlotBeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Owner?.OnSlotDrag(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Owner?.OnSlotEndDrag(this, eventData);
        }

        #endregion

        #region IDropHandler

        public void OnDrop(PointerEventData eventData)
        {
            Owner?.OnSlotDrop(this, eventData);
        }

        #endregion
    }
}
