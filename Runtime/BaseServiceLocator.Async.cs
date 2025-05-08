#if !DISABLE_SL_ASYNC

#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
// Required for List

// Required for Debug

namespace Nonatomic.ServiceLocator
{
	///BaseServiceLocator.Async.cs
	public abstract partial class BaseServiceLocator
	{
		/// <summary>
		///     Asynchronously retrieves a service of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <param name="cancellation">Optional cancellation token.</param>
		/// <param name="timeout">Optional timeout period.</param>
		/// <returns>A Task that resolves to the requested service instance.</returns>
		/// <exception cref="TimeoutException">Thrown if the timeout period elapses before the service is available.</exception>
		/// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via the cancellation token.</exception>
		public virtual async Task<T> GetServiceAsync<T>(CancellationToken cancellation = default,
			TimeSpan? timeout = null) where T : class
		{
			var serviceType = typeof(T);
			TaskCompletionSource<object> tcs;

			lock (Lock)
			{
				if (ServiceMap.TryGetValue(serviceType, out var service))
				{
					return (T)service;
				}

				tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
				if (!PromiseMap.TryGetValue(serviceType, out var taskList))
				{
					taskList = new();
					PromiseMap[serviceType] = taskList;
				}

				taskList.Add(tcs);
			}

			// Resources to dispose
			CancellationTokenSource? timeoutCts = null;
			CancellationTokenSource? linkedCts = null;
			CancellationTokenRegistration linkedTokenRegistration = default;

			if (timeout.HasValue)
			{
				timeoutCts = new();
			}

			var effectiveToken = cancellation;
			if (timeoutCts != null)
			{
				linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, timeoutCts.Token);
				effectiveToken = linkedCts.Token;
			}

			if (effectiveToken.CanBeCanceled)
			{
				linkedTokenRegistration = effectiveToken.Register(() =>
				{
					var wasTimeout = timeoutCts?.IsCancellationRequested ?? false;
					lock (Lock)
					{
						if (PromiseMap.TryGetValue(serviceType, out var taskList) && taskList.Contains(tcs) &&
							!tcs.Task.IsCompleted)
						{
							if (wasTimeout)
							{
								tcs.TrySetException(new TimeoutException(
									$"Service {serviceType.Name} retrieval timed out after {timeout!.Value}."));
							}
							else
							{
								tcs.TrySetCanceled(cancellation);
							}
							// Removal from PromiseMap is handled in the finally block of this method.
						}
					}
				});
			}

			if (timeoutCts != null && timeout.HasValue) // Ensure timeout has value for CancelAfter
			{
				timeoutCts.CancelAfter(timeout.Value);
			}

			try
			{
				return (T)await tcs.Task;
			}
			finally
			{
				linkedTokenRegistration.Dispose();
				linkedCts?.Dispose();
				timeoutCts?.Dispose();

				lock (Lock)
				{
					if (PromiseMap.TryGetValue(serviceType, out var taskList))
					{
						taskList.Remove(tcs);
						if (taskList.Count == 0)
						{
							PromiseMap.Remove(serviceType);
						}
					}
				}
			}
		}

		/// <summary>
		///     Asynchronously retrieves two services of the specified types.
		/// </summary>
		/// <param name="cancellation">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual Task<(T1, T2)> GetServiceAsync<T1, T2>(CancellationToken cancellation = default,
			TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
		{
			// Pass the timeout to the GetServicesAsync overload
			return GetServicesAsync<T1, T2>(cancellation, timeout);
		}

		/// <summary>
		///     Asynchronously retrieves two services of the specified types.
		/// </summary>
		/// <param name="cancellation">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual async Task<(T1, T2)> GetServicesAsync<T1, T2>(CancellationToken cancellation = default,
			TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
		{
			// Each GetServiceAsync call will handle its own timeout and cancellation linking.
			// The 'cancellation' token is passed to each.
			var task1 = GetServiceAsync<T1>(cancellation, timeout);
			var task2 = GetServiceAsync<T2>(cancellation, timeout);

			await Task.WhenAll(task1, task2);
			return (await task1, await task2);
		}

		/// <summary>
		///     Asynchronously retrieves three services of the specified types.
		/// </summary>
		/// <param name="cancellation">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual Task<(T1, T2, T3)> GetServiceAsync<T1, T2, T3>(CancellationToken cancellation = default,
			TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
		{
			return GetServicesAsync<T1, T2, T3>(cancellation, timeout);
		}

		public virtual async Task<(T1, T2, T3)> GetServicesAsync<T1, T2, T3>(CancellationToken cancellation = default,
			TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
		{
			var task1 = GetServiceAsync<T1>(cancellation, timeout);
			var task2 = GetServiceAsync<T2>(cancellation, timeout);
			var task3 = GetServiceAsync<T3>(cancellation, timeout);

			await Task.WhenAll(task1, task2, task3);
			return (await task1, await task2, await task3);
		}

		/// <summary>
		///     Asynchronously retrieves four services of the specified types.
		/// </summary>
		/// <param name="cancellation">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual Task<(T1, T2, T3, T4)> GetServiceAsync<T1, T2, T3, T4>(CancellationToken cancellation = default,
			TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			return GetServicesAsync<T1, T2, T3, T4>(cancellation, timeout);
		}

		public virtual async Task<(T1, T2, T3, T4)> GetServicesAsync<T1, T2, T3, T4>(
			CancellationToken cancellation = default, TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			var task1 = GetServiceAsync<T1>(cancellation, timeout);
			var task2 = GetServiceAsync<T2>(cancellation, timeout);
			var task3 = GetServiceAsync<T3>(cancellation, timeout);
			var task4 = GetServiceAsync<T4>(cancellation, timeout);

			await Task.WhenAll(task1, task2, task3, task4);
			return (await task1, await task2, await task3, await task4);
		}

		/// <summary>
		///     Asynchronously retrieves five services of the specified types.
		/// </summary>
		/// <param name="cancellation">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual Task<(T1, T2, T3, T4, T5)> GetServiceAsync<T1, T2, T3, T4, T5>(
			CancellationToken cancellation = default, TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			return GetServicesAsync<T1, T2, T3, T4, T5>(cancellation, timeout);
		}

		public virtual async Task<(T1, T2, T3, T4, T5)> GetServicesAsync<T1, T2, T3, T4, T5>(
			CancellationToken cancellation = default, TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			var task1 = GetServiceAsync<T1>(cancellation, timeout);
			var task2 = GetServiceAsync<T2>(cancellation, timeout);
			var task3 = GetServiceAsync<T3>(cancellation, timeout);
			var task4 = GetServiceAsync<T4>(cancellation, timeout);
			var task5 = GetServiceAsync<T5>(cancellation, timeout);

			await Task.WhenAll(task1, task2, task3, task4, task5);
			return (await task1, await task2, await task3, await task4, await task5);
		}

		/// <summary>
		///     Asynchronously retrieves six services of the specified types.
		/// </summary>
		/// <param name="cancellation">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual Task<(T1, T2, T3, T4, T5, T6)> GetServiceAsync<T1, T2, T3, T4, T5, T6>(
			CancellationToken cancellation = default, TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			return GetServicesAsync<T1, T2, T3, T4, T5, T6>(cancellation, timeout);
		}

		public virtual async Task<(T1, T2, T3, T4, T5, T6)> GetServicesAsync<T1, T2, T3, T4, T5, T6>(
			CancellationToken cancellation = default, TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			var task1 = GetServiceAsync<T1>(cancellation, timeout);
			var task2 = GetServiceAsync<T2>(cancellation, timeout);
			var task3 = GetServiceAsync<T3>(cancellation, timeout);
			var task4 = GetServiceAsync<T4>(cancellation, timeout);
			var task5 = GetServiceAsync<T5>(cancellation, timeout);
			var task6 = GetServiceAsync<T6>(cancellation, timeout);

			await Task.WhenAll(task1, task2, task3, task4, task5, task6);
			return (await task1, await task2, await task3, await task4, await task5, await task6);
		}
	}
}
#endif