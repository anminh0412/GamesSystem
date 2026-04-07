// =============================================================================
// InventoryGrid.cs
// 2-D grid engine for Tetris / RE4-style item placement.
// =============================================================================
// The grid stores a 2-D array of InventoryItem references.
// Every cell that an item occupies points to the SAME InventoryItem object.
// A cell value of null means the cell is empty.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Core spatial grid that tracks which item occupies which cells.
    /// Used by <see cref="Inventory"/> to implement the Tetris-style layout.
    /// Thread-safe reads; writes must happen on the main thread.
    /// </summary>
    public class InventoryGrid
    {
        #region Fields

        private InventoryItem[,] _cells; // [col, row]
        private int _columns;
        private int _rows;

        #endregion

        #region Properties

        public int Columns => _columns;
        public int Rows    => _rows;
        public int TotalCells => _columns * _rows;

        #endregion

        #region Events

        /// <summary>Fired after any cell state changes. Arg = changed cells.</summary>
        public event Action<IReadOnlyList<Vector2Int>> OnGridChanged;

        #endregion

        #region Constructor

        public InventoryGrid(int columns, int rows)
        {
            Resize(columns, rows);
        }

        #endregion

        #region Resize

        /// <summary>
        /// Resizes the grid. Items that no longer fit after a shrink are returned
        /// as displaced items that the caller must re-insert or handle.
        /// </summary>
        public List<InventoryItem> Resize(int newColumns, int newRows)
        {
            newColumns = Mathf.Clamp(newColumns, 1, InventorySystemConstants.MAX_GRID_COLUMNS);
            newRows    = Mathf.Clamp(newRows,    1, InventorySystemConstants.MAX_GRID_ROWS);

            var displaced = new List<InventoryItem>();

            if (_cells != null)
            {
                // Collect items that would fall outside the new bounds
                var seen = new HashSet<InventoryItem>();
                for (int c = 0; c < _columns; c++)
                {
                    for (int r = 0; r < _rows; r++)
                    {
                        var item = _cells[c, r];
                        if (item == null || seen.Contains(item)) continue;
                        seen.Add(item);

                        // Check if item's pivot is outside the new grid
                        if (item.GridPosition.x >= newColumns || item.GridPosition.y >= newRows)
                        {
                            ForceRemove(item);
                            displaced.Add(item);
                        }
                    }
                }
            }

            _columns = newColumns;
            _rows    = newRows;

            var newCells = new InventoryItem[_columns, _rows];

            // Copy surviving placements
            if (_cells != null)
            {
                int copyC = Mathf.Min(_columns, _cells.GetLength(0));
                int copyR = Mathf.Min(_rows,    _cells.GetLength(1));
                for (int c = 0; c < copyC; c++)
                    for (int r = 0; r < copyR; r++)
                        newCells[c, r] = _cells[c, r];
            }

            _cells = newCells;
            return displaced;
        }

        #endregion

        #region Query

        /// <summary>Returns the item at (col, row), or null if empty / out of bounds.</summary>
        public InventoryItem GetAt(int col, int row)
        {
            if (!IsInBounds(col, row)) return null;
            return _cells[col, row];
        }

        public InventoryItem GetAt(Vector2Int pos) => GetAt(pos.x, pos.y);

        /// <summary>True when (col, row) is inside the grid and contains no item.</summary>
        public bool IsCellEmpty(int col, int row)
            => IsInBounds(col, row) && _cells[col, row] == null;

        public bool IsCellEmpty(Vector2Int pos) => IsCellEmpty(pos.x, pos.y);

        public bool IsInBounds(int col, int row)
            => col >= 0 && col < _columns && row >= 0 && row < _rows;

        public bool IsInBounds(Vector2Int pos) => IsInBounds(pos.x, pos.y);

        /// <summary>Count of currently empty cells.</summary>
        public int EmptyCellCount()
        {
            int count = 0;
            for (int c = 0; c < _columns; c++)
                for (int r = 0; r < _rows; r++)
                    if (_cells[c, r] == null) count++;
            return count;
        }

        #endregion

        #region Placement Validation

        /// <summary>
        /// Returns true when <paramref name="item"/> (with its data shape and
        /// <paramref name="rotation"/>) can be placed at <paramref name="pivot"/>
        /// without going out-of-bounds or overlapping another item.
        /// Pass <paramref name="ignoreItem"/> to exclude an item already on the
        /// grid from collision checks (used during drag-and-drop moves).
        /// </summary>
        public bool CanPlace(InventoryItem item, Vector2Int pivot,
                             ShapeRotation rotation,
                             InventoryItem ignoreItem = null)
        {
            if (item == null) return false;

            var cells = item.Data.Shape.GetWorldCells(pivot, rotation);
            foreach (var cell in cells)
            {
                if (!IsInBounds(cell))    return false;

                var occupant = _cells[cell.x, cell.y];
                if (occupant != null && occupant != item && occupant != ignoreItem)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Finds the first valid pivot position for placing <paramref name="item"/>
        /// (scanning left-to-right, top-to-bottom). Returns Vector2Int(-1,-1) if
        /// no space is found.
        /// </summary>
        public Vector2Int FindFirstFit(InventoryItem item, ShapeRotation rotation)
        {
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _columns; c++)
                {
                    var pivot = new Vector2Int(c, r);
                    if (CanPlace(item, pivot, rotation))
                        return pivot;
                }
            }
            return new Vector2Int(-1, -1);
        }

        #endregion

        #region Placement Mutation

        /// <summary>
        /// Places <paramref name="item"/> at <paramref name="pivot"/> with the given rotation.
        /// Returns false and does nothing if placement is invalid.
        /// </summary>
        public bool Place(InventoryItem item, Vector2Int pivot, ShapeRotation rotation)
        {
            if (!CanPlace(item, pivot, rotation)) return false;

            var cells = item.Data.Shape.GetWorldCells(pivot, rotation);
            foreach (var cell in cells)
                _cells[cell.x, cell.y] = item;

            item.GridPosition = pivot;
            item.Rotation     = rotation;

            OnGridChanged?.Invoke(cells);
            return true;
        }

        /// <summary>
        /// Removes <paramref name="item"/> from all cells it currently occupies.
        /// Returns false if the item is not in this grid.
        /// </summary>
        public bool Remove(InventoryItem item)
        {
            if (item == null || !item.IsPlaced) return false;

            var cells = item.GetOccupiedCells();
            bool found = false;

            foreach (var cell in cells)
            {
                if (IsInBounds(cell) && _cells[cell.x, cell.y] == item)
                {
                    _cells[cell.x, cell.y] = null;
                    found = true;
                }
            }

            if (found)
            {
                item.GridPosition = new Vector2Int(-1, -1);
                OnGridChanged?.Invoke(cells);
            }

            return found;
        }

        /// <summary>
        /// Moves <paramref name="item"/> to a new pivot / rotation in a single atomic
        /// operation. Returns false if the destination is invalid (item stays put).
        /// </summary>
        public bool Move(InventoryItem item, Vector2Int newPivot, ShapeRotation newRotation)
        {
            if (item == null || !item.IsPlaced) return false;

            // Temporarily clear old position so it doesn't block itself
            var oldCells = item.GetOccupiedCells();
            foreach (var cell in oldCells)
                if (IsInBounds(cell) && _cells[cell.x, cell.y] == item)
                    _cells[cell.x, cell.y] = null;

            var oldPos = item.GridPosition;
            var oldRot = item.Rotation;

            item.GridPosition = newPivot;
            item.Rotation     = newRotation;

            if (CanPlace(item, newPivot, newRotation, ignoreItem: null))
            {
                var newCells = item.Data.Shape.GetWorldCells(newPivot, newRotation);
                foreach (var cell in newCells)
                    _cells[cell.x, cell.y] = item;

                var changed = new List<Vector2Int>(oldCells);
                changed.AddRange(newCells);
                OnGridChanged?.Invoke(changed);
                return true;
            }

            // Rollback
            item.GridPosition = oldPos;
            item.Rotation     = oldRot;
            foreach (var cell in oldCells)
                if (IsInBounds(cell))
                    _cells[cell.x, cell.y] = item;

            return false;
        }

        /// <summary>
        /// Unconditionally clears all cells occupied by an item.
        /// Used internally during resize; does NOT update item.GridPosition.
        /// </summary>
        private void ForceRemove(InventoryItem item)
        {
            for (int c = 0; c < _columns; c++)
                for (int r = 0; r < _rows; r++)
                    if (_cells[c, r] == item)
                        _cells[c, r] = null;
        }

        /// <summary>Clears all cells. Does not notify items.</summary>
        public void Clear()
        {
            Array.Clear(_cells, 0, _cells.Length);
            var allCells = new List<Vector2Int>(_columns * _rows);
            for (int c = 0; c < _columns; c++)
                for (int r = 0; r < _rows; r++)
                    allCells.Add(new Vector2Int(c, r));
            OnGridChanged?.Invoke(allCells);
        }

        #endregion

        #region Enumeration

        /// <summary>
        /// Returns all unique items currently placed in the grid
        /// (each item appears once, regardless of how many cells it occupies).
        /// </summary>
        public List<InventoryItem> GetAllItems()
        {
            var seen   = new HashSet<InventoryItem>();
            var result = new List<InventoryItem>();

            for (int c = 0; c < _columns; c++)
            {
                for (int r = 0; r < _rows; r++)
                {
                    var item = _cells[c, r];
                    if (item != null && seen.Add(item))
                        result.Add(item);
                }
            }

            return result;
        }

        #endregion

        #region Debug

        /// <summary>Returns an ASCII art representation of the grid (for debugging).</summary>
        public string ToDebugString()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Grid {_columns}×{_rows}:");
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _columns; c++)
                {
                    var item = _cells[c, r];
                    sb.Append(item == null ? "[ ]" : "[X]");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        #endregion
    }
}
