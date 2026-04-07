// =============================================================================
// InventoryInputHandler.cs
// Bridges Unity's new Input System to inventory actions.
// =============================================================================
// Attach one InventoryInputHandler per player (PlayerInput component on the
// same GameObject). The handler routes inputs to InventoryManager and UIManager.
// Supports Keyboard+Mouse, Gamepad, and Touch (mobile).
// =============================================================================

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InventorySystem
{
    /// <summary>
    /// Per-player input handler for all inventory-related actions.
    /// Requires a <see cref="PlayerInput"/> component on the same GameObject.
    /// Action Map name: "Inventory" (configure in Input Actions asset).
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class InventoryInputHandler : MonoBehaviour
    {
        #region Inspector

        [Header("Player")]
        [Tooltip("Index of the player this handler belongs to (0 = P1, 1 = P2, …).")]
        [SerializeField] [Range(0, 3)] private int _playerIndex = 0;

        [Header("Toggle")]
        [Tooltip("Should inventory open/close be toggled (one press) or held?")]
        [SerializeField] private bool _toggleMode = true;

        #endregion

        #region Events (consumed by UI layer)

        public event Action          OnInventoryToggled;
        public event Action<int>     OnHotbarSlotSelected;   // arg = slot index 0-8
        public event Action          OnRotateItem;
        public event Action          OnSplitStack;
        public event Action<Vector2> OnNavigate;
        public event Action          OnConfirm;
        public event Action          OnCancel;

        #endregion

        #region State

        private PlayerInput _playerInput;
        private InputActionMap _inventoryMap;
        private bool _isOpen;
        public InputDeviceType CurrentDevice { get; private set; } = InputDeviceType.KeyboardMouse;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _playerInput.notificationBehavior = PlayerNotifications.InvokeUnityEvents;

            _inventoryMap = _playerInput.actions.FindActionMap(
                InventorySystemConstants.ACTION_MAP_INVENTORY, throwIfNotFound: false);

            if (_inventoryMap == null)
            {
                Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} Action Map '{InventorySystemConstants.ACTION_MAP_INVENTORY}' not found. " +
                                 "Make sure your Input Actions asset has an 'Inventory' map.");
                return;
            }

            BindActions();
        }

        private void OnDestroy()
        {
            UnbindActions();
        }

        private void OnEnable()
        {
            _inventoryMap?.Enable();
        }

        private void OnDisable()
        {
            _inventoryMap?.Disable();
        }

        #endregion

        #region Action Binding

        private void BindActions()
        {
            BindAction(InventorySystemConstants.ACTION_OPEN_INVENTORY,  OnOpenInventoryPerformed);
            BindAction(InventorySystemConstants.ACTION_ROTATE_ITEM,     OnRotatePerformed);
            BindAction(InventorySystemConstants.ACTION_SPLIT_STACK,     OnSplitPerformed);
            BindAction(InventorySystemConstants.ACTION_NAVIGATE,        OnNavigatePerformed);
            BindAction(InventorySystemConstants.ACTION_CONFIRM,         OnConfirmPerformed);
            BindAction(InventorySystemConstants.ACTION_CANCEL,          OnCancelPerformed);

            // Hotbar slots 1-9
            for (int i = 0; i < InventorySystemConstants.MAX_HOTBAR_SLOTS; i++)
            {
                int slotIndex = i; // capture for lambda
                string actionName = $"Hotbar{i + 1}";
                var action = _inventoryMap.FindAction(actionName, throwIfNotFound: false);
                if (action != null)
                    action.performed += _ => HandleHotbar(slotIndex);
            }
        }

        private void UnbindActions()
        {
            if (_inventoryMap == null) return;
            UnbindAction(InventorySystemConstants.ACTION_OPEN_INVENTORY,  OnOpenInventoryPerformed);
            UnbindAction(InventorySystemConstants.ACTION_ROTATE_ITEM,     OnRotatePerformed);
            UnbindAction(InventorySystemConstants.ACTION_SPLIT_STACK,     OnSplitPerformed);
            UnbindAction(InventorySystemConstants.ACTION_NAVIGATE,        OnNavigatePerformed);
            UnbindAction(InventorySystemConstants.ACTION_CONFIRM,         OnConfirmPerformed);
            UnbindAction(InventorySystemConstants.ACTION_CANCEL,          OnCancelPerformed);
        }

        private void BindAction(string actionName, Action<InputAction.CallbackContext> handler)
        {
            var action = _inventoryMap?.FindAction(actionName, throwIfNotFound: false);
            if (action != null) action.performed += handler;
        }

        private void UnbindAction(string actionName, Action<InputAction.CallbackContext> handler)
        {
            var action = _inventoryMap?.FindAction(actionName, throwIfNotFound: false);
            if (action != null) action.performed -= handler;
        }

        #endregion

        #region Handlers

        private void OnOpenInventoryPerformed(InputAction.CallbackContext ctx)
        {
            UpdateDeviceType(ctx);
            _isOpen = !_isOpen;
            OnInventoryToggled?.Invoke();
        }

        private void OnRotatePerformed(InputAction.CallbackContext ctx)
        {
            UpdateDeviceType(ctx);
            OnRotateItem?.Invoke();
        }

        private void OnSplitPerformed(InputAction.CallbackContext ctx)
        {
            UpdateDeviceType(ctx);
            OnSplitStack?.Invoke();
        }

        private void OnNavigatePerformed(InputAction.CallbackContext ctx)
        {
            UpdateDeviceType(ctx);
            OnNavigate?.Invoke(ctx.ReadValue<Vector2>());
        }

        private void OnConfirmPerformed(InputAction.CallbackContext ctx)
        {
            UpdateDeviceType(ctx);
            OnConfirm?.Invoke();
        }

        private void OnCancelPerformed(InputAction.CallbackContext ctx)
        {
            UpdateDeviceType(ctx);
            OnCancel?.Invoke();
        }

        private void HandleHotbar(int slotIndex)
        {
            var invManager = ServiceLocator.TryGet<InventoryManager>();
            if (invManager == null) return;

            var hotbar = invManager.GetHotbar(_playerIndex);
            if (hotbar == null) return;

            hotbar.ActiveSlot = slotIndex;
            OnHotbarSlotSelected?.Invoke(slotIndex);
        }

        private void UpdateDeviceType(InputAction.CallbackContext ctx)
        {
            if (ctx.control?.device is Gamepad)
                CurrentDevice = InputDeviceType.Gamepad;
            else if (ctx.control?.device is Touchscreen)
                CurrentDevice = InputDeviceType.Touch;
            else
                CurrentDevice = InputDeviceType.KeyboardMouse;
        }

        #endregion

        #region Mobile Touch Helpers

        /// <summary>
        /// Processes a long-press gesture to open an item context menu.
        /// Call from your UI touch event system when a long-press is detected on a slot.
        /// </summary>
        public void HandleLongPress(InventoryItem item)
        {
            if (item == null) return;
            // Raise a generic event — the UI layer handles showing the context menu
            InventoryEventBus.Publish(new ItemUsedEvent { Item = item, PlayerIndex = _playerIndex });
        }

        #endregion
    }
}
