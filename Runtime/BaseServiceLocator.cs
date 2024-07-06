using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nonatomic.ServiceLocator
{
	/// <summary>
	/// A ScriptableObject-based service locator for managing and accessing services throughout the application.
	/// This class provides various methods for registering, unregistering, and retrieving services.
	/// </summary>
	public abstract class BaseServiceLocator : ScriptableObject
	{
		public bool IsInitialized { get; protected  set; } = false;

		// Stores registered services, mapping their types to instances.
		protected  readonly Dictionary<Type, object> ServiceMap = new();

		// Stores promises for services that haven't been registered yet.
		protected  readonly Dictionary<Type, TaskCompletionSource<object>> PromiseMap = new();
		
		// Store coroutines created to wait for services not yet registered.
		protected readonly List<(Type, Action<object>)> PendingCoroutines = new();

		/// <summary>
		/// Manually cleans up all unfulfilled promises.
		/// </summary>
		public virtual void CleanupPromises()
		{
			foreach (var promise in PromiseMap.Values)
			{
				promise.TrySetCanceled();
			}
			PromiseMap.Clear();
		}


		/// <summary>
		/// Registers a service with the service locator.
		/// </summary>
		/// <typeparam name="T">The type of the service being registered.</typeparam>
		/// <param name="service">The instance of the service to register.</param>
		/// <remarks>
		/// If there are any pending promises for this service type, they will be resolved.
		/// </remarks>
		public virtual void Register<T>(T service) where T : class
		{
			var serviceType = typeof(T);
			ServiceMap[serviceType] = service;

			if (PromiseMap.TryGetValue(serviceType, out var taskCompletion))
			{
				taskCompletion.TrySetResult(service);
				PromiseMap.Remove(serviceType);
			}

			// Resolve any pending coroutines for this service type
			var pendingCoroutines = PendingCoroutines.FindAll(pendingCoroutine => pendingCoroutine.Item1 == serviceType);
			foreach (var (_, callback) in pendingCoroutines)
			{
				callback(service);
			}
			PendingCoroutines.RemoveAll(pendingCoroutine => pendingCoroutine.Item1 == serviceType);
		}

		/// <summary>
		/// Unregisters a service from the service locator.
		/// </summary>
		/// <typeparam name="T">The type of the service to unregister.</typeparam>
		/// <remarks>
		/// This method only removes the service from the ServiceMap. It does not affect any pending promises.
		/// </remarks>
		public virtual void Unregister<T>() where T : class
		{
			ServiceMap.Remove(typeof(T));
		}

		/// <summary>
		/// Asynchronously retrieves a service of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <returns>A Task that resolves to the requested service instance.</returns>
		/// <remarks>
		/// If the service is not yet registered, this method will wait until it becomes available.
		/// </remarks>
		public virtual async Task<T> GetServiceAsync<T>() where T : class
		{
			var serviceType = typeof(T);

			if (ServiceMap.TryGetValue(serviceType, out var service)) return (T)service;

			if (!PromiseMap.TryGetValue(serviceType, out var taskCompletion))
			{
				taskCompletion = new TaskCompletionSource<object>();
				PromiseMap[serviceType] = taskCompletion;
			}

			return (T)await taskCompletion.Task;
		}

		/// <summary>
		/// Attempts to retrieve a service of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <param name="service">When this method returns, contains the service instance if found; otherwise, null.</param>
		/// <returns>true if the service was found; otherwise, false.</returns>
		public virtual bool TryGetService<T>(out T? service) where T : class
		{
			service = null;

			if (ServiceMap.TryGetValue(typeof(T), out var result) && result is T typedService)
			{
				service = typedService;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Retrieves a service using a coroutine.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <param name="callback">Action to be called with the retrieved service.</param>
		/// <returns>An IEnumerator for use with StartCoroutine.</returns>
		public virtual IEnumerator GetServiceCoroutine<T>(Action<T?> callback) where T : class
		{
			var serviceType = typeof(T);

			// Check if the service is already available
			if (ServiceMap.TryGetValue(serviceType, out var service))
			{
				callback((T)service);
				yield break;
			}

			// Create a pending coroutine entry
			var pendingCoroutine = (serviceType, new Action<object?>(obj => callback(obj as T)));
			PendingCoroutines.Add(pendingCoroutine);

			while (PendingCoroutines.Count > 0)
			{
				// Check if the service has been registered during this iteration
				if (ServiceMap.TryGetValue(serviceType, out service))
				{
					PendingCoroutines.Remove(pendingCoroutine);
					callback((T)service);
					yield break;
				}

				yield return null;
			}
		}

		/// <summary>
		/// Attempts to retrieve a service, returning null if not found.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <returns>The service instance if found; otherwise, null.</returns>
		public virtual T? GetServiceOrDefault<T>() where T : class
		{
			return TryGetService(out T service) ? service : null;
		}

		/// <summary>
		/// Retrieves a service using a promise-based approach.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <returns>An IServicePromise that will resolve with the requested service.</returns>
		/// <remarks>
		/// If the service is already registered, the promise will resolve immediately.
		/// If not, it will resolve when the service is registered in the future.
		/// </remarks>
		public virtual IServicePromise<T> GetService<T>() where T : class
		{
			var promise = new ServicePromise<T>();
			var serviceType = typeof(T);

			if (ServiceMap.TryGetValue(serviceType, out var service))
			{
				promise.Resolve((T)service);
			}
			else
			{
				if (!PromiseMap.TryGetValue(serviceType, out var taskCompletion))
				{
					taskCompletion = new TaskCompletionSource<object>();
					PromiseMap[serviceType] = taskCompletion;
				}

				taskCompletion.Task.ContinueWith(task =>
				{
					if (task.IsCompletedSuccessfully)
						promise.Resolve((T)task.Result);
					else
						promise.Reject(task.Exception);
				});
			}

			return promise;
		}

		/// <summary>
		/// Cleans up the ServiceLocator, clearing services, promises, and coroutines without affecting initialization state.
		/// </summary>
		public virtual void Cleanup()
		{
			ServiceMap.Clear();
			CleanupPromises();
			CancelPendingCoroutines();
		}

		/// <summary>
		/// Initializes the ServiceLocator when the ScriptableObject is enabled.
		/// This method is called when the ScriptableObject is loaded or when entering play mode.
		/// </summary>
		protected virtual void OnEnable()
		{
			if (!IsInitialized)
			{
				Initialize();
			}
		}

		/// <summary>
		/// Cleans up the ServiceLocator when the ScriptableObject is disabled.
		/// This method is called when the ScriptableObject is unloaded or when exiting play mode.
		/// </summary>
		protected virtual void OnDisable()
		{
			DeInitialize();
		}

		/// <summary>
		/// Initializes the ServiceLocator, setting up scene-based cleanup.
		/// </summary>
		protected virtual void Initialize()
		{
			SceneManager.sceneUnloaded += OnSceneUnloaded;
			IsInitialized = true;
		}

		/// <summary>
		/// Fully de-initializes the ServiceLocator, clearing all data and resetting its state.
		/// </summary>
		protected virtual void DeInitialize()
		{
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
			IsInitialized = false;
			Cleanup();
		}

		/// <summary>
		/// Cancels all pending coroutines.
		/// </summary>
		protected virtual void CancelPendingCoroutines()
		{
			foreach (var (_, callback) in PendingCoroutines)
			{
				callback(null);
			}
			PendingCoroutines.Clear();
		}

		/// <summary>
		/// Cleans up promises when a scene is unloaded.
		/// </summary>
		protected virtual void OnSceneUnloaded(Scene scene)
		{
			CleanupPromises();
			CancelPendingCoroutines();
		}
	}
}