// =============================================================================
// ItemShape.cs
// Tetris / Resident-Evil-4-style item shape data and rotation logic.
// =============================================================================
// An ItemShape is a list of (col, row) offsets relative to the item's pivot
// cell (top-left of its bounding box). Rotation is applied by transforming
// the offset set around the bounding-box centre.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// A single cell offset within a shape definition.
    /// (0,0) is the pivot — the top-left of the bounding box.
    /// </summary>
    [Serializable]
    public struct ShapeCell : IEquatable<ShapeCell>
    {
        [Tooltip("Column offset from shape pivot (left = 0).")]
        public int X; // column

        [Tooltip("Row offset from shape pivot (top = 0).")]
        public int Y; // row

        public ShapeCell(int x, int y) { X = x; Y = y; }

        public bool Equals(ShapeCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is ShapeCell c && Equals(c);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X},{Y})";

        public static bool operator ==(ShapeCell a, ShapeCell b) => a.Equals(b);
        public static bool operator !=(ShapeCell a, ShapeCell b) => !a.Equals(b);
    }

    // =========================================================================

    /// <summary>
    /// Immutable description of which cells an item occupies on the grid,
    /// along with helpers to rotate the shape and query its bounding box.
    /// </summary>
    [Serializable]
    public class ItemShape
    {
        #region Serialised Fields

        [SerializeField]
        [Tooltip("Cell offsets that define the item's footprint. Each entry is (col, row) from pivot.")]
        private List<ShapeCell> _cells = new List<ShapeCell> { new ShapeCell(0, 0) };

        #endregion

        #region Properties

        /// <summary>Read-only view of the raw (non-rotated) cell list.</summary>
        public IReadOnlyList<ShapeCell> Cells => _cells;

        /// <summary>Total number of cells this item occupies.</summary>
        public int CellCount => _cells.Count;

        #endregion

        #region Constructors

        /// <summary>Creates a 1×1 single-cell shape.</summary>
        public ItemShape() { }

        /// <summary>Creates a shape from an explicit list of cells.</summary>
        public ItemShape(IEnumerable<ShapeCell> cells)
        {
            _cells = new List<ShapeCell>(cells);
        }

        /// <summary>Creates a solid W×H rectangular shape.</summary>
        public static ItemShape CreateRectangle(int width, int height)
        {
            var cells = new List<ShapeCell>(width * height);
            for (int row = 0; row < height; row++)
                for (int col = 0; col < width; col++)
                    cells.Add(new ShapeCell(col, row));
            return new ItemShape(cells);
        }

        #endregion

        #region Rotation

        /// <summary>
        /// Returns a new set of cells representing this shape rotated clockwise
        /// by <paramref name="rotation"/> steps of 90°, normalised so that the
        /// minimum X and Y are both 0 (pivot at top-left).
        /// </summary>
        public List<ShapeCell> GetRotatedCells(ShapeRotation rotation)
        {
            var rotated = new List<ShapeCell>(_cells.Count);

            foreach (var cell in _cells)
            {
                int rx, ry;
                switch (rotation)
                {
                    case ShapeRotation.Rot0:
                        rx = cell.X; ry = cell.Y;
                        break;
                    case ShapeRotation.Rot90:
                        // CW 90°: (x,y) → (y, -x)  — normalised afterwards
                        rx = cell.Y; ry = -cell.X;
                        break;
                    case ShapeRotation.Rot180:
                        rx = -cell.X; ry = -cell.Y;
                        break;
                    case ShapeRotation.Rot270:
                        // CW 270° = CCW 90°: (x,y) → (-y, x)
                        rx = -cell.Y; ry = cell.X;
                        break;
                    default:
                        rx = cell.X; ry = cell.Y;
                        break;
                }
                rotated.Add(new ShapeCell(rx, ry));
            }

            NormaliseToOrigin(rotated);
            return rotated;
        }

        /// <summary>Shifts all cells so that minX = 0 and minY = 0.</summary>
        private static void NormaliseToOrigin(List<ShapeCell> cells)
        {
            if (cells.Count == 0) return;

            int minX = int.MaxValue, minY = int.MaxValue;
            foreach (var c in cells)
            {
                if (c.X < minX) minX = c.X;
                if (c.Y < minY) minY = c.Y;
            }

            for (int i = 0; i < cells.Count; i++)
                cells[i] = new ShapeCell(cells[i].X - minX, cells[i].Y - minY);
        }

        #endregion

        #region Bounding Box

        /// <summary>
        /// Returns the width and height of the bounding box for a given rotation.
        /// </summary>
        public Vector2Int GetBounds(ShapeRotation rotation)
        {
            var cells = GetRotatedCells(rotation);
            int maxX = 0, maxY = 0;
            foreach (var c in cells)
            {
                if (c.X > maxX) maxX = c.X;
                if (c.Y > maxY) maxY = c.Y;
            }
            return new Vector2Int(maxX + 1, maxY + 1); // +1: cells are 0-indexed
        }

        /// <summary>Width of the shape in its default (Rot0) orientation.</summary>
        public int DefaultWidth  => GetBounds(ShapeRotation.Rot0).x;

        /// <summary>Height of the shape in its default (Rot0) orientation.</summary>
        public int DefaultHeight => GetBounds(ShapeRotation.Rot0).y;

        #endregion

        #region Utility

        /// <summary>
        /// Returns world-grid positions for every cell of this shape placed at
        /// <paramref name="pivot"/> with the given rotation.
        /// </summary>
        public List<Vector2Int> GetWorldCells(Vector2Int pivot, ShapeRotation rotation)
        {
            var rotatedCells = GetRotatedCells(rotation);
            var result = new List<Vector2Int>(rotatedCells.Count);
            foreach (var c in rotatedCells)
                result.Add(new Vector2Int(pivot.x + c.X, pivot.y + c.Y));
            return result;
        }

        public override string ToString()
        {
            return $"ItemShape[{_cells.Count} cells, {DefaultWidth}×{DefaultHeight}]";
        }

        #endregion
    }
}
