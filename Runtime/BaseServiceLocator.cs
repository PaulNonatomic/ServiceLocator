#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
		public event Action? OnChange;
		public bool IsInitialized { get; protected set; } = false;

		protected readonly Dictionary<Type, object> ServiceMap = new();
		protected readonly Dictionary<Type, List<TaskCompletionSource<object>>> PromiseMap = new();
		protected readonly Dictionary<Type, string> ServiceSceneMap = new();
		protected readonly List<(Type, Action<object>)> PendingCoroutines = new();
		protected readonly object Lock = new();
		
		/// <summary>
		/// Returns a dictionary containing all currently registered services.
		/// </summary>
		/// <returns>A dictionary with service types as keys and service instances as values.</returns>
		public virtual IReadOnlyDictionary<Type, object> GetAllServices()
		{
			lock (Lock)
			{
				// Return a copy of the service map to avoid potential threading issues
				return new Dictionary<Type, object>(ServiceMap);
			}
		}
		
		public virtual string GetSceneNameForService(Type serviceType)
		{
			lock (Lock)
			{
				return ServiceSceneMap.TryGetValue(serviceType, out var sceneName) 
					? sceneName 
					: "No Scene";
			}
		}

		/// <summary>
		/// Manually cleans up all unfulfilled promises.
		/// </summary>
		/// <summary>
		/// Manually cleans up all unfulfilled promises.
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
		
		public virtual void RejectService<T>(Exception exception) where T : class
		{
			if (exception == null) throw new ArgumentNullException(nameof(exception));

			lock (Lock)
			{
				var serviceType = typeof(T);
				if (PromiseMap.TryGetValue(serviceType, out var taskList))
				{
					foreach (var tcs in taskList.ToList())
					{
						tcs.TrySetException(exception);
					}
					PromiseMap.Remove(serviceType);
				}
			}
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
			lock (Lock)
			{
				if (service == null)
				{
					throw new ArgumentNullException("service", "Cannot register a null service.");
				}

				var serviceType = typeof(T);
				ServiceMap[serviceType] = service;
				
				// Track scene information for this service
				var sceneName = "No Scene";
				if (service is MonoBehaviour monoBehaviour)
				{
					sceneName = monoBehaviour.gameObject.scene.name;
				}
		
				ServiceSceneMap[serviceType] = sceneName;

				if (PromiseMap.TryGetValue(serviceType, out var taskCompletions))
				{
					foreach (var tcs in taskCompletions.ToList()) // ToList to avoid modification issues
					{
						tcs.TrySetResult(service);
					}
					PromiseMap.Remove(serviceType);
				}

				var pendingCoroutines = PendingCoroutines.FindAll(pendingCoroutine => pendingCoroutine.Item1 == serviceType);
				foreach (var (_, callback) in pendingCoroutines)
				{
					callback(service);
				}
				PendingCoroutines.RemoveAll(pendingCoroutine => pendingCoroutine.Item1 == serviceType);

				NotifyChange();
			}
		}

		protected virtual void NotifyChange()
		{
			OnChange?.Invoke();
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
			lock (Lock)
			{
				var serviceType = typeof(T);
				ServiceMap.Remove(serviceType);
				ServiceSceneMap.Remove(serviceType);
				NotifyChange();
			}
		}

		/// <summary>
		/// Asynchronously retrieves a service of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <returns>A Task that resolves to the requested service instance.</returns>
		/// <remarks>
		/// If the service is not yet registered, this method will wait until it becomes available.
		/// </remarks>
		public virtual async Task<T> GetServiceAsync<T>(CancellationToken cancellation = default) where T : class
		{
			var serviceType = typeof(T);
			TaskCompletionSource<object> taskCompletion;

			lock (Lock)
			{
				if (ServiceMap.TryGetValue(serviceType, out var service))
				{
					return (T)service;
				}

				taskCompletion = new TaskCompletionSource<object>();
				if (!PromiseMap.TryGetValue(serviceType, out var taskList))
				{
					taskList = new List<TaskCompletionSource<object>>();
					PromiseMap[serviceType] = taskList;
				}
				taskList.Add(taskCompletion);
			}

			if (cancellation.CanBeCanceled)
			{
				cancellation.Register(() =>
				{
					lock (Lock)
					{
						if (!PromiseMap.TryGetValue(serviceType, out var taskList) || !taskList.Contains(taskCompletion)) return;
						
						taskCompletion.TrySetCanceled();
						taskList.Remove(taskCompletion);
						if (taskList.Count != 0) return;
						
						PromiseMap.Remove(serviceType);
					}
				});
			}

			return (T)await taskCompletion.Task;
		}

		//Syntactic sugar for adding additional services to an existing GetServiceAsync call
		public virtual Task<(T1, T2)> GetServiceAsync<T1, T2>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class 
			=> GetServicesAsync<T1, T2>(cancellation);

		public virtual async Task<(T1, T2)> GetServicesAsync<T1, T2>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
		{
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			var linkedToken = linkedCts.Token;
	
			try
			{
				var task1 = GetServiceAsync<T1>(linkedToken);
				var task2 = GetServiceAsync<T2>(linkedToken);

				await Task.WhenAll(task1, task2);
				return (await task1, await task2);
			}
			catch (OperationCanceledException)
			{
				// If any task is canceled, cancel all related requests
				linkedCts.Cancel();
				throw;
			}
		}
		
		//Syntactic sugar for adding additional services to an existing GetServiceAsync call
		public virtual Task<(T1, T2, T3)> GetServiceAsync<T1, T2, T3>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class 
			where T3 : class 
			=> GetServicesAsync<T1, T2, T3>(cancellation);
		
		public virtual async Task<(T1, T2, T3)> GetServicesAsync<T1, T2, T3>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
		{
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			var linkedToken = linkedCts.Token;

			try
			{
				var task1 = GetServiceAsync<T1>(linkedToken);
				var task2 = GetServiceAsync<T2>(linkedToken);
				var task3 = GetServiceAsync<T3>(linkedToken);

				await Task.WhenAll(task1, task2, task3);
				return (await task1, await task2, await task3);
			}
			catch (OperationCanceledException)
			{
				// If any task is canceled, cancel all related requests
				linkedCts.Cancel();
				throw;
			}
		}
		
		//Syntactic sugar for adding additional services to an existing GetServiceAsync call
		public virtual Task<(T1, T2, T3, T4)> GetServiceAsync<T1, T2, T3, T4>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class 
			where T3 : class 
			where T4 : class 
			=> GetServicesAsync<T1, T2, T3, T4>(cancellation);
		
		public virtual async Task<(T1, T2, T3, T4)> GetServicesAsync<T1, T2, T3, T4>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			var linkedToken = linkedCts.Token;

			try
			{
				var task1 = GetServiceAsync<T1>(linkedToken);
				var task2 = GetServiceAsync<T2>(linkedToken);
				var task3 = GetServiceAsync<T3>(linkedToken);
				var task4 = GetServiceAsync<T4>(linkedToken);

				await Task.WhenAll(task1, task2, task3, task4);
				return (await task1, await task2, await task3, await task4);
			}
			catch (OperationCanceledException)
			{
				// If any task is canceled, cancel all related requests
				linkedCts.Cancel();
				throw;
			}
		}
		
		//Syntactic sugar for adding additional services to an existing GetServiceAsync call
		public virtual Task<(T1, T2, T3, T4, T5)> GetServiceAsync<T1, T2, T3, T4, T5>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class 
			where T3 : class 
			where T4 : class 
			where T5 : class 
			=> GetServicesAsync<T1, T2, T3, T4, T5>(cancellation);
		
		public virtual async Task<(T1, T2, T3, T4, T5)> GetServicesAsync<T1, T2, T3, T4, T5>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			var linkedToken = linkedCts.Token;

			try
			{
				var task1 = GetServiceAsync<T1>(linkedToken);
				var task2 = GetServiceAsync<T2>(linkedToken);
				var task3 = GetServiceAsync<T3>(linkedToken);
				var task4 = GetServiceAsync<T4>(linkedToken);
				var task5 = GetServiceAsync<T5>(linkedToken);

				await Task.WhenAll(task1, task2, task3, task4, task5);
				return (await task1, await task2, await task3, await task4, await task5);
			}
			catch (OperationCanceledException)
			{
				// If any task is canceled, cancel all related requests
				linkedCts.Cancel();
				throw;
			}
		}
		
		//Syntactic sugar for adding additional services to an existing GetServiceAsync call
		public virtual Task<(T1, T2, T3, T4, T5, T6)> GetServiceAsync<T1, T2, T3, T4, T5, T6>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class 
			where T3 : class 
			where T4 : class 
			where T5 : class 
			where T6 : class 
			=> GetServicesAsync<T1, T2, T3, T4, T5, T6>(cancellation);
		
		public virtual async Task<(T1, T2, T3, T4, T5, T6)> GetServicesAsync<T1, T2, T3, T4, T5, T6>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			var linkedToken = linkedCts.Token;

			try
			{
				var task1 = GetServiceAsync<T1>(linkedToken);
				var task2 = GetServiceAsync<T2>(linkedToken);
				var task3 = GetServiceAsync<T3>(linkedToken);
				var task4 = GetServiceAsync<T4>(linkedToken);
				var task5 = GetServiceAsync<T5>(linkedToken);
				var task6 = GetServiceAsync<T6>(linkedToken);

				await Task.WhenAll(task1, task2, task3, task4, task5, task6);
				return (await task1, await task2, await task3, await task4, await task5, await task6);
			}
			catch (OperationCanceledException)
			{
				// If any task is canceled, cancel all related requests
				linkedCts.Cancel();
				throw;
			}
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

			lock (Lock)
			{
				if (ServiceMap.TryGetValue(typeof(T), out var result) && result is T typedService)
				{
					service = typedService;
					return true;
				}

				return false;
			}
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
			var isPending = false;
			Action<object?> wrappedCallback = null!;

			lock (Lock)
			{
				if (ServiceMap.TryGetValue(serviceType, out var service))
				{
					callback((T)service);
					yield break;
				}

				wrappedCallback = obj => callback(obj as T);
				PendingCoroutines.Add((serviceType, wrappedCallback));
				isPending = true;
			}

			while (isPending)
			{
				lock (Lock)
				{
					if (ServiceMap.TryGetValue(serviceType, out var service))
					{
						PendingCoroutines.RemoveAll(x => x.Item1 == serviceType && x.Item2 == wrappedCallback);
						callback((T)service);
						yield break;
					}
					// Check if the coroutine is still pending after cleanup
					if (!PendingCoroutines.Any(x => x.Item1 == serviceType && x.Item2 == wrappedCallback))
					{
						isPending = false; // Exit if cleaned up
						yield break;
					}
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
			return TryGetService(out T? service) ? service : null;
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
		public virtual IServicePromise<T> GetService<T>(CancellationToken cancellation = default) where T : class
		{
			var promise = new ServicePromise<T>();
			var serviceType = typeof(T);

			lock (Lock)
			{
				if (ServiceMap.TryGetValue(serviceType, out var service))
				{
					promise.Resolve((T)service);
					return promise;
				}

				var taskCompletion = new TaskCompletionSource<object>();
				if (!PromiseMap.TryGetValue(serviceType, out var taskList))
				{
					taskList = new List<TaskCompletionSource<object>>();
					PromiseMap[serviceType] = taskList;
				}
				taskList.Add(taskCompletion);

				promise.BindTo(taskCompletion);

				// Add this line to use the new cancellation capability
				promise.WithCancellation(cancellation);

				if (cancellation.CanBeCanceled)
				{
					cancellation.Register(() =>
					{
						lock (Lock)
						{
							if (!taskList.Contains(taskCompletion)) return;
							
							taskCompletion.TrySetCanceled();
							taskList.Remove(taskCompletion);
							
							if (taskList.Count == 0)
							{
								PromiseMap.Remove(serviceType);
							}
						}
					});
				}

				taskCompletion.Task.ContinueWith(task =>
				{
					lock (Lock)
					{
						if (taskList.Contains(taskCompletion))
						{
							taskList.Remove(taskCompletion);
							if (taskList.Count == 0)
							{
								PromiseMap.Remove(serviceType);
							}
						}
					}

					if (task.IsCompletedSuccessfully)
					{
						promise.Resolve((T)task.Result);
					}
					else if (task.IsCanceled)
					{
						promise.Reject(new TaskCanceledException("Service retrieval was canceled"));
					}
					else if (task.IsFaulted)
					{
						promise.Reject(task.Exception ?? new Exception("Unknown error"));
					}
				}, cancellation);
			}

			return promise;
		}
		
		public virtual IServicePromise<(T1, T2)> GetService<T1, T2>(CancellationToken token = default)
			where T1 : class
			where T2 : class
		{
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token), 
				GetService<T2>(token));
		}

		public virtual IServicePromise<(T1, T2, T3)> GetService<T1, T2, T3>(CancellationToken token = default)
			where T1 : class
			where T2 : class
			where T3 : class
		{
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token), 
				GetService<T2>(token), 
				GetService<T3>(token));
		}
		
		public virtual IServicePromise<(T1, T2, T3, T4)> GetService<T1, T2, T3, T4>(CancellationToken token = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token), 
				GetService<T2>(token), 
				GetService<T3>(token), 
				GetService<T4>(token));
		}
		
		public virtual IServicePromise<(T1, T2, T3, T4, T5)> GetService<T1, T2, T3, T4, T5>(CancellationToken token = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token), 
				GetService<T2>(token), 
				GetService<T3>(token), 
				GetService<T4>(token), 
				GetService<T5>(token));
		}
		
		public virtual IServicePromise<(T1, T2, T3, T4, T5, T6)> GetService<T1, T2, T3, T4, T5, T6>(CancellationToken token = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token), 
				GetService<T2>(token), 
				GetService<T3>(token), 
				GetService<T4>(token), 
				GetService<T5>(token), 
				GetService<T6>(token));
		}

		/// <summary>
		/// Cleans up the ServiceLocator, clearing services, promises, and coroutines without affecting initialization state.
		/// </summary>
		public virtual void Cleanup()
		{
			lock (Lock)
			{
				//Dispose all disposable services
				foreach(var service in ServiceMap.Values)
				{
					if (service is IDisposable disposable)
					{
						disposable.Dispose();
					}
				}
				
				ServiceSceneMap.Clear();
				ServiceMap.Clear();
				CleanupPromises();
				
				CancelPendingCoroutines();
				NotifyChange();
			}
		}

		/// <summary>
		/// Initializes the ServiceLocator when the ScriptableObject is enabled.
		/// This method is called when the ScriptableObject is loaded or when entering play mode.
		/// </summary>
		protected virtual void OnEnable()
		{
			if (IsInitialized) return;
			Initialize();
		}

		/// <summary>
		/// Cleans up the ServiceLocator when the ScriptableObject is disabled.
		/// This method is called when the ScriptableObject is unloaded or when exiting play mode.
		/// </summary>
		protected virtual void OnDisable()
		{
			if (!IsInitialized) return;
			DeInitialize();
		}

		/// <summary>
		/// Initializes the ServiceLocator, setting up scene-based cleanup.
		/// </summary>
		protected virtual void Initialize()
		{
			if (IsInitialized) return;
			
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
			#endif
			
			SceneManager.sceneUnloaded += OnSceneUnloaded;
			IsInitialized = true;
		}

		/// <summary>
		/// Fully de-initializes the ServiceLocator, clearing all data and resetting its state.
		/// </summary>
		protected virtual void DeInitialize()
		{
			if (!IsInitialized) return;
			
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
			#endif
			
			SceneManager.sceneUnloaded -= OnSceneUnloaded;
			Cleanup();
			
			IsInitialized = false;
		}

		/// <summary>
		/// Cancels all pending coroutines.
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

		/// <summary>
		/// Cleans up promises when a scene is unloaded.
		/// </summary>
		protected virtual void OnSceneUnloaded(Scene scene)
		{
			lock (Lock)
			{
				// Just clean up promises and coroutines, but keep the scene map
				CleanupPromises();
				CancelPendingCoroutines();
		
				// Log which services are from the unloaded scene
				var servicesFromUnloadedScene = ServiceSceneMap
					.Where(kvp => kvp.Value == scene.name)
					.Select(kvp => kvp.Key.Name)
					.ToList();
			
				if (servicesFromUnloadedScene.Any())
				{
					Debug.Log($"Scene {scene.name} unloaded but these services remain: {string.Join(", ", servicesFromUnloadedScene)}");
					NotifyChange();
				}
			}
		}

		#if UNITY_EDITOR
		protected virtual void HandlePlayModeStateChanged(UnityEditor.PlayModeStateChange state)
		{
			if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
			DeInitialize();
		}
		#endif
		
	}
}