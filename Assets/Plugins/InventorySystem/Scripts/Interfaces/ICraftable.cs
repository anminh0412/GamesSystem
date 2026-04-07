// =============================================================================
// ICraftable.cs
// Implemented by crafting stations that can process recipes.
// =============================================================================

using System.Collections.Generic;

namespace InventorySystem
{
    /// <summary>
    /// Contract for any object that can execute crafting recipes
    /// (workbench, forge, alchemy table, etc.).
    /// </summary>
    public interface ICraftable
    {
        /// <summary>Station type tag — recipes may be restricted to specific station types.</summary>
        string StationType { get; }

        /// <summary>Recipes this station can handle (null = all recipes).</summary>
        IReadOnlyList<CraftingRecipeSO> SupportedRecipes { get; }

        /// <summary>Enqueue a crafting job. Returns true if accepted.</summary>
        bool StartCraft(CraftingRecipeSO recipe, IInventory sourceInventory);

        /// <summary>Cancel the currently queued or in-progress craft job.</summary>
        void CancelCraft();

        /// <summary>True while a craft is in progress or queued.</summary>
        bool IsCrafting { get; }
    }
}
