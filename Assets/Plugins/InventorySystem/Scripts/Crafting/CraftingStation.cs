// =============================================================================
// CraftingStation.cs
// MonoBehaviour that implements ICraftable for a physical crafting station.
// =============================================================================
// Attach to any world object (Workbench, Forge, Alchemy Table).
// Supports a crafting queue, timed crafting, and UI integration.
// =============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace InventorySystem
{
    // ── Craft Job ─────────────────────────────────────────────────────────────

    /// <summary>Represents one entry in the crafting queue.</summary>
    public class CraftJob
    {
        public CraftingRecipeSO Recipe;
        public IInventory       SourceInventory;
        public float            TimeRemaining;
        public CraftJobStatus   Status;
        public float            Progress => Recipe.CraftTime > 0f
            ? 1f - TimeRemaining / Recipe.CraftTime
            : 1f;
    }

    // =========================================================================

    /// <summary>
    /// Crafting station component — manages a recipe queue and coroutine-based
    /// timed crafting. Raises events on completion for UI and audio feedback.
    /// </summary>
    public class CraftingStation : MonoBehaviour, ICraftable
    {
        #region Inspector

        [Header("Station")]
        [Tooltip("Station type tag matched against CraftingRecipeSO.RequiredStationType.")]
        [SerializeField] private string _stationType = "Workbench";

        [Tooltip("Leave empty to allow all recipes. Otherwise only listed recipes are accepted.")]
        [SerializeField] private List<CraftingRecipeSO> _supportedRecipes = new List<CraftingRecipeSO>();

        [Header("Queue")]
        [Tooltip("Max simultaneous jobs in the queue.")]
        [SerializeField] [Range(1, 10)] private int _maxQueueSize = 3;

        [Header("Events")]
        public UnityEvent<CraftJob> OnCraftStartedEvent;
        public UnityEvent<CraftJob> OnCraftCompletedEvent;
        public UnityEvent<CraftJob> OnCraftFailedEvent;
        public UnityEvent<float>    OnCraftProgressEvent; // 0–1

        #endregion

        #region State

        private readonly Queue<CraftJob> _queue = new Queue<CraftJob>();
        private CraftJob _currentJob;
        private Coroutine _craftCoroutine;

        #endregion

        #region ICraftable

        public string StationType => _stationType;
        public IReadOnlyList<CraftingRecipeSO> SupportedRecipes => _supportedRecipes;
        public bool IsCrafting => _currentJob != null;

        public bool StartCraft(CraftingRecipeSO recipe, IInventory sourceInventory)
        {
            if (recipe == null || sourceInventory == null) return false;

            // Validate station type
            if (!string.IsNullOrEmpty(recipe.RequiredStationType) &&
                recipe.RequiredStationType != _stationType)
            {
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Recipe '{recipe.RecipeName}' requires station '{recipe.RequiredStationType}', this is '{_stationType}'.");
                return false;
            }

            if (_queue.Count >= _maxQueueSize)
            {
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} Craft queue full at station '{_stationType}'.");
                return false;
            }

            if (!recipe.CanCraft(sourceInventory)) return false;

            // Consume ingredients immediately on enqueue
            recipe.ConsumeIngredients(sourceInventory);

            var job = new CraftJob
            {
                Recipe          = recipe,
                SourceInventory = sourceInventory,
                TimeRemaining   = recipe.CraftTime,
                Status          = CraftJobStatus.Pending
            };
            _queue.Enqueue(job);

            if (_craftCoroutine == null)
                _craftCoroutine = StartCoroutine(ProcessQueue());

            InventoryEventBus.Publish(new CraftStartedEvent { Recipe = recipe, Duration = recipe.CraftTime });
            return true;
        }

        public void CancelCraft()
        {
            if (_currentJob == null) return;
            _currentJob.Status = CraftJobStatus.Cancelled;

            if (_craftCoroutine != null)
            {
                StopCoroutine(_craftCoroutine);
                _craftCoroutine = null;
            }

            _currentJob = null;
            // Note: ingredients are NOT refunded (design decision; override if needed)
        }

        #endregion

        #region Queue Processing

        private IEnumerator ProcessQueue()
        {
            while (_queue.Count > 0)
            {
                _currentJob = _queue.Dequeue();
                _currentJob.Status = CraftJobStatus.InProgress;

                OnCraftStartedEvent?.Invoke(_currentJob);

                if (_currentJob.Recipe.CraftTime > 0f)
                {
                    // Timed craft
                    while (_currentJob.TimeRemaining > 0f)
                    {
                        _currentJob.TimeRemaining -= Time.deltaTime;
                        OnCraftProgressEvent?.Invoke(_currentJob.Progress);
                        yield return null;
                    }
                }

                CompleteCraft(_currentJob);
                _currentJob = null;
            }

            _craftCoroutine = null;
        }

        private void CompleteCraft(CraftJob job)
        {
            var recipe    = job.Recipe;
            var inventory = job.SourceInventory;

            int leftover = inventory.AddItem(recipe.ResultItem, recipe.ResultAmount);
            if (leftover > 0)
                Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} Craft completed but {leftover} result item(s) could not fit in inventory.");

            job.Status = CraftJobStatus.Completed;
            OnCraftCompletedEvent?.Invoke(job);
            InventoryEventBus.Publish(new CraftCompletedEvent
            {
                Recipe     = recipe,
                ResultItem = inventory.Items.Count > 0 ? inventory.Items[^1] : null
            });
        }

        #endregion

        #region Public Accessors

        public CraftJob CurrentJob        => _currentJob;
        public int       QueuedJobCount   => _queue.Count;
        public float     CurrentProgress  => _currentJob?.Progress ?? 0f;

        /// <summary>Returns true if this station can execute the given recipe.</summary>
        public bool CanCraft(CraftingRecipeSO recipe, IInventory inventory)
        {
            if (recipe == null || inventory == null) return false;
            if (!string.IsNullOrEmpty(recipe.RequiredStationType) &&
                recipe.RequiredStationType != _stationType) return false;
            if (_queue.Count >= _maxQueueSize) return false;
            return recipe.CanCraft(inventory);
        }

        #endregion
    }
}
