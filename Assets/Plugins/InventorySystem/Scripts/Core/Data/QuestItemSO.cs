// =============================================================================
// QuestItemSO.cs
// Quest / key item — cannot be dropped, sold, or destroyed.
// =============================================================================

using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(
        fileName = "New Quest Item",
        menuName  = "Inventory System/Items/Quest Item",
        order     = 15)]
    public class QuestItemSO : ItemSO
    {
        #region Quest Data

        [Header("Quest Properties")]
        [Tooltip("Quest ID(s) this item belongs to. Used by the quest system to check objectives.")]
        [SerializeField] private string[] _associatedQuestIDs = {};

        [Tooltip("If true, the item is automatically removed when the associated quest completes.")]
        [SerializeField] private bool _removeOnQuestComplete = false;

        #endregion

        #region Public Properties

        public string[] AssociatedQuestIDs      => _associatedQuestIDs;
        public bool     RemoveOnQuestComplete   => _removeOnQuestComplete;

        #endregion
    }
}
