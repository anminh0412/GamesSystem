// =============================================================================
// ItemSOEditor.cs
// Custom Inspector for ItemSO — adds shape preview and ID generator button.
// =============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace InventorySystem
{
    [CustomEditor(typeof(ItemSO), editorForChildClasses: true)]
    public class ItemSOEditor : Editor
    {
        private const float CELL_PX = 20f;
        private const float CELL_GAP = 2f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var item = (ItemSO)target;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Shape Preview", EditorStyles.boldLabel);
            DrawShapePreview(item.Shape, ShapeRotation.Rot0);

            EditorGUILayout.Space(4);

            // ID generation
            if (string.IsNullOrEmpty(item.ItemID))
            {
                EditorGUILayout.HelpBox("This item has no ID. Click below to generate one.", MessageType.Warning);
            }

            if (GUILayout.Button("Generate New Item ID"))
            {
                serializedObject.FindProperty("_itemID").stringValue = System.Guid.NewGuid().ToString("N");
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawShapePreview(ItemShape shape, ShapeRotation rotation)
        {
            if (shape == null) return;

            var cells  = shape.GetRotatedCells(rotation);
            var bounds = shape.GetBounds(rotation);
            int cols   = bounds.x;
            int rows   = bounds.y;

            float totalW = cols * (CELL_PX + CELL_GAP) - CELL_GAP;
            float totalH = rows * (CELL_PX + CELL_GAP) - CELL_GAP;

            Rect baseRect = GUILayoutUtility.GetRect(totalW, totalH);

            // Draw background grid
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Rect cellRect = new Rect(
                        baseRect.x + c * (CELL_PX + CELL_GAP),
                        baseRect.y + r * (CELL_PX + CELL_GAP),
                        CELL_PX, CELL_PX);

                    // Check if this cell is occupied
                    bool occupied = false;
                    foreach (var sc in cells)
                    {
                        if (sc.X == c && sc.Y == r) { occupied = true; break; }
                    }

                    EditorGUI.DrawRect(cellRect, occupied ? new Color(0.2f, 0.6f, 1f, 0.9f)
                                                          : new Color(0.15f, 0.15f, 0.15f, 0.5f));
                }
            }
        }
    }
}
#endif
