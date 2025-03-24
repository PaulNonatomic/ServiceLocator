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
	/// </summary>
	public abstract class BaseServiceLocator : ScriptableObject
	{
		public event Action? OnChange;
		public bool IsInitialized { get; protected set; } = false;

		[NonSerialized] protected readonly Dictionary<Type, object> ServiceMap = new();
		[NonSerialized] protected readonly Dictionary<Type, List<TaskCompletionSource<object>>> PromiseMap = new();
		[NonSerialized] protected readonly Dictionary<Type, string> ServiceSceneMap = new();
		[NonSerialized] protected readonly List<(Type, Action<object>)> PendingCoroutines = new();
		[NonSerialized] protected readonly object Lock = new();
		
		/// <summary>
		/// Returns a dictionary containing all currently registered services.
		/// </summary>
		public virtual IReadOnlyDictionary<Type, object> GetAllServices()
		{
			lock (Lock)
			{
				return new Dictionary<Type, object>(ServiceMap);
			}
		}
		
		/// <summary>
		/// Returns the scene name associated with a service type.
		/// </summary>
		public virtual string GetSceneNameForService(Type serviceType)
		{
			lock (Lock)
			{
				if (!ServiceSceneMap.TryGetValue(serviceType, out var sceneName))
					return "No Scene";
				
				return sceneName;
			}
		}

		/// <summary>
		/// Cancels and removes all unfulfilled promises.
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
		/// Rejects all pending promises for a specific service type with an exception.
		/// </summary>
		public virtual void RejectService<T>(Exception exception) where T : class
		{
			if (exception == null) throw new ArgumentNullException(nameof(exception));

			lock (Lock)
			{
				var serviceType = typeof(T);
				if (!PromiseMap.TryGetValue(serviceType, out var taskList)) 
					return;
				
				foreach (var tcs in taskList.ToList())
				{
					tcs.TrySetException(exception);
				}
				PromiseMap.Remove(serviceType);
			}
		}

		/// <summary>
		/// Registers a service with the service locator.
		/// </summary>
		/// <typeparam name="T">The type of the service being registered.</typeparam>
		/// <param name="service">The instance of the service to register.</param>
		public virtual void Register<T>(T service) where T : class
		{
			lock (Lock)
			{
				if (service == null)
					throw new ArgumentNullException("service", "Cannot register a null service.");

				var serviceType = typeof(T);
				ServiceMap[serviceType] = service;
				
				// Track scene information for this service
				var sceneName = "No Scene";
				if (service is MonoBehaviour monoBehaviour)
					sceneName = monoBehaviour.gameObject.scene.name;
				
				ServiceSceneMap[serviceType] = sceneName;

				// Resolve any pending promises for this service
				if (PromiseMap.TryGetValue(serviceType, out var taskCompletions))
				{
					foreach (var tcs in taskCompletions.ToList())
					{
						tcs.TrySetResult(service);
					}
					PromiseMap.Remove(serviceType);
				}

				// Notify any pending coroutines
				var pendingCoroutines = PendingCoroutines.FindAll(pendingCoroutine => pendingCoroutine.Item1 == serviceType);
				foreach (var (_, callback) in pendingCoroutines)
				{
					callback(service);
				}
				PendingCoroutines.RemoveAll(pendingCoroutine => pendingCoroutine.Item1 == serviceType);

				NotifyChange();
			}
		}

		/// <summary>
		/// Invokes the OnChange event.
		/// </summary>
		protected virtual void NotifyChange()
		{
			OnChange?.Invoke();
		}

		/// <summary>
		/// Unregisters a service from the service locator.
		/// </summary>
		/// <typeparam name="T">The type of the service to unregister.</typeparam>
		public virtual void Unregister<T>() where T : class
		{
			lock (Lock)
			{
				var serviceType = typeof(T);
				ServiceMap.Remove(serviceType);
				ServiceSceneMap.Remove(serviceType);
				
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
				
				NotifyChange();
			}
		}

		/// <summary>
		/// Asynchronously retrieves a service of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <returns>A Task that resolves to the requested service instance.</returns>
		public virtual async Task<T> GetServiceAsync<T>(CancellationToken cancellation = default) where T : class
		{
			var serviceType = typeof(T);
			TaskCompletionSource<object> taskCompletion;

			lock (Lock)
			{
				if (ServiceMap.TryGetValue(serviceType, out var service))
					return (T)service;

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
						if (!PromiseMap.TryGetValue(serviceType, out var taskList) || !taskList.Contains(taskCompletion)) 
							return;
						
						taskCompletion.TrySetCanceled();
						taskList.Remove(taskCompletion);
						if (taskList.Count != 0) 
							return;
						
						PromiseMap.Remove(serviceType);
					}
				});
			}

			return (T)await taskCompletion.Task;
		}

		/// <summary>
		/// Asynchronously retrieves two services of the specified types.
		/// </summary>
		public virtual Task<(T1, T2)> GetServiceAsync<T1, T2>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class 
			=> GetServicesAsync<T1, T2>(cancellation);

		/// <summary>
		/// Asynchronously retrieves two services of the specified types.
		/// </summary>
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
				linkedCts.Cancel();
				throw;
			}
		}
		
		/// <summary>
		/// Asynchronously retrieves three services of the specified types.
		/// </summary>
		public virtual Task<(T1, T2, T3)> GetServiceAsync<T1, T2, T3>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class 
			where T3 : class 
			=> GetServicesAsync<T1, T2, T3>(cancellation);
		
		/// <summary>
		/// Asynchronously retrieves three services of the specified types.
		/// </summary>
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
				linkedCts.Cancel();
				throw;
			}
		}
		
		/// <summary>
		/// Asynchronously retrieves four services of the specified types.
		/// </summary>
		public virtual Task<(T1, T2, T3, T4)> GetServiceAsync<T1, T2, T3, T4>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class 
			where T3 : class 
			where T4 : class 
			=> GetServicesAsync<T1, T2, T3, T4>(cancellation);
		
		/// <summary>
		/// Asynchronously retrieves four services of the specified types.
		/// </summary>
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
				linkedCts.Cancel();
				throw;
			}
		}
		
		/// <summary>
		/// Asynchronously retrieves five services of the specified types.
		/// </summary>
		public virtual Task<(T1, T2, T3, T4, T5)> GetServiceAsync<T1, T2, T3, T4, T5>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class 
			where T3 : class 
			where T4 : class 
			where T5 : class 
			=> GetServicesAsync<T1, T2, T3, T4, T5>(cancellation);
		
		/// <summary>
		/// Asynchronously retrieves five services of the specified types.
		/// </summary>
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
				linkedCts.Cancel();
				throw;
			}
		}
		
		/// <summary>
		/// Asynchronously retrieves six services of the specified types.
		/// </summary>
		public virtual Task<(T1, T2, T3, T4, T5, T6)> GetServiceAsync<T1, T2, T3, T4, T5, T6>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class 
			where T3 : class 
			where T4 : class 
			where T5 : class 
			where T6 : class 
			=> GetServicesAsync<T1, T2, T3, T4, T5, T6>(cancellation);
		
		/// <summary>
		/// Asynchronously retrieves six services of the specified types.
		/// </summary>
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
				if (!ServiceMap.TryGetValue(typeof(T), out var result) || result is not T typedService)
					return false;
				
				service = typedService;
				return true;
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
					
					if (!PendingCoroutines.Any(x => x.Item1 == serviceType && x.Item2 == wrappedCallback))
					{
						isPending = false;
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
				promise.WithCancellation(cancellation);

				if (cancellation.CanBeCanceled)
				{
					cancellation.Register(() =>
					{
						lock (Lock)
						{
							if (!taskList.Contains(taskCompletion)) 
								return;
							
							taskCompletion.TrySetCanceled();
							taskList.Remove(taskCompletion);
							
							if (taskList.Count != 0) 
								return;
							
							PromiseMap.Remove(serviceType);
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
		
		/// <summary>
		/// Retrieves two services using a promise-based approach.
		/// </summary>
		public virtual IServicePromise<(T1, T2)> GetService<T1, T2>(CancellationToken token = default)
			where T1 : class
			where T2 : class
		{
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token), 
				GetService<T2>(token));
		}

		/// <summary>
		/// Retrieves three services using a promise-based approach.
		/// </summary>
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
		
		/// <summary>
		/// Retrieves four services using a promise-based approach.
		/// </summary>
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
		
		/// <summary>
		/// Retrieves five services using a promise-based approach.
		/// </summary>
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
		
		/// <summary>
		/// Retrieves six services using a promise-based approach.
		/// </summary>
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
		/// Cleans up the ServiceLocator, clearing services, promises, and coroutines.
		/// </summary>
		public virtual void Cleanup()
		{
			lock (Lock)
			{
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
		/// Initializes the ServiceLocator when enabled.
		/// </summary>
		protected virtual void OnEnable()
		{
			if (IsInitialized) 
				return;
			
			Initialize();
		}

		/// <summary>
		/// Cleans up the ServiceLocator when disabled.
		/// </summary>
		protected virtual void OnDisable()
		{
			if (!IsInitialized) 
				return;
			
			DeInitialize();
		}

		/// <summary>
		/// Initializes the ServiceLocator.
		/// </summary>
		protected virtual void Initialize()
		{
			if (IsInitialized) 
				return;
			
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
			#endif
			
			SceneManager.sceneUnloaded += HandleSceneUnloaded;
			IsInitialized = true;
		}

		/// <summary>
		/// De-initializes the ServiceLocator.
		/// </summary>
		protected virtual void DeInitialize()
		{
			if (!IsInitialized) 
				return;
			
			#if UNITY_EDITOR
			UnityEditor.EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
			#endif
			
			SceneManager.sceneUnloaded -= HandleSceneUnloaded;
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
		/// Handles when a scene is unloaded.
		/// </summary>
		protected virtual void HandleSceneUnloaded(Scene scene)
		{
			lock (Lock)
			{
				UnregisterServicesFromScene(scene.name);
			}
		}
		
		/// <summary>
		/// Unregisters all services that belong to a specific scene that has been unloaded.
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

				if (servicesToRemove.Count == 0) return;
				
				Debug.LogWarning($"Detected: {servicesToRemove.Count} services remain in unloaded scene: {sceneName}");
				
				foreach (var serviceType in servicesToRemove)
				{
					ServiceMap.Remove(serviceType);
					ServiceSceneMap.Remove(serviceType);
			
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
					
					Debug.LogWarning($"Unregistered {serviceType.Name} from unloaded scene {sceneName}");
				}
		
				NotifyChange();
			}
		}

		#if UNITY_EDITOR
		/// <summary>
		/// Handles play mode state changes in the editor.
		/// </summary>
		protected virtual void HandlePlayModeStateChanged(UnityEditor.PlayModeStateChange state)
		{
			if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
			
			DeInitialize();
		}
		#endif
	}
}