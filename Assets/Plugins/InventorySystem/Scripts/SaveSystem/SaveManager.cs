// =============================================================================
// SaveManager.cs
// Robust JSON-based save/load system with multiple slot support.
// =============================================================================
// Discovers all ISaveable objects via ServiceLocator + scene traversal,
// collects their state into a root SaveFile, and serialises to disk.
//
// Save location: Application.persistentDataPath / AIS_Saves / slot_N.ais
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace InventorySystem
{
    // ── Root save file ────────────────────────────────────────────────────────

    [Serializable]
    public class SaveFile
    {
        public int                        SlotIndex;
        public string                     Timestamp;
        public string                     SystemVersion;
        public Dictionary<string, string> Data = new Dictionary<string, string>();
    }

    // =========================================================================

    /// <summary>
    /// Static save/load facade.  Individual systems implement <see cref="ISaveable"/>
    /// and are discovered automatically through the <see cref="SaveableRegistry"/>.
    /// </summary>
    public static class SaveManager
    {
        #region Path Helpers

        private static string SaveFolder =>
            Path.Combine(Application.persistentDataPath, InventorySystemConstants.SAVE_FOLDER_NAME);

        private static string SlotPath(int slot) =>
            Path.Combine(SaveFolder, $"slot_{slot}{InventorySystemConstants.SAVE_FILE_EXTENSION}");

        #endregion

        #region Save

        /// <summary>
        /// Saves all registered ISaveable systems to the given slot.
        /// Returns <see cref="SaveResult.Success"/> on success.
        /// </summary>
        public static SaveResult SaveAll(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= InventorySystemConstants.MAX_SAVE_SLOTS)
            {
                Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} SaveAll: slot {slotIndex} out of range.");
                return SaveResult.SlotOutOfRange;
            }

            try
            {
                EnsureSaveFolderExists();

                var file = new SaveFile
                {
                    SlotIndex     = slotIndex,
                    Timestamp     = DateTime.UtcNow.ToString("o"),
                    SystemVersion = InventorySystemConstants.SYSTEM_VERSION
                };

                foreach (var saveable in SaveableRegistry.GetAll())
                {
                    try
                    {
                        var state = saveable.CaptureState();
                        file.Data[saveable.SaveKey] = JsonUtility.ToJson(state);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} CaptureState failed for '{saveable.SaveKey}': {ex}");
                    }
                }

                string json = JsonUtility.ToJson(new SaveFileWrapper { Json = JsonUtility.ToJson(file) }, prettyPrint: false);
                // Use a simple wrapper because JsonUtility can't serialize Dictionary directly
                string rawData = SerializeSaveFile(file);
                File.WriteAllText(SlotPath(slotIndex), rawData);

                PlayerPrefs.SetInt(InventorySystemConstants.PREFS_LAST_SLOT, slotIndex);
                PlayerPrefs.Save();

                InventoryEventBus.Publish(new SaveCompletedEvent { SlotIndex = slotIndex, Success = true });
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Saved to slot {slotIndex}: {SlotPath(slotIndex)}");
                return SaveResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} Save failed: {ex}");
                InventoryEventBus.Publish(new SaveCompletedEvent { SlotIndex = slotIndex, Success = false });
                return SaveResult.Failed;
            }
        }

        #endregion

        #region Load

        /// <summary>
        /// Loads all registered ISaveable systems from the given slot.
        /// Returns <see cref="SaveResult.Success"/> on success.
        /// </summary>
        public static SaveResult LoadAll(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= InventorySystemConstants.MAX_SAVE_SLOTS)
                return SaveResult.SlotOutOfRange;

            string path = SlotPath(slotIndex);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} No save file at slot {slotIndex}: {path}");
                return SaveResult.Failed;
            }

            try
            {
                string rawData = File.ReadAllText(path);
                var file       = DeserialiseSaveFile(rawData);
                if (file == null)
                {
                    Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} Failed to parse save file at slot {slotIndex}.");
                    return SaveResult.Failed;
                }

                foreach (var saveable in SaveableRegistry.GetAll())
                {
                    if (!file.Data.TryGetValue(saveable.SaveKey, out string json)) continue;
                    try
                    {
                        // Each ISaveable declares what type it expects via RestoreState
                        saveable.RestoreState(json); // Pass raw JSON; each saveable parses it
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} RestoreState failed for '{saveable.SaveKey}': {ex}");
                    }
                }

                InventoryEventBus.Publish(new LoadCompletedEvent { SlotIndex = slotIndex, Success = true });
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Loaded from slot {slotIndex}.");
                return SaveResult.Success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} Load failed: {ex}");
                InventoryEventBus.Publish(new LoadCompletedEvent { SlotIndex = slotIndex, Success = false });
                return SaveResult.Failed;
            }
        }

        #endregion

        #region Delete & Query

        public static void DeleteSlot(int slotIndex)
        {
            string path = SlotPath(slotIndex);
            if (File.Exists(path)) File.Delete(path);
        }

        public static bool SlotExists(int slotIndex)
            => File.Exists(SlotPath(slotIndex));

        public static int LastSavedSlot
            => PlayerPrefs.GetInt(InventorySystemConstants.PREFS_LAST_SLOT, -1);

        /// <summary>Returns basic metadata from a save slot without fully loading it.</summary>
        public static SaveSlotInfo GetSlotInfo(int slotIndex)
        {
            if (!SlotExists(slotIndex)) return null;
            try
            {
                string rawData = File.ReadAllText(SlotPath(slotIndex));
                var file       = DeserialiseSaveFile(rawData);
                return file == null ? null : new SaveSlotInfo
                {
                    SlotIndex  = file.SlotIndex,
                    Timestamp  = file.Timestamp,
                    Version    = file.SystemVersion
                };
            }
            catch { return null; }
        }

        #endregion

        #region Serialisation Helpers

        private static void EnsureSaveFolderExists()
        {
            if (!Directory.Exists(SaveFolder))
                Directory.CreateDirectory(SaveFolder);
        }

        // Because JsonUtility can't handle Dictionary<string,string> directly,
        // we use a simple key-value list wrapper.
        private static string SerializeSaveFile(SaveFile file)
        {
            var wrapper = new SaveFileJsonWrapper
            {
                SlotIndex     = file.SlotIndex,
                Timestamp     = file.Timestamp,
                SystemVersion = file.SystemVersion,
                Keys          = new List<string>(),
                Values        = new List<string>()
            };
            foreach (var kvp in file.Data)
            {
                wrapper.Keys.Add(kvp.Key);
                wrapper.Values.Add(kvp.Value);
            }
            return JsonUtility.ToJson(wrapper, prettyPrint: true);
        }

        private static SaveFile DeserialiseSaveFile(string raw)
        {
            var wrapper = JsonUtility.FromJson<SaveFileJsonWrapper>(raw);
            if (wrapper == null) return null;

            var file = new SaveFile
            {
                SlotIndex     = wrapper.SlotIndex,
                Timestamp     = wrapper.Timestamp,
                SystemVersion = wrapper.SystemVersion
            };
            for (int i = 0; i < wrapper.Keys.Count && i < wrapper.Values.Count; i++)
                file.Data[wrapper.Keys[i]] = wrapper.Values[i];
            return file;
        }

        #endregion
    }

    // ── JSON-serialisable wrappers ─────────────────────────────────────────────

    [Serializable]
    internal class SaveFileWrapper { public string Json; }

    [Serializable]
    internal class SaveFileJsonWrapper
    {
        public int          SlotIndex;
        public string       Timestamp;
        public string       SystemVersion;
        public List<string> Keys   = new List<string>();
        public List<string> Values = new List<string>();
    }

    // ── Slot metadata ─────────────────────────────────────────────────────────

    public class SaveSlotInfo
    {
        public int    SlotIndex;
        public string Timestamp;
        public string Version;
    }

    // =========================================================================
    // SaveableRegistry — global registry of ISaveable implementors
    // =========================================================================

    /// <summary>
    /// ISaveable objects self-register here on Awake and deregister on OnDestroy.
    /// The SaveManager uses this list to enumerate all saveable state.
    /// </summary>
    public static class SaveableRegistry
    {
        private static readonly List<ISaveable> _saveables = new List<ISaveable>();

        public static void Register(ISaveable s)
        {
            if (!_saveables.Contains(s)) _saveables.Add(s);
        }

        public static void Unregister(ISaveable s)
        {
            _saveables.Remove(s);
        }

        public static IReadOnlyList<ISaveable> GetAll() => _saveables;
    }
}
