#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Nonatomic.ServiceLocator
{
    /// <summary>
    ///     A ScriptableObject-based service locator for managing and accessing services throughout the application.
    /// </summary>
    public abstract partial class BaseServiceLocator : ScriptableObject
    {
        [NonSerialized] protected readonly object Lock = new();
        [NonSerialized] protected readonly Dictionary<Type, object> ServiceMap = new();

        #if ENABLE_SL_SCENE_TRACKING
        [NonSerialized] protected readonly Dictionary<Type, string> ServiceSceneMap = new();
        #endif

        public bool IsInitialized { get; protected set; }

        /// <summary>
        ///     Initializes the ServiceLocator when enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (IsInitialized)
            {
                return;
            }

            Initialize();
        }

        /// <summary>
        ///     Cleans up the ServiceLocator when disabled.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (!IsInitialized)
            {
                return;
            }

            DeInitialize();
        }

        public event Action? OnChange;

        /// <summary>
        ///     Returns a dictionary containing all currently registered services.
        /// </summary>
        public virtual IReadOnlyDictionary<Type, object> GetAllServices()
        {
            lock (Lock)
            {
                return new Dictionary<Type, object>(ServiceMap);
            }
        }

        #if ENABLE_SL_SCENE_TRACKING
        /// <summary>
        ///     Returns the scene name associated with a service type.
        /// </summary>
        public virtual string GetSceneNameForService(Type serviceType)
        {
            lock (Lock)
            {
                return !ServiceSceneMap.TryGetValue(serviceType, out var sceneName)
                    ? "No Scene"
                    : sceneName;
            }
        }
        #endif

        /// <summary>
        /// Validates if a service reference is valid and matches the currently registered service.
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <param name="serviceReference">The service reference to validate</param>
        /// <returns>True if the reference is valid and matches the current registration</returns>
        public virtual bool IsServiceValid<T>(T? serviceReference) where T : class
        {
            if (serviceReference == null)
            {
                return false;
            }
    
            lock (Lock)
            {
                // Check if we have a service of this type registered
                if (!ServiceMap.TryGetValue(typeof(T), out var registeredService))
                {
                    return false;
                }
        
                // Check if the registered service is valid
                if (registeredService == null || 
                    (registeredService is UnityEngine.Object unityObj && unityObj == null))
                {
                    return false;
                }
        
                // Check if the reference matches the registered service
                // This handles the case where the service was replaced
                if (!ReferenceEquals(serviceReference, registeredService))
                {
                    return false;
                }
        
                // If we got here, the reference is valid and matches the current registration
                return true;
            }
        }

        /// <summary>
        /// Validates if a service held by the ServiceLocator is valid.
        /// </summary>
        /// <typeparam name="T">The service interface type</typeparam>
        /// <returns>True if the reference is valid</returns>
        public virtual bool IsServiceValid<T>() where T : class
        {
            lock (Lock)
            {
                // First check if the service is actually registered
                if (!ServiceMap.TryGetValue(typeof(T), out var service))
                {
                    return false;
                }

                // Check if the service is null
                if (service == null)
                {
                    return false;
                }

                // Special handling for Unity objects that might be destroyed
                if (service is Object unityObject)
                {
                    // Unity's "==" operator is overridden to check if the object is destroyed
                    return unityObject != null;
                }

                // For regular C# objects, if we got this far, it's valid
                return true;
            }
        }

        /// <summary>
        ///     Registers a service with the service locator.
        /// </summary>
        /// <typeparam name="T">The type of the service being registered.</typeparam>
        /// <param name="service">The instance of the service to register.</param>
        public virtual void Register<T>(T service) where T : class
        {
            lock (Lock)
            {
                if (service == null)
                {
                    throw new ArgumentNullException("service", "Cannot register a null service.");
                }

                var serviceType = typeof(T);
                ServiceMap[serviceType] = service;

                #if ENABLE_SL_SCENE_TRACKING
                // Track scene information for this service
                var sceneName = "No Scene";
                if (service is MonoBehaviour monoBehaviour)
                {
                    sceneName = monoBehaviour.gameObject.scene.name;
                }

                ServiceSceneMap[serviceType] = sceneName;
                #endif

                // Notify async registrations - using different preprocessor blocks
                #if ENABLE_SL_ASYNC
                ResolveAsyncPromises(serviceType, service);
                #endif

                #if ENABLE_SL_UNITASK
                ResolveUniTaskPromises(serviceType, service);
                #endif

                #if ENABLE_SL_PROMISES
                ResolvePromises(serviceType, service);
                #endif

                #if ENABLE_SL_COROUTINES
                ResolvePendingCoroutines(serviceType, service);
                #endif

                NotifyChange();

                #if ENABLE_SL_LOGGING
                Debug.Log($"Service registered: {serviceType.Name}");
                #endif
            }
        }

        /// <summary>
        ///     Invokes the OnChange event.
        /// </summary>
        protected virtual void NotifyChange()
        {
            OnChange?.Invoke();
        }

        /// <summary>
        ///     Unregisters a service from the service locator.
        /// </summary>
        /// <typeparam name="T">The type of the service to unregister.</typeparam>
        public virtual void Unregister<T>() where T : class
        {
            lock (Lock)
            {
                var serviceType = typeof(T);
                ServiceMap.Remove(serviceType);

                #if ENABLE_SL_SCENE_TRACKING
                ServiceSceneMap.Remove(serviceType);
                #endif

                // Reject promises using different preprocessor blocks
                #if ENABLE_SL_ASYNC
                RejectAsyncPromises(serviceType);
                #endif

                #if ENABLE_SL_UNITASK
                RejectUniTaskPromises(serviceType);
                #endif

                #if ENABLE_SL_PROMISES
                RejectPromises(serviceType);
                #endif

                NotifyChange();

                #if ENABLE_SL_LOGGING
                Debug.Log($"Service unregistered: {serviceType.Name}");
                #endif
            }
        }

        /// <summary>
        ///     Attempts to retrieve a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <param name="service">When this method returns, contains the service instance if found; otherwise, null.</param>
        /// <returns>true if the service was found; otherwise, false.</returns>
        public virtual bool TryGetService<T>(out T? service) where T : class
        {
            service = null;

            lock (Lock)
            {
                if (!ServiceMap.TryGetValue(typeof(T), out var result) || result is not T typedService)
                {
                    return false;
                }

                service = typedService;
                return true;
            }
        }

        /// <summary>
        ///     Attempts to retrieve a service, returning null if not found.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>The service instance if found; otherwise, null.</returns>
        public virtual T? GetServiceOrDefault<T>() where T : class
        {
            return TryGetService(out T? service) ? service : null;
        }

        /// <summary>
        ///     Cleans up the ServiceLocator, clearing services, promises, and coroutines.
        /// </summary>
        public virtual void Cleanup()
        {
            lock (Lock)
            {
                foreach (var service in ServiceMap.Values)
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                #if ENABLE_SL_SCENE_TRACKING
                ServiceSceneMap.Clear();
                #endif

                ServiceMap.Clear();

                // Clean up async-related resources with preprocessor directives
                #if ENABLE_SL_ASYNC
                CleanupAsyncPromises();
                #endif

                #if ENABLE_SL_UNITASK
                CleanupUniTaskPromises();
                #endif

                #if ENABLE_SL_PROMISES
                CleanupPromises();
                #endif

                #if ENABLE_SL_COROUTINES
                CancelPendingCoroutines();
                #endif

                NotifyChange();

                #if ENABLE_SL_LOGGING
                Debug.Log("Service Locator cleaned up");
                #endif
            }
        }

        /// <summary>
        ///     Initializes the ServiceLocator.
        /// </summary>
        protected virtual void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            #endif

            #if ENABLE_SL_SCENE_TRACKING
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            #endif

            IsInitialized = true;

            #if ENABLE_SL_LOGGING
            Debug.Log("Service Locator initialized");
            #endif
        }

        /// <summary>
        ///     De-initializes the ServiceLocator.
        /// </summary>
        protected virtual void DeInitialize()
        {
            if (!IsInitialized)
            {
                return;
            }

            #if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            #endif

            #if ENABLE_SL_SCENE_TRACKING
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            #endif

            Cleanup();

            IsInitialized = false;

            #if ENABLE_SL_LOGGING
            Debug.Log("Service Locator de-initialized");
            #endif
        }

        #if ENABLE_SL_SCENE_TRACKING
        /// <summary>
        ///     Handles when a scene is unloaded.
        /// </summary>
        protected virtual void HandleSceneUnloaded(Scene scene)
        {
            lock (Lock)
            {
                UnregisterServicesFromScene(scene.name);
            }
        }

        /// <summary>
        ///     Unregisters all services that belong to a specific scene that has been unloaded.
        /// </summary>
        /// <param name="sceneName">The name of the unloaded scene.</param>
        public virtual void UnregisterServicesFromScene(string sceneName)
        {
            lock (Lock)
            {
                var servicesToRemove = ServiceSceneMap
                    .Where(kvp => kvp.Value == sceneName)
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (servicesToRemove.Count == 0)
                {
                    return;
                }

                #if ENABLE_SL_LOGGING
                Debug.LogWarning($"Detected: {servicesToRemove.Count} services remain in unloaded scene: {sceneName}");
                #endif

                foreach (var serviceType in servicesToRemove)
                {
                    ServiceMap.Remove(serviceType);
                    ServiceSceneMap.Remove(serviceType);

                    // Reject any pending promises with appropriate preprocessor directives
                    #if ENABLE_SL_ASYNC
                    RejectAsyncPromises(serviceType, new ObjectDisposedException(
                        serviceType.Name,
                        $"Service {serviceType.Name} from scene {sceneName} was unregistered"));
                    #endif

                    #if ENABLE_SL_UNITASK
                    RejectUniTaskPromises(serviceType, new ObjectDisposedException(
                        serviceType.Name,
                        $"Service {serviceType.Name} from scene {sceneName} was unregistered"));
                    #endif

                    #if ENABLE_SL_PROMISES
                    RejectPromises(serviceType, new ObjectDisposedException(
                        serviceType.Name,
                        $"Service {serviceType.Name} from scene {sceneName} was unregistered"));
                    #endif

                    #if ENABLE_SL_LOGGING
                    Debug.LogWarning($"Unregistered {serviceType.Name} from unloaded scene {sceneName}");
                    #endif
                }

                NotifyChange();
            }
        }
        #endif

        #if UNITY_EDITOR
        /// <summary>
        ///     Handles play mode state changes in the editor.
        /// </summary>
        protected virtual void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode)
            {
                return;
            }

            DeInitialize();
        }
        #endif

        // Implementation-specific helper method stubs that will be defined in partial classes
        
        #if ENABLE_SL_ASYNC
        protected virtual void ResolveAsyncPromises(Type serviceType, object service) {}
        protected virtual void RejectAsyncPromises(Type serviceType, Exception? exception = null) {}
        protected virtual void CleanupAsyncPromises() {}
        #endif

        #if ENABLE_SL_UNITASK
        protected virtual void ResolveUniTaskPromises(Type serviceType, object service) {}
        protected virtual void RejectUniTaskPromises(Type serviceType, Exception? exception = null) {}
        protected virtual void CleanupUniTaskPromises() {}
        #endif

        #if ENABLE_SL_PROMISES
        protected virtual void ResolvePromises(Type serviceType, object service) {}
        protected virtual void RejectPromises(Type serviceType, Exception? exception = null) {}
        protected virtual void CleanupPromises() {}
        #endif

        #if ENABLE_SL_COROUTINES
        protected virtual void ResolvePendingCoroutines(Type serviceType, object service) {}
        protected virtual void CancelPendingCoroutines() {}
        #endif
    }
}