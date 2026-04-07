// =============================================================================
// WorldItem.cs
// MonoBehaviour placed on a dropped item in the scene.
// Handles pickup interaction for both 2D and 3D projects.
// =============================================================================

using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem
{
    /// <summary>
    /// Represents a physical item lying in the world (dropped loot, spawned pickup).
    /// Attach to a prefab along with a Collider/Collider2D trigger.
    /// </summary>
    public class WorldItem : MonoBehaviour
    {
        #region Inspector

        [Header("Item")]
        [SerializeField] private ItemSO _itemData;
        [SerializeField] [Min(1)] private int _stackCount = 1;

        [Header("Auto-Pickup")]
        [Tooltip("Automatically picked up when the player enters the trigger.")]
        [SerializeField] private bool _autoPickup = false;

        [Tooltip("Player tag used for pickup detection.")]
        [SerializeField] private string _playerTag = "Player";

        [Header("Interaction")]
        [Tooltip("Text shown in the interaction prompt (if your game has one).")]
        [SerializeField] private string _interactPrompt = "Pick Up";

        [Header("Events")]
        public UnityEvent<InventoryItem> OnPickedUp;

        #endregion

        #region Properties

        public ItemSO ItemData   => _itemData;
        public int    StackCount => _stackCount;

        #endregion

        #region Initialisation

        /// <summary>
        /// Initialises this world item with runtime data (called when spawning a drop).
        /// </summary>
        public void Initialise(ItemSO data, int stack)
        {
            _itemData   = data;
            _stackCount = Mathf.Max(1, stack);
            RefreshVisuals();
        }

        private void Start() => RefreshVisuals();

        private void RefreshVisuals()
        {
            // If the item has a 3D world prefab, you might instantiate it as a child here.
            // For simplicity this base implementation just sets the GameObject name.
            if (_itemData != null)
                gameObject.name = $"WorldItem_{_itemData.ItemName}_x{_stackCount}";
        }

        #endregion

        #region Pickup

        // 3D trigger
        private void OnTriggerEnter(Collider other)
        {
            if (!_autoPickup) return;
            if (!other.CompareTag(_playerTag)) return;
            TryPickup(other.gameObject);
        }

        // 2D trigger
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_autoPickup) return;
            if (!other.CompareTag(_playerTag)) return;
            TryPickup(other.gameObject);
        }

        /// <summary>
        /// Attempts to add this item to the player's inventory.
        /// Call this from an interaction system when the player presses "pick up".
        /// </summary>
        public void TryPickup(GameObject player)
        {
            if (_itemData == null) return;

            var invManager = ServiceLocator.TryGet<InventoryManager>();
            if (invManager == null) return;

            // Determine player index (works with multi-player via PlayerIndex component)
            int playerIndex = 0;
            var idxComp     = player.GetComponent<PlayerIndexComponent>();
            if (idxComp != null) playerIndex = idxComp.PlayerIndex;

            var bag      = invManager.GetPlayerInventory(playerIndex);
            int leftover = bag.AddItem(_itemData, _stackCount);

            if (leftover < _stackCount)
            {
                // At least some were picked up
                int pickedUp = _stackCount - leftover;
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Player {playerIndex} picked up {pickedUp}× {_itemData.ItemName}.");

                // Fire event
                var item = new InventoryItem(_itemData, pickedUp);
                OnPickedUp?.Invoke(item);

                if (leftover <= 0)
                    Destroy(gameObject);
                else
                    _stackCount = leftover; // partial pickup — leave remainder in world
            }
            else
            {
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Inventory full — could not pick up {_itemData.ItemName}.");
            }
        }

        #endregion
    }

    // ── Helper component to track player index on the player GameObject ───────

    /// <summary>
    /// Attach this to each player GameObject so WorldItem can determine the
    /// correct inventory to deposit into during split-screen play.
    /// </summary>
    public class PlayerIndexComponent : MonoBehaviour
    {
        [SerializeField] [Range(0, 3)] private int _playerIndex = 0;
        public int PlayerIndex => _playerIndex;
    }
}
