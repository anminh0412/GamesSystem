// =============================================================================
// ServiceLocator.cs
// Lightweight service locator for the Advanced Inventory System.
// =============================================================================
// Managers register themselves on Awake/Start and deregister on OnDestroy.
// Systems that need a service call ServiceLocator.Get<T>().
//
// This avoids hard Singleton coupling and makes unit testing straightforward.
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// Central service registry.
    /// All AIS manager classes register here at startup.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        // ── Registration ──────────────────────────────────────────────────────

        /// <summary>
        /// Register <paramref name="service"/> under type <typeparamref name="T"/>.
        /// Overwrites any existing registration and logs a warning.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"{InventorySystemConstants.LOG_WARN_PREFIX} ServiceLocator: overwriting existing registration for {type.Name}.");
            }
            _services[type] = service;
        }

        /// <summary>Remove the registration for type <typeparamref name="T"/>.</summary>
        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        // ── Retrieval ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the registered service of type <typeparamref name="T"/>, or null
        /// if nothing is registered. Logs an error in the latter case.
        /// </summary>
        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return service as T;

            Debug.LogError($"{InventorySystemConstants.LOG_ERR_PREFIX} ServiceLocator: no service registered for {typeof(T).Name}.");
            return null;
        }

        /// <summary>
        /// Returns the registered service of type <typeparamref name="T"/> without
        /// logging an error when absent. Useful for optional services.
        /// </summary>
        public static T TryGet<T>() where T : class
        {
            _services.TryGetValue(typeof(T), out var service);
            return service as T;
        }

        /// <summary>Returns true when a service of type <typeparamref name="T"/> is registered.</summary>
        public static bool Has<T>() where T : class
            => _services.ContainsKey(typeof(T));

        // ── Lifecycle ─────────────────────────────────────────────────────────

        /// <summary>
        /// Clears all registrations. Call during scene teardown or test cleanup.
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
            Debug.Log($"{InventorySystemConstants.LOG_PREFIX} ServiceLocator cleared.");
        }
    }
}
