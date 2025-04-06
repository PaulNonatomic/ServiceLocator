#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if !DISABLE_SL_UNITASK && ENABLE_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace Nonatomic.ServiceLocator
{
	/// <summary>
	///     A ScriptableObject-based service locator for managing and accessing services throughout the application.
	/// </summary>
	public abstract partial class BaseServiceLocator : ScriptableObject
	{
		[NonSerialized] protected readonly object Lock = new();

		#if !DISABLE_SL_COROUTINES
		[NonSerialized] protected readonly List<(Type, Action<object>)> PendingCoroutines = new();
		#endif

		#if !DISABLE_SL_ASYNC || !DISABLE_SL_PROMISES
		[NonSerialized] protected readonly Dictionary<Type, List<TaskCompletionSource<object>>> PromiseMap = new();
		#endif

		[NonSerialized] protected readonly Dictionary<Type, object> ServiceMap = new();

		#if !DISABLE_SL_SCENE_TRACKING
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

		#if !DISABLE_SL_SCENE_TRACKING
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

				#if !DISABLE_SL_SCENE_TRACKING
				// Track scene information for this service
				var sceneName = "No Scene";
				if (service is MonoBehaviour monoBehaviour)
				{
					sceneName = monoBehaviour.gameObject.scene.name;
				}

				ServiceSceneMap[serviceType] = sceneName;
				#endif

				#if !DISABLE_SL_ASYNC || !DISABLE_SL_PROMISES
				// Resolve any pending promises for this service
				if (PromiseMap.TryGetValue(serviceType, out var taskCompletions))
				{
					foreach (var tcs in taskCompletions.ToList())
					{
						tcs.TrySetResult(service);
					}

					PromiseMap.Remove(serviceType);
				}
				#endif

				#if !DISABLE_SL_UNITASK && ENABLE_UNITASK
                // Resolve any pending UniTask promises for this service
                if (UniTaskPromiseMap.TryGetValue(serviceType, out var uniTaskCompletions))
                {
                    foreach (var utcs in uniTaskCompletions.ToList())
                    {
                        utcs.TrySetResult(service);
                    }

                    UniTaskPromiseMap.Remove(serviceType);
                }
				#endif

				#if !DISABLE_SL_COROUTINES
				// Notify any pending coroutines
				var pendingCoroutines =
					PendingCoroutines.FindAll(pendingCoroutine => pendingCoroutine.Item1 == serviceType);
				foreach (var (_, callback) in pendingCoroutines)
				{
					callback(service);
				}

				PendingCoroutines.RemoveAll(pendingCoroutine => pendingCoroutine.Item1 == serviceType);
				#endif

				NotifyChange();

				#if !DISABLE_SL_LOGGING
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

				#if !DISABLE_SL_SCENE_TRACKING
				ServiceSceneMap.Remove(serviceType);
				#endif

				#if !DISABLE_SL_ASYNC || !DISABLE_SL_PROMISES
				// Reject any pending promises for this service
				if (PromiseMap.TryGetValue(serviceType, out var taskList))
				{
					foreach (var tcs in taskList.ToList())
					{
						tcs.TrySetException(new ObjectDisposedException(
							serviceType.Name,
							$"Service {serviceType.Name} was unregistered while waiting for it to be available"));
					}

					PromiseMap.Remove(serviceType);
				}
				#endif

				#if !DISABLE_SL_UNITASK && ENABLE_UNITASK
                // Reject any pending UniTask promises for this service
                if (UniTaskPromiseMap.TryGetValue(serviceType, out var uniTaskList))
                {
                    foreach (var utcs in uniTaskList.ToList())
                    {
                        utcs.TrySetException(new ObjectDisposedException(
                            serviceType.Name,
                            $"Service {serviceType.Name} was unregistered while waiting for it to be available"));
                    }

                    UniTaskPromiseMap.Remove(serviceType);
                }
				#endif

				NotifyChange();

				#if !DISABLE_SL_LOGGING
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

				#if !DISABLE_SL_SCENE_TRACKING
				ServiceSceneMap.Clear();
				#endif

				ServiceMap.Clear();

				#if !DISABLE_SL_ASYNC || !DISABLE_SL_PROMISES
				CleanupPromises();
				#endif

				#if !DISABLE_SL_UNITASK && ENABLE_UNITASK
                CleanupUniTaskPromises();
				#endif

				#if !DISABLE_SL_COROUTINES
				CancelPendingCoroutines();
				#endif

				NotifyChange();

				#if !DISABLE_SL_LOGGING
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

			#if !DISABLE_SL_SCENE_TRACKING
			SceneManager.sceneUnloaded += HandleSceneUnloaded;
			#endif

			IsInitialized = true;

			#if !DISABLE_SL_LOGGING
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

			#if !DISABLE_SL_SCENE_TRACKING
			SceneManager.sceneUnloaded -= HandleSceneUnloaded;
			#endif

			Cleanup();

			IsInitialized = false;

			#if !DISABLE_SL_LOGGING
			Debug.Log("Service Locator de-initialized");
			#endif
		}

		#if !DISABLE_SL_COROUTINES
		/// <summary>
		///     Cancels all pending coroutines.
		/// </summary>
		protected virtual void CancelPendingCoroutines()
		{
			lock (Lock)
			{
				foreach (var (_, callback) in PendingCoroutines)
				{
					callback(null!);
				}

				PendingCoroutines.Clear();
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

		#if !DISABLE_SL_ASYNC || !DISABLE_SL_PROMISES
		/// <summary>
		///     Cancels and removes all unfulfilled promises.
		/// </summary>
		public virtual void CleanupPromises()
		{
			lock (Lock)
			{
				foreach (var promiseList in PromiseMap.Values)
				{
					foreach (var promise in promiseList.ToList())
					{
						promise.TrySetCanceled();
					}
				}

				PromiseMap.Clear();
			}
		}

		/// <summary>
		///     Validates if a service reference is valid and matches the currently registered service.
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
					(registeredService is Object unityObj && unityObj == null))
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
		///     Validates if a service held by the ServiceLocator is valid.
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
		///     Rejects all pending promises for a specific service type with an exception.
		/// </summary>
		public virtual void RejectService<T>(Exception exception) where T : class
		{
			if (exception == null)
			{
				throw new ArgumentNullException(nameof(exception));
			}

			lock (Lock)
			{
				var serviceType = typeof(T);
				if (!PromiseMap.TryGetValue(serviceType, out var taskList))
				{
					return;
				}

				foreach (var tcs in taskList.ToList())
				{
					tcs.TrySetException(exception);
				}

				PromiseMap.Remove(serviceType);
			}
		}
		#endif

		#if !DISABLE_SL_SCENE_TRACKING
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

				#if !DISABLE_SL_LOGGING
				Debug.LogWarning($"Detected: {servicesToRemove.Count} services remain in unloaded scene: {sceneName}");
				#endif

				foreach (var serviceType in servicesToRemove)
				{
					ServiceMap.Remove(serviceType);
					ServiceSceneMap.Remove(serviceType);

					#if !DISABLE_SL_ASYNC || !DISABLE_SL_PROMISES
					if (PromiseMap.TryGetValue(serviceType, out var taskList))
					{
						foreach (var tcs in taskList.ToList())
						{
							tcs.TrySetException(new ObjectDisposedException(
								serviceType.Name,
								$"Service {serviceType.Name} from scene {sceneName} was unregistered"));
						}

						PromiseMap.Remove(serviceType);
					}
					#endif

					#if !DISABLE_SL_UNITASK && ENABLE_UNITASK
                    if (UniTaskPromiseMap.TryGetValue(serviceType, out var uniTaskList))
                    {
                        foreach (var utcs in uniTaskList.ToList())
                        {
                            utcs.TrySetException(new ObjectDisposedException(
                                serviceType.Name,
                                $"Service {serviceType.Name} from scene {sceneName} was unregistered"));
                        }

                        UniTaskPromiseMap.Remove(serviceType);
                    }
					#endif

					#if !DISABLE_SL_LOGGING
					Debug.LogWarning($"Unregistered {serviceType.Name} from unloaded scene {sceneName}");
					#endif
				}

				NotifyChange();
			}
		}
		#endif
	}
}