// =============================================================================
// ItemDatabaseEditor.cs
// Custom Inspector for ItemDatabaseSO with auto-populate and CSV export.
// =============================================================================

#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace InventorySystem
{
    [CustomEditor(typeof(ItemDatabaseSO))]
    public class ItemDatabaseEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            var db = (ItemDatabaseSO)target;

            // Auto-populate button
            if (GUILayout.Button("Auto-Populate From Project", GUILayout.Height(28)))
            {
                db.AutoPopulate();
            }

            EditorGUILayout.Space(4);

            // Export CSV
            if (GUILayout.Button("Export Items to CSV", GUILayout.Height(28)))
            {
                ExportToCSV(db);
            }

            // Export JSON
            if (GUILayout.Button("Export Items to JSON", GUILayout.Height(28)))
            {
                ExportToJSON(db);
            }

            // Stats
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox($"Total items registered: {db.Count}", MessageType.Info);
        }

        private static void ExportToCSV(ItemDatabaseSO db)
        {
            string path = EditorUtility.SaveFilePanel("Export Item Database", "", "ItemDatabase", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("ItemID,Name,Category,Rarity,Weight,BaseValue,Stackable,MaxStack,ShapeW,ShapeH");

            foreach (var item in db.AllItems)
            {
                if (item == null) continue;
                sb.AppendLine($"{item.ItemID},{EscapeCSV(item.ItemName)},{item.Category},{item.Rarity}," +
                              $"{item.Weight},{item.BaseValue},{item.IsStackable},{item.MaxStackSize}," +
                              $"{item.Shape.DefaultWidth},{item.Shape.DefaultHeight}");
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Exported {db.Count} items to {path}");
            EditorUtility.RevealInFinder(path);
        }

        private static void ExportToJSON(ItemDatabaseSO db)
        {
            string path = EditorUtility.SaveFilePanel("Export Item Database", "", "ItemDatabase", "json");
            if (string.IsNullOrEmpty(path)) return;

            var sb = new StringBuilder();
            sb.AppendLine("[");

            var items = db.AllItems;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null) continue;

                sb.AppendLine("  {");
                sb.AppendLine($"    \"itemID\": \"{item.ItemID}\",");
                sb.AppendLine($"    \"name\": \"{EscapeJson(item.ItemName)}\",");
                sb.AppendLine($"    \"description\": \"{EscapeJson(item.Description)}\",");
                sb.AppendLine($"    \"category\": \"{item.Category}\",");
                sb.AppendLine($"    \"rarity\": \"{item.Rarity}\",");
                sb.AppendLine($"    \"weight\": {item.Weight},");
                sb.AppendLine($"    \"baseValue\": {item.BaseValue},");
                sb.AppendLine($"    \"stackable\": {item.IsStackable.ToString().ToLower()},");
                sb.AppendLine($"    \"maxStack\": {item.MaxStackSize},");
                sb.AppendLine($"    \"shapeWidth\": {item.Shape.DefaultWidth},");
                sb.AppendLine($"    \"shapeHeight\": {item.Shape.DefaultHeight}");
                sb.Append("  }");
                sb.AppendLine(i < items.Count - 1 ? "," : "");
            }

            sb.AppendLine("]");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Exported {db.Count} items to {path}");
            EditorUtility.RevealInFinder(path);
        }

        private static string EscapeCSV(string s) => s.Contains(',') ? $"\"{s}\"" : s;
        private static string EscapeJson(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }
}
#endif
