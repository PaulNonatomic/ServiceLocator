#if !DISABLE_SL_UNITASK && ENABLE_UNITASK

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nonatomic.ServiceLocator
{
	public abstract partial class BaseServiceLocator
	{
		[NonSerialized]
		protected readonly Dictionary<Type, List<UniTaskCompletionSource<object>>> UniTaskPromiseMap = new();

		/// <summary>
		///     Cancels and removes all unfulfilled UniTask promises.
		/// </summary>
		public virtual void CleanupUniTaskPromises()
		{
			lock (Lock)
			{
				foreach (var promiseList in UniTaskPromiseMap.Values)
				{
					foreach (var promise in promiseList.ToList())
					{
						promise.TrySetCanceled();
					}
				}

				UniTaskPromiseMap.Clear();
			}
		}

		/// <summary>
		///     Rejects all pending UniTask promises for a specific service type with an exception.
		/// </summary>
		public virtual void RejectServiceUniTask<T>(Exception exception) where T : class
		{
			if (exception == null)
			{
				throw new ArgumentNullException(nameof(exception));
			}

			lock (Lock)
			{
				var serviceType = typeof(T);
				if (!UniTaskPromiseMap.TryGetValue(serviceType, out var taskList))
				{
					return;
				}

				foreach (var tcs in taskList.ToList())
				{
					tcs.TrySetException(exception);
				}

				UniTaskPromiseMap.Remove(serviceType);
			}
		}

		/// <summary>
		///     Asynchronously retrieves a service of the specified type using UniTask.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <returns>A UniTask that resolves to the requested service instance.</returns>
		public virtual async UniTask<T> GetServiceUniTask<T>(CancellationToken cancellation = default) where T : class
		{
			var serviceType = typeof(T);
			UniTaskCompletionSource<object> taskCompletion;

			lock (Lock)
			{
				if (ServiceMap.TryGetValue(serviceType, out var service))
				{
					return (T)service;
				}

				taskCompletion = new();
				if (!UniTaskPromiseMap.TryGetValue(serviceType, out var taskList))
				{
					taskList = new();
					UniTaskPromiseMap[serviceType] = taskList;
				}

				taskList.Add(taskCompletion);
			}

			if (cancellation.CanBeCanceled)
			{
				cancellation.Register(() =>
				{
					lock (Lock)
					{
						if (!UniTaskPromiseMap.TryGetValue(serviceType, out var taskList) ||
							!taskList.Contains(taskCompletion))
						{
							return;
						}

						taskCompletion.TrySetCanceled(cancellation);
						taskList.Remove(taskCompletion);
						if (taskList.Count != 0)
						{
							return;
						}

						UniTaskPromiseMap.Remove(serviceType);
					}
				});
			}

			return (T)await taskCompletion.Task;
		}

		/// <summary>
		///     Asynchronously retrieves two services of the specified types using UniTask.
		/// </summary>
		public virtual UniTask<(T1, T2)> GetServiceUniTask<T1, T2>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
		{
			return GetServicesUniTask<T1, T2>(cancellation);
		}

		/// <summary>
		///     Asynchronously retrieves two services of the specified types using UniTask.
		/// </summary>
		public virtual async UniTask<(T1, T2)> GetServicesUniTask<T1, T2>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
		{
			// Create individual tasks
			var task1 = GetServiceUniTask<T1>(cancellation);
			var task2 = GetServiceUniTask<T2>(cancellation);

			// Use individual awaits instead of WhenAll to avoid issues with multiple awaits
			var result1 = await task1;
			var result2 = await task2;

			return (result1, result2);
		}

		/// <summary>
		///     Asynchronously retrieves three services of the specified types using UniTask.
		/// </summary>
		public virtual UniTask<(T1, T2, T3)> GetServiceUniTask<T1, T2, T3>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
		{
			return GetServicesUniTask<T1, T2, T3>(cancellation);
		}

		/// <summary>
		///     Asynchronously retrieves three services of the specified types using UniTask.
		/// </summary>
		public virtual async UniTask<(T1, T2, T3)> GetServicesUniTask<T1, T2, T3>(
			CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
		{
			// Create individual tasks
			var task1 = GetServiceUniTask<T1>(cancellation);
			var task2 = GetServiceUniTask<T2>(cancellation);
			var task3 = GetServiceUniTask<T3>(cancellation);

			// Use individual awaits instead of WhenAll to avoid issues with multiple awaits
			var result1 = await task1;
			var result2 = await task2;
			var result3 = await task3;

			return (result1, result2, result3);
		}

		/// <summary>
		///     Asynchronously retrieves four services of the specified types using UniTask.
		/// </summary>
		public virtual UniTask<(T1, T2, T3, T4)> GetServiceUniTask<T1, T2, T3, T4>(
			CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			return GetServicesUniTask<T1, T2, T3, T4>(cancellation);
		}

		/// <summary>
		///     Asynchronously retrieves four services of the specified types using UniTask.
		/// </summary>
		public virtual async UniTask<(T1, T2, T3, T4)> GetServicesUniTask<T1, T2, T3, T4>(
			CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			// Create individual tasks
			var task1 = GetServiceUniTask<T1>(cancellation);
			var task2 = GetServiceUniTask<T2>(cancellation);
			var task3 = GetServiceUniTask<T3>(cancellation);
			var task4 = GetServiceUniTask<T4>(cancellation);

			// Use individual awaits instead of WhenAll to avoid issues with multiple awaits
			var result1 = await task1;
			var result2 = await task2;
			var result3 = await task3;
			var result4 = await task4;

			return (result1, result2, result3, result4);
		}

		/// <summary>
		///     Asynchronously retrieves five services of the specified types using UniTask.
		/// </summary>
		public virtual UniTask<(T1, T2, T3, T4, T5)> GetServiceUniTask<T1, T2, T3, T4, T5>(
			CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			return GetServicesUniTask<T1, T2, T3, T4, T5>(cancellation);
		}

		/// <summary>
		///     Asynchronously retrieves five services of the specified types using UniTask.
		/// </summary>
		public virtual async UniTask<(T1, T2, T3, T4, T5)> GetServicesUniTask<T1, T2, T3, T4, T5>(
			CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			// Create individual tasks
			var task1 = GetServiceUniTask<T1>(cancellation);
			var task2 = GetServiceUniTask<T2>(cancellation);
			var task3 = GetServiceUniTask<T3>(cancellation);
			var task4 = GetServiceUniTask<T4>(cancellation);
			var task5 = GetServiceUniTask<T5>(cancellation);

			// Use individual awaits instead of WhenAll to avoid issues with multiple awaits
			var result1 = await task1;
			var result2 = await task2;
			var result3 = await task3;
			var result4 = await task4;
			var result5 = await task5;

			return (result1, result2, result3, result4, result5);
		}

		/// <summary>
		///     Asynchronously retrieves six services of the specified types using UniTask.
		/// </summary>
		public virtual UniTask<(T1, T2, T3, T4, T5, T6)> GetServiceUniTask<T1, T2, T3, T4, T5, T6>(
			CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			return GetServicesUniTask<T1, T2, T3, T4, T5, T6>(cancellation);
		}

		/// <summary>
		///     Asynchronously retrieves six services of the specified types using UniTask.
		/// </summary>
		public virtual async UniTask<(T1, T2, T3, T4, T5, T6)> GetServicesUniTask<T1, T2, T3, T4, T5, T6>(
			CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			// Create individual tasks
			var task1 = GetServiceUniTask<T1>(cancellation);
			var task2 = GetServiceUniTask<T2>(cancellation);
			var task3 = GetServiceUniTask<T3>(cancellation);
			var task4 = GetServiceUniTask<T4>(cancellation);
			var task5 = GetServiceUniTask<T5>(cancellation);
			var task6 = GetServiceUniTask<T6>(cancellation);

			// Use individual awaits instead of WhenAll to avoid issues with multiple awaits
			var result1 = await task1;
			var result2 = await task2;
			var result3 = await task3;
			var result4 = await task4;
			var result5 = await task5;
			var result6 = await task6;

			return (result1, result2, result3, result4, result5, result6);
		}
	}
}
#endif