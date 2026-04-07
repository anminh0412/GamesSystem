// =============================================================================
// InventoryUI.cs
// Master controller for one inventory grid panel.
// =============================================================================
//
// Responsibilities:
//   • Build and pool the NxM InventorySlotUI cell grid
//   • Create / recycle InventoryItemUI panels for placed items
//   • Handle all slot callbacks (left-click, right-click, hover, drag, drop)
//   • Implement drag-and-drop (intra- and inter-inventory)
//   • Shift+click → SplitStackUI
//   • Drag same item on same item → merge stacks
//   • Right-click → ContextMenuUI with pluggable IItemAction list
//   • Hover → TooltipUI
//
// Setup:
//   1. Create a Canvas panel with a GridLayoutGroup child for slots.
//   2. Create a free-form RectTransform child (same size) for item visuals.
//   3. Assign prefabs for InventorySlotUI and InventoryItemUI.
//   4. Bind to a runtime Inventory via Bind().
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InventorySystem
{
    /// <summary>
    /// UI controller for a single inventory container.
    /// Attach one per inventory panel in the scene.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        #region Inspector

        [Header("Prefabs")]
        [Tooltip("Prefab with InventorySlotUI component.")]
        [SerializeField] private GameObject _slotPrefab;

        [Tooltip("Prefab with InventoryItemUI component.")]
        [SerializeField] private GameObject _itemUIPrefab;

        [Header("Containers")]
        [Tooltip("Parent for slot grid cells (must have GridLayoutGroup).")]
        [SerializeField] private RectTransform _slotsContainer;

        [Tooltip("Parent for item visual panels (free layout, same size as grid).")]
        [SerializeField] private RectTransform _itemsContainer;

        [Tooltip("Parent for the drag ghost (usually the root Canvas, drag layer).")]
        [SerializeField] private RectTransform _dragLayer;

        [Header("Grid Layout")]
        [SerializeField] private float _cellSize    = InventorySystemConstants.CELL_SIZE;
        [SerializeField] private float _cellPadding = InventorySystemConstants.CELL_PADDING;

        [Header("Player Index")]
        [Tooltip("Which player owns this UI (used for actions like Equip).")]
        [SerializeField] [Range(0, 3)] private int _playerIndex = 0;

        [Header("Item Actions")]
        [Tooltip("Set of IItemAction implementations shown in the context menu. " +
                 "Add action MonoBehaviours to this list or override BuildActionList().")]
        [SerializeField] private List<MonoBehaviour> _registeredActionComponents = new List<MonoBehaviour>();

        [Header("Behaviour")]
        [Tooltip("Allow dragging items from this inventory.")]
        [SerializeField] private bool _allowDrag = true;

        [Tooltip("Allow dropping items into this inventory.")]
        [SerializeField] private bool _allowDrop = true;

        #endregion

        #region Runtime Data

        /// <summary>The inventory data model this UI represents.</summary>
        public Inventory BoundInventory { get; private set; }

        // Cell grid [col, row]
        private InventorySlotUI[,] _slots;

        // Item → its UI panel
        private readonly Dictionary<InventoryItem, InventoryItemUI> _itemUIs
            = new Dictionary<InventoryItem, InventoryItemUI>();

        // Object pools
        private readonly Stack<InventorySlotUI>  _slotPool = new Stack<InventorySlotUI>();
        private readonly Stack<InventoryItemUI>  _itemPool = new Stack<InventoryItemUI>();

        // Active slots list for easy iteration
        private readonly List<InventorySlotUI>   _activeSlots = new List<InventorySlotUI>();

        // Shared drag state — static so cross-inventory drops work
        private static DragState s_drag = new DragState();

        // Currently highlighted cells
        private readonly List<InventorySlotUI> _highlightedSlots = new List<InventorySlotUI>();

        // Selection
        private InventoryItem _selectedItem;

        // Cached action list (rebuilt per right-click)
        private readonly List<IItemAction> _actionCache = new List<IItemAction>();

        #endregion

        #region Events

        /// <summary>Fired when the user selects an item (left-click).</summary>
        public event Action<InventoryItem> OnItemSelected;

        /// <summary>Fired after a successful drop / move.</summary>
        public event Action<InventoryItem, Vector2Int> OnItemMoved;

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            UnbindInventory();
        }

        #endregion

        #region Bind / Unbind

        /// <summary>
        /// Binds this UI to a runtime Inventory and rebuilds the grid.
        /// Call this after creating or loading the inventory.
        /// </summary>
        public void Bind(Inventory inventory)
        {
            UnbindInventory();

            BoundInventory = inventory;

            inventory.OnItemAdded    += HandleItemAdded;
            inventory.OnItemRemoved  += HandleItemRemoved;
            inventory.OnStackChanged += HandleStackChanged;
            inventory.OnLayoutChanged += HandleLayoutChanged;
            inventory.OnCleared      += HandleCleared;

            RebuildGrid();
        }

        private void UnbindInventory()
        {
            if (BoundInventory == null) return;

            BoundInventory.OnItemAdded    -= HandleItemAdded;
            BoundInventory.OnItemRemoved  -= HandleItemRemoved;
            BoundInventory.OnStackChanged -= HandleStackChanged;
            BoundInventory.OnLayoutChanged -= HandleLayoutChanged;
            BoundInventory.OnCleared      -= HandleCleared;

            BoundInventory = null;
        }

        #endregion

        #region Grid Construction

        /// <summary>Tears down and rebuilds the entire slot grid and item panels.</summary>
        public void RebuildGrid()
        {
            if (BoundInventory == null) return;

            ReturnAllSlotsToPool();
            ReturnAllItemsToPool();
            _activeSlots.Clear();

            int cols = BoundInventory.Grid.Columns;
            int rows = BoundInventory.Grid.Rows;

            _slots = new InventorySlotUI[cols, rows];

            // Resize the GridLayoutGroup
            var layout = _slotsContainer.GetComponent<GridLayoutGroup>();
            if (layout)
            {
                layout.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = cols;
                layout.cellSize        = new Vector2(_cellSize, _cellSize);
                layout.spacing         = new Vector2(_cellPadding, _cellPadding);
            }

            // Size containers to fit grid
            float totalW = cols * (_cellSize + _cellPadding) - _cellPadding;
            float totalH = rows * (_cellSize + _cellPadding) - _cellPadding;

            _slotsContainer.sizeDelta = new Vector2(totalW, totalH);
            if (_itemsContainer) _itemsContainer.sizeDelta = new Vector2(totalW, totalH);

            // Create slot buttons
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var slot = RentSlot();
                    slot.Initialise(c, r, this);
                    slot.transform.SetParent(_slotsContainer, worldPositionStays: false);
                    _slots[c, r] = slot;
                    _activeSlots.Add(slot);
                }
            }

            // Create item panels for every placed item
            foreach (var item in BoundInventory.Items)
                CreateOrUpdateItemUI(item);
        }

        #endregion

        #region Slot Callbacks (called by InventorySlotUI)

        // ── Left Click ────────────────────────────────────────────────────────

        public void OnSlotLeftClick(InventorySlotUI slot, PointerEventData evt)
        {
            if (BoundInventory == null) return;

            var item = BoundInventory.Grid.GetAt(slot.GridPosition);
            if (item == null) { Deselect(); return; }

            // Shift+Click → split
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (shiftHeld && item.Data.IsStackable && item.StackCount > 1)
            {
                OpenSplitDialog(item);
                return;
            }

            Select(item);
        }

        // ── Right Click ───────────────────────────────────────────────────────

        public void OnSlotRightClick(InventorySlotUI slot, PointerEventData evt)
        {
            if (BoundInventory == null) return;

            var item = BoundInventory.Grid.GetAt(slot.GridPosition);
            if (item == null) return;

            OpenContextMenu(item, evt.position);
        }

        // ── Hover ─────────────────────────────────────────────────────────────

        public void OnSlotPointerEnter(InventorySlotUI slot, PointerEventData evt)
        {
            if (BoundInventory == null) return;

            var item = BoundInventory.Grid.GetAt(slot.GridPosition);
            if (item != null)
                TooltipUI.Instance?.Show(item);

            // During drag: update hover highlight
            if (s_drag.IsActive)
                UpdateDragHover(slot);
        }

        public void OnSlotPointerExit(InventorySlotUI slot, PointerEventData evt)
        {
            TooltipUI.Instance?.Hide();
        }

        // ── Drag ──────────────────────────────────────────────────────────────

        public void OnSlotBeginDrag(InventorySlotUI slot, PointerEventData evt)
        {
            if (!_allowDrag || BoundInventory == null) return;

            var item = BoundInventory.Grid.GetAt(slot.GridPosition);
            if (item == null) return;

            TooltipUI.Instance?.Hide();
            ContextMenuUI.Instance?.Hide();

            s_drag = DragState.Create(item, BoundInventory, this);
            s_drag.CurrentRotation = item.Rotation;
            DragItemTracker.Set(item);

            // Create ghost
            var ghostGo = CreateGhostUI(item);
            s_drag.GhostUI = ghostGo.GetComponent<InventoryItemUI>();

            // Visually mark the original as dragging
            if (_itemUIs.TryGetValue(item, out var origUI))
                origUI.SetDragging(true);
        }

        public void OnSlotDrag(InventorySlotUI slot, PointerEventData evt)
        {
            if (!s_drag.IsActive) return;

            // Move ghost to cursor
            if (s_drag.GhostUI != null && _dragLayer != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _dragLayer, evt.position, evt.pressEventCamera, out Vector2 localPt);
                s_drag.GhostUI.GetComponent<RectTransform>().anchoredPosition = localPt;
            }
        }

        public void OnSlotEndDrag(InventorySlotUI slot, PointerEventData evt)
        {
            if (!s_drag.IsActive) return;
            CancelDrag(); // If OnDrop was not called
        }

        public void OnSlotDrop(InventorySlotUI slot, PointerEventData evt)
        {
            if (!s_drag.IsActive || !_allowDrop) return;

            CommitDrop(slot);
        }

        #endregion

        #region Drag & Drop Logic

        private void UpdateDragHover(InventorySlotUI hoveredSlot)
        {
            if (!s_drag.IsActive) return;

            ClearHighlights();
            s_drag.HoveredSlot = hoveredSlot;
            s_drag.HoveredInventoryUI = this;

            var cells = s_drag.Item.Data.Shape.GetWorldCells(
                hoveredSlot.GridPosition, s_drag.CurrentRotation);

            bool valid = BoundInventory.Grid.CanPlace(
                s_drag.Item, hoveredSlot.GridPosition,
                s_drag.CurrentRotation,
                ignoreItem: s_drag.SourceInventory == BoundInventory ? s_drag.Item : null);

            s_drag.IsValidDrop = valid;

            foreach (var cell in cells)
            {
                var cellSlot = GetSlotAt(cell.x, cell.y);
                if (cellSlot == null) continue;
                if (valid) cellSlot.SetValid();
                else       cellSlot.SetInvalid();
                _highlightedSlots.Add(cellSlot);
            }
        }

        private void CommitDrop(InventorySlotUI targetSlot)
        {
            var item     = s_drag.Item;
            var srcInv   = s_drag.SourceInventory;
            var targetInv = BoundInventory;
            var pivot    = targetSlot.GridPosition;
            var rot      = s_drag.CurrentRotation;

            // Destroy ghost
            if (s_drag.GhostUI != null)
                Destroy(s_drag.GhostUI.gameObject);

            // Un-dim original
            if (s_drag.SourceUI != null && s_drag.SourceUI._itemUIs.TryGetValue(item, out var origUI))
                origUI.SetDragging(false);

            ClearHighlights();

            // ── Check for merge (drop on same item type) ──────────────────────
            var targetOccupant = targetInv.Grid.GetAt(pivot);
            if (targetOccupant != null && targetOccupant != item
                && targetOccupant.Data.ItemID == item.Data.ItemID
                && item.Data.IsStackable)
            {
                int leftover = targetOccupant.Merge(item);
                if (leftover == 0)
                {
                    // Fully merged — remove source item
                    srcInv.RemoveItemInstance(item);
                }
                // else partial merge — item remains in source with updated count
                s_drag.Clear();
                return;
            }

            // ── Same inventory: move ──────────────────────────────────────────
            if (srcInv == targetInv)
            {
                bool moved = targetInv.MoveItem(item, pivot, rot);
                if (!moved)
                {
                    // Snap back — no change needed (item is still at original pos)
                    Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Move invalid — snapping back.");
                }
                else
                {
                    RefreshItemUI(item);
                    OnItemMoved?.Invoke(item, pivot);
                    InventoryEventBus.Publish(new ItemMovedEvent
                        { Inventory = targetInv, Item = item, FromCell = s_drag.OriginalGridPos, ToCell = pivot });
                }
            }
            // ── Cross-inventory drop ──────────────────────────────────────────
            else
            {
                // Validate space in target
                if (!targetInv.Grid.CanPlace(item, pivot, rot))
                {
                    Debug.Log($"{InventorySystemConstants.LOG_PREFIX} No space in target inventory.");
                    s_drag.Clear();
                    return;
                }

                // Transfer: remove from source, add to target at specific position
                srcInv.RemoveItemInstance(item);
                item.GridPosition = new Vector2Int(-1, -1); // clear placement
                targetInv.Grid.Place(item, pivot, rot);
                // Manually add to target item list (bypass AddItem to preserve position)
                AddItemDirectly(targetInv, item);

                s_drag.SourceUI?.RemoveItemUI(item);
                CreateOrUpdateItemUI(item);
            }

            s_drag.Clear();
        }

        private void CancelDrag()
        {
            if (!s_drag.IsActive) return;

            if (s_drag.GhostUI != null)
                Destroy(s_drag.GhostUI.gameObject);

            if (s_drag.SourceUI != null && s_drag.SourceUI._itemUIs.TryGetValue(s_drag.Item, out var origUI))
                origUI.SetDragging(false);

            ClearHighlights();
            DragItemTracker.Clear();
            DragItemTracker.Clear();
            s_drag.Clear();
        }

        #endregion

        #region Rotate During Drag

        /// <summary>
        /// Called by InventoryInputHandler when the Rotate action fires while dragging.
        /// Rotates the in-flight item 90° CW and updates the ghost.
        /// </summary>
        public void RotateDragging()
        {
            if (!s_drag.IsActive) return;

            s_drag.CurrentRotation = (ShapeRotation)(
                ((int)s_drag.CurrentRotation + 1) % InventorySystemConstants.ROTATION_STATES);

            // Update ghost size
            if (s_drag.GhostUI != null)
                s_drag.GhostUI.UpdateLayout(_cellSize, _cellPadding);

            // Refresh hover highlight
            if (s_drag.HoveredSlot != null)
                UpdateDragHover(s_drag.HoveredSlot);
        }

        #endregion

        #region Split

        private void OpenSplitDialog(InventoryItem item)
        {
            SplitStackUI.Instance?.Show(item, BoundInventory, CommitSplit);
        }

        /// <summary>
        /// Called by SplitStackUI on confirm. Splits <paramref name="amount"/> off
        /// <paramref name="item"/> and places the new stack in the first available slot.
        /// </summary>
        public void CommitSplit(InventoryItem item, int amount)
        {
            if (item == null || amount <= 0 || amount >= item.StackCount) return;

            var newItem = item.Split(amount);
            if (newItem == null) return;

            // Find space for the new stack
            var pivot = BoundInventory.Grid.FindFirstFit(newItem, ShapeRotation.Rot0);
            if (pivot.x < 0)
            {
                // No space — merge back
                item.Merge(newItem);
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Split failed — no room for new stack.");
                return;
            }

            BoundInventory.Grid.Place(newItem, pivot, ShapeRotation.Rot0);
            AddItemDirectly(BoundInventory, newItem);
            CreateOrUpdateItemUI(newItem);

            // Refresh source item UI (stack count changed)
            RefreshItemUI(item);
        }

        #endregion

        #region Context Menu

        private void OpenContextMenu(InventoryItem item, Vector2 screenPos)
        {
            BuildActionList(item, _actionCache);
            ContextMenuUI.Instance?.Show(item, BoundInventory, _actionCache, screenPos);
        }

        /// <summary>
        /// Builds the list of IItemAction objects for the given item.
        /// Override in a subclass to add custom actions.
        /// </summary>
        protected virtual void BuildActionList(InventoryItem item, List<IItemAction> list)
        {
            list.Clear();

            // From registered action components
            foreach (var comp in _registeredActionComponents)
            {
                if (comp is IItemAction action)
                    list.Add(action);
            }

            // Built-in fallbacks if nothing registered
            if (list.Count == 0)
            {
                list.Add(new UseItemAction());
                list.Add(new EquipItemAction(_playerIndex));
                list.Add(new DropItemAction(transform));
                list.Add(new DestroyItemAction());
            }

            // Always offer split for large stackable items
            if (item.Data.IsStackable && item.StackCount > 1)
                list.Add(new SplitActionAdapter(this, item));
        }

        #endregion

        #region Selection

        private void Select(InventoryItem item)
        {
            if (_selectedItem == item) { Deselect(); return; }

            Deselect();
            _selectedItem = item;

            if (_itemUIs.TryGetValue(item, out var ui))
                ui.SetSelected(true);

            OnItemSelected?.Invoke(item);
        }

        private void Deselect()
        {
            if (_selectedItem != null && _itemUIs.TryGetValue(_selectedItem, out var ui))
                ui.SetSelected(false);
            _selectedItem = null;
        }

        #endregion

        #region Item UI Management

        private void CreateOrUpdateItemUI(InventoryItem item)
        {
            if (!item.IsPlaced) return;

            if (_itemUIs.TryGetValue(item, out var existing))
            {
                existing.Refresh();
                existing.UpdateLayout(_cellSize, _cellPadding);
                return;
            }

            var ui = RentItemUI();
            ui.transform.SetParent(_itemsContainer, worldPositionStays: false);
            ui.Bind(item, this);
            ui.UpdateLayout(_cellSize, _cellPadding);
            ui.gameObject.SetActive(true);
            _itemUIs[item] = ui;
        }

        private void RefreshItemUI(InventoryItem item)
        {
            if (_itemUIs.TryGetValue(item, out var ui))
            {
                ui.Refresh();
                ui.UpdateLayout(_cellSize, _cellPadding);
            }
        }

        private void RemoveItemUI(InventoryItem item)
        {
            if (!_itemUIs.TryGetValue(item, out var ui)) return;
            ui.Unbind();
            ReturnItemToPool(ui);
            _itemUIs.Remove(item);
        }

        private GameObject CreateGhostUI(InventoryItem item)
        {
            var layer = _dragLayer != null ? _dragLayer : (RectTransform)transform;
            var go    = Instantiate(_itemUIPrefab, layer);
            var ui    = go.GetComponent<InventoryItemUI>();

            ui.Bind(item, this);
            ui.UpdateLayout(_cellSize, _cellPadding);
            ui.SetDragging(true);

            // Disable raycasts on ghost so slots beneath can still receive events
            var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            return go;
        }

        #endregion

        #region Inventory Event Handlers

        private void HandleItemAdded(InventoryItem item, int amount)
        {
            CreateOrUpdateItemUI(item);
        }

        private void HandleItemRemoved(string itemID, int amount)
        {
            // Find the item UI whose item no longer exists in the inventory
            var toRemove = new List<InventoryItem>();
            foreach (var kvp in _itemUIs)
                if (!BoundInventory.Items.Contains(kvp.Key))
                    toRemove.Add(kvp.Key);
            foreach (var it in toRemove)
                RemoveItemUI(it);
        }

        private void HandleStackChanged(InventoryItem item)
        {
            RefreshItemUI(item);
        }

        private void HandleLayoutChanged()
        {
            // Full refresh — grid was resized or items were moved programmatically
            RebuildGrid();
        }

        private void HandleCleared()
        {
            ReturnAllItemsToPool();
            _itemUIs.Clear();
        }

        #endregion

        #region Highlight Helpers

        private void ClearHighlights()
        {
            foreach (var slot in _highlightedSlots)
                slot?.SetNormal();
            _highlightedSlots.Clear();
        }

        private InventorySlotUI GetSlotAt(int col, int row)
        {
            if (_slots == null) return null;
            if (col < 0 || col >= _slots.GetLength(0)) return null;
            if (row < 0 || row >= _slots.GetLength(1)) return null;
            return _slots[col, row];
        }

        #endregion

        #region Pooling

        // ── Slots ─────────────────────────────────────────────────────────────

        private InventorySlotUI RentSlot()
        {
            if (_slotPool.Count > 0)
            {
                var s = _slotPool.Pop();
                s.gameObject.SetActive(true);
                return s;
            }
            var go = Instantiate(_slotPrefab);
            return go.GetComponent<InventorySlotUI>();
        }

        private void ReturnSlotToPool(InventorySlotUI slot)
        {
            slot.gameObject.SetActive(false);
            slot.transform.SetParent(_slotsContainer, worldPositionStays: false);
            _slotPool.Push(slot);
        }

        private void ReturnAllSlotsToPool()
        {
            foreach (var slot in _activeSlots)
                if (slot != null) ReturnSlotToPool(slot);
            _activeSlots.Clear();
            _slots = null;
        }

        // ── Item UIs ──────────────────────────────────────────────────────────

        private InventoryItemUI RentItemUI()
        {
            if (_itemPool.Count > 0)
            {
                var i = _itemPool.Pop();
                i.gameObject.SetActive(true);
                return i;
            }
            var go = Instantiate(_itemUIPrefab);
            return go.GetComponent<InventoryItemUI>();
        }

        private void ReturnItemToPool(InventoryItemUI itemUI)
        {
            itemUI.gameObject.SetActive(false);
            itemUI.transform.SetParent(_itemsContainer, worldPositionStays: false);
            _itemPool.Push(itemUI);
        }

        private void ReturnAllItemsToPool()
        {
            foreach (var kvp in _itemUIs)
            {
                kvp.Value.Unbind();
                ReturnItemToPool(kvp.Value);
            }
            _itemUIs.Clear();
        }

        #endregion

        #region Internal Helpers

        /// <summary>
        /// Registers an already-placed item into the inventory list without
        /// re-running placement logic. Used by cross-inventory drag-drop.
        /// </summary>
        private static void AddItemDirectly(Inventory inv, InventoryItem item)
        {
            inv.AddPlaced(item);
        }

        #endregion
    }

    // =========================================================================
    // Inline adapter — lets "Split" appear in the context menu
    // =========================================================================

    /// <summary>
    /// Wraps the split action as an IItemAction so it appears in the context menu.
    /// </summary>
    internal class SplitActionAdapter : IItemAction
    {
        private readonly InventoryUI   _ui;
        private readonly InventoryItem _item;

        public SplitActionAdapter(InventoryUI ui, InventoryItem item) { _ui = ui; _item = item; }

        public string ActionName => LocalizationBridge.GetUIString("btn_split");

        public bool CanExecute(InventoryItem item, IInventory inventory)
            => item?.Data.IsStackable == true && item.StackCount > 1;

        public void Execute(InventoryItem item, IInventory inventory)
            => _ui.CommitSplit(item, item.StackCount / 2);
    }
}
