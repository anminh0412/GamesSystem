// =============================================================================
// ResourceItemSO.cs
// Crafting resource / material item — ore, wood, cloth, etc.
// =============================================================================

using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(
        fileName = "New Resource",
        menuName  = "Inventory System/Items/Resource",
        order     = 14)]
    public class ResourceItemSO : ItemSO
    {
        #region Resource Data

        [Header("Resource Properties")]
        [Tooltip("Sub-category tag used by crafting recipes to filter material types.")]
        [SerializeField] private string _resourceTag = "Metal";

        [Tooltip("Tier / quality of this resource (1 = lowest). Recipes may require a minimum tier.")]
        [Min(1)]
        [SerializeField] private int _tier = 1;

        #endregion

        #region Public Properties

        public string ResourceTag => _resourceTag;
        public int    Tier        => _tier;

        #endregion
    }
}
