// =============================================================================
// DragState.cs
// Shared drag-and-drop context — one instance per drag operation.
// =============================================================================
// Carried as a static field on InventoryUI so cross-inventory drops can access
// the in-flight item without needing direct component references.
// =============================================================================

using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Captures everything needed to describe an in-progress drag operation.
    /// Created on BeginDrag, cleared on EndDrag or successful Drop.
    /// </summary>
    public class DragState
    {
        // ── Source ────────────────────────────────────────────────────────────

        /// <summary>The item being dragged.</summary>
        public InventoryItem Item;

        /// <summary>The inventory the item came from.</summary>
        public Inventory SourceInventory;

        /// <summary>The InventoryUI that started the drag.</summary>
        public InventoryUI SourceUI;

        /// <summary>Grid position in the source inventory before the drag started.</summary>
        public Vector2Int OriginalGridPos;

        /// <summary>Rotation in the source inventory before the drag started.</summary>
        public ShapeRotation OriginalRotation;

        // ── Ghost / Cursor ────────────────────────────────────────────────────

        /// <summary>
        /// The ghost panel that follows the cursor.
        /// Instantiated from the InventoryItemUI prefab, reparented to the drag layer.
        /// </summary>
        public InventoryItemUI GhostUI;

        /// <summary>Current rotation of the ghost (may differ from source).</summary>
        public ShapeRotation CurrentRotation;

        // ── Hover state ───────────────────────────────────────────────────────

        /// <summary>The InventoryUI currently under the cursor during drag.</summary>
        public InventoryUI HoveredInventoryUI;

        /// <summary>The cell currently under the cursor during drag.</summary>
        public InventorySlotUI HoveredSlot;

        /// <summary>Whether the current hover position is a valid drop target.</summary>
        public bool IsValidDrop;

        // ── Factory ───────────────────────────────────────────────────────────

        public static DragState Create(InventoryItem item, Inventory source, InventoryUI sourceUI)
        {
            return new DragState
            {
                Item              = item,
                SourceInventory   = source,
                SourceUI          = sourceUI,
                OriginalGridPos   = item.GridPosition,
                OriginalRotation  = item.Rotation,
                CurrentRotation   = item.Rotation,
                IsValidDrop       = false
            };
        }

        public bool IsActive => Item != null;

        public void Clear()
        {
            Item              = null;
            SourceInventory   = null;
            SourceUI          = null;
            GhostUI           = null;
            HoveredInventoryUI = null;
            HoveredSlot       = null;
        }
    }
}
