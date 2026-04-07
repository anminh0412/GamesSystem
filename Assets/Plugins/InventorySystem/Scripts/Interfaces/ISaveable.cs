// =============================================================================
// ISaveable.cs
// Implemented by any system component that persists state.
// =============================================================================

namespace InventorySystem
{
    /// <summary>
    /// Marks a component as participating in the AIS Save/Load cycle.
    /// The <see cref="SaveManager"/> discovers all ISaveable objects and
    /// orchestrates serialisation in a consistent order.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Unique key used as the JSON property name inside the save file.
        /// Must be stable across sessions (don't use object instance IDs).
        /// </summary>
        string SaveKey { get; }

        /// <summary>
        /// Return a JSON-serialisable data object representing current state.
        /// Only plain data (no Unity object references) should be returned.
        /// </summary>
        object CaptureState();

        /// <summary>
        /// Restore state from a previously captured data object.
        /// Called during Load after all objects are initialised.
        /// </summary>
        void RestoreState(object data);
    }
}
