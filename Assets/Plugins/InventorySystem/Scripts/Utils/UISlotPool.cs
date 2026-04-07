// =============================================================================
// UISlotPool.cs
// Generic object pool for UI slot GameObjects (inventory cells, hotbar icons, etc.)
// =============================================================================
// Avoids expensive Instantiate/Destroy calls during grid resizes and inventory
// open/close operations.
// =============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Generic MonoBehaviour-aware object pool.
    /// The pool expands automatically when exhausted and shrinks by releasing
    /// excess objects back to the parent transform.
    /// </summary>
    public class UISlotPool : MonoBehaviour
    {
        #region Inspector

        [Header("Pool Config")]
        [Tooltip("Prefab to pool (must have a MonoBehaviour component T on it).")]
        [SerializeField] private GameObject _prefab;

        [Tooltip("Number of instances to pre-warm on Awake.")]
        [SerializeField] [Min(0)] private int _prewarmCount = 20;

        [Tooltip("Parent transform for pooled (inactive) objects. Defaults to this transform.")]
        [SerializeField] private Transform _poolParent;

        #endregion

        #region State

        private readonly Stack<GameObject> _free  = new Stack<GameObject>();
        private readonly List<GameObject>  _inUse = new List<GameObject>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_poolParent == null) _poolParent = transform;
            Prewarm(_prewarmCount);
        }

        #endregion

        #region API

        /// <summary>Retrieves an instance from the pool, expanding if necessary.</summary>
        public GameObject Rent(Transform parent = null)
        {
            GameObject go;
            if (_free.Count > 0)
            {
                go = _free.Pop();
            }
            else
            {
                go = Instantiate(_prefab, _poolParent);
                Debug.Log($"{InventorySystemConstants.LOG_PREFIX} UISlotPool: expanded (new instance created).");
            }

            go.transform.SetParent(parent != null ? parent : _poolParent, worldPositionStays: false);
            go.SetActive(true);
            _inUse.Add(go);
            return go;
        }

        /// <summary>
        /// Returns a pooled instance. If T is on the object, resets it before pooling.
        /// </summary>
        public void Return(GameObject go)
        {
            if (go == null) return;
            if (!_inUse.Remove(go))
            {
                Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} UISlotPool: returning object that wasn't rented from this pool.");
            }

            go.SetActive(false);
            go.transform.SetParent(_poolParent, worldPositionStays: false);
            _free.Push(go);
        }

        /// <summary>Returns all currently rented instances.</summary>
        public void ReturnAll()
        {
            for (int i = _inUse.Count - 1; i >= 0; i--)
                Return(_inUse[i]);
        }

        public int FreeCount  => _free.Count;
        public int InUseCount => _inUse.Count;
        public int TotalCount => _free.Count + _inUse.Count;

        #endregion

        #region Internal

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(_prefab, _poolParent);
                go.SetActive(false);
                _free.Push(go);
            }
        }

        #endregion
    }
}
