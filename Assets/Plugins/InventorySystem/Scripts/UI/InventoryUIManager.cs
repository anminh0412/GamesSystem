// =============================================================================
// InventoryUIManager.cs
// Opens and closes inventory panels; bridges InventoryInputHandler → UI.
// =============================================================================
// One instance per player. Listens to InventoryInputHandler events and shows
// the correct panel(s). Also handles rotate-during-drag input.
// =============================================================================

using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Coordinates all inventory UI panels for one player.
    /// Attach alongside InventoryInputHandler on the player GameObject.
    /// </summary>
    public class InventoryUIManager : MonoBehaviour
    {
        #region Inspector

        [Header("Player")]
        [SerializeField] [Range(0, 3)] private int _playerIndex = 0;

        [Header("UI Panels")]
        [Tooltip("The main bag InventoryUI panel for this player.")]
        [SerializeField] private InventoryUI _bagUI;

        [Tooltip("The equipment InventoryUI panel (can be null if not used).")]
        [SerializeField] private InventoryUI _equipUI;

        [Tooltip("Root panel GameObject shown/hidden on toggle.")]
        [SerializeField] private GameObject _inventoryRoot;

        [Header("Animation (optional)")]
        [SerializeField] private Animator _panelAnimator;
        [SerializeField] private string   _openTrigger  = "Open";
        [SerializeField] private string   _closeTrigger = "Close";

        #endregion

        #region State

        private bool _isOpen;
        private InventoryInputHandler _input;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _input = GetComponent<InventoryInputHandler>();
            if (_input != null)
            {
                _input.OnInventoryToggled  += ToggleInventory;
                _input.OnRotateItem        += OnRotateItem;
            }

            // Bind UIs to data
            var mgr = ServiceLocator.TryGet<InventoryManager>();
            if (mgr != null)
            {
                var bag   = mgr.GetPlayerInventory(_playerIndex);
                var equip = mgr.GetEquipmentInventory(_playerIndex);

                _bagUI?.Bind(bag);
                _equipUI?.Bind(equip);
            }

            // Start closed
            SetOpen(false, instant: true);
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.OnInventoryToggled -= ToggleInventory;
                _input.OnRotateItem       -= OnRotateItem;
            }
        }

        #endregion

        #region Open / Close

        public void ToggleInventory() => SetOpen(!_isOpen);

        public void OpenInventory()   => SetOpen(true);
        public void CloseInventory()  => SetOpen(false);

        public bool IsOpen => _isOpen;

        private void SetOpen(bool open, bool instant = false)
        {
            _isOpen = open;

            if (_inventoryRoot)
                _inventoryRoot.SetActive(open);

            if (_panelAnimator && !instant)
                _panelAnimator.SetTrigger(open ? _openTrigger : _closeTrigger);

            if (open)
                InventoryEventBus.Publish(new InventoryOpenedEvent
                    { Inventory = _bagUI?.BoundInventory, PlayerIndex = _playerIndex });
            else
            {
                InventoryEventBus.Publish(new InventoryClosedEvent
                    { Inventory = _bagUI?.BoundInventory, PlayerIndex = _playerIndex });
                TooltipUI.Instance?.Hide(instant: true);
                ContextMenuUI.Instance?.HideInstant();
                SplitStackUI.Instance?.HideInstant();
            }
        }

        #endregion

        #region Input Forwarding

        private void OnRotateItem()
        {
            // Forward rotate to whichever InventoryUI is currently active
            _bagUI?.RotateDragging();
            _equipUI?.RotateDragging();
        }

        #endregion

        #region External Panel Support

        /// <summary>
        /// Opens a secondary panel (chest, vendor) alongside the player bag.
        /// </summary>
        public void OpenSecondary(InventoryUI secondaryUI, Inventory secondaryInventory)
        {
            secondaryUI?.Bind(secondaryInventory);
            secondaryUI?.gameObject.SetActive(true);
            OpenInventory();
        }

        public void CloseSecondary(InventoryUI secondaryUI)
        {
            secondaryUI?.gameObject.SetActive(false);
        }

        #endregion
    }
}
