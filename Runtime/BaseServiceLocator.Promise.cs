#if !DISABLE_SL_PROMISES

#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// Required for List

// Required for Debug

namespace Nonatomic.ServiceLocator
{
	///BaseServiceLocator.Promise.cs
	public abstract partial class BaseServiceLocator
	{
		/// <summary>
		///     Retrieves a service using a promise-based approach.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <param name="cancellation">Optional cancellation token.</param>
		/// <param name="timeout">Optional timeout period.</param>
		/// <returns>An IServicePromise that will resolve with the requested service.</returns>
		public virtual IServicePromise<T> GetService<T>(CancellationToken cancellation = default,
			TimeSpan? timeout = null) where T : class
		{
			var promise = new ServicePromise<T>();
			var serviceType = typeof(T);
			TaskCompletionSource<object> tcs;

			var ctsToDispose = new List<CancellationTokenSource>();

			lock (Lock)
			{
				if (ServiceMap.TryGetValue(serviceType, out var service))
				{
					promise.Resolve((T)service);
					return promise;
				}

				tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
				if (!PromiseMap.TryGetValue(serviceType, out var taskList))
				{
					taskList = new();
					PromiseMap[serviceType] = taskList;
				}

				taskList.Add(tcs);
				promise.BindTo(tcs);
			}

			CancellationTokenSource? timeoutCts = null;
			if (timeout.HasValue)
			{
				timeoutCts = new();
				ctsToDispose.Add(timeoutCts);
			}

			CancellationTokenSource? linkedCts = null;
			var finalToken = cancellation;

			if (timeoutCts != null)
			{
				linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation, timeoutCts.Token);
				finalToken = linkedCts.Token;
				ctsToDispose.Add(linkedCts);
			}

			CancellationTokenRegistration registration = default;
			if (finalToken.CanBeCanceled)
			{
				registration = finalToken.Register(() =>
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
								tcs.TrySetCanceled(cancellation.IsCancellationRequested
									? cancellation
									: CancellationToken.None);
							}
						}
					}
				});
			}

			if (timeoutCts != null && timeout.HasValue) // Ensure timeout has value for CancelAfter
			{
				timeoutCts.CancelAfter(timeout.Value);
			}

			tcs.Task.ContinueWith(task =>
			{
				registration.Dispose();
				foreach (var cts in ctsToDispose)
				{
					cts.Dispose();
				}

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
			}, TaskContinuationOptions.ExecuteSynchronously);

			return promise;
		}

		/// <summary>
		///     Retrieves two services using a promise-based approach.
		/// </summary>
		/// <param name="token">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual IServicePromise<(T1, T2)> GetService<T1, T2>(CancellationToken token = default,
			TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
		{
			// Pass timeout to individual GetService calls
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token, timeout),
				GetService<T2>(token, timeout),
				token // Pass the original token to CombinePromises for its own linked CTS if needed
			);
		}

		/// <summary>
		///     Retrieves three services using a promise-based approach.
		/// </summary>
		/// <param name="token">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual IServicePromise<(T1, T2, T3)> GetService<T1, T2, T3>(CancellationToken token = default,
			TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
		{
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token, timeout),
				GetService<T2>(token, timeout),
				GetService<T3>(token, timeout),
				token
			);
		}

		/// <summary>
		///     Retrieves four services using a promise-based approach.
		/// </summary>
		/// <param name="token">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual IServicePromise<(T1, T2, T3, T4)> GetService<T1, T2, T3, T4>(CancellationToken token = default,
			TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token, timeout),
				GetService<T2>(token, timeout),
				GetService<T3>(token, timeout),
				GetService<T4>(token, timeout),
				token
			);
		}

		/// <summary>
		///     Retrieves five services using a promise-based approach.
		/// </summary>
		/// <param name="token">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual IServicePromise<(T1, T2, T3, T4, T5)> GetService<T1, T2, T3, T4, T5>(
			CancellationToken token = default, TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token, timeout),
				GetService<T2>(token, timeout),
				GetService<T3>(token, timeout),
				GetService<T4>(token, timeout),
				GetService<T5>(token, timeout),
				token
			);
		}

		/// <summary>
		///     Retrieves six services using a promise-based approach.
		/// </summary>
		/// <param name="token">Optional cancellation token for the aggregate operation.</param>
		/// <param name="timeout">Optional timeout for each individual service retrieval.</param>
		public virtual IServicePromise<(T1, T2, T3, T4, T5, T6)> GetService<T1, T2, T3, T4, T5, T6>(
			CancellationToken token = default, TimeSpan? timeout = null)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			return ServicePromiseCombiner.CombinePromises(
				GetService<T1>(token, timeout),
				GetService<T2>(token, timeout),
				GetService<T3>(token, timeout),
				GetService<T4>(token, timeout),
				GetService<T5>(token, timeout),
				GetService<T6>(token, timeout),
				token
			);
		}
	}
}
#endif