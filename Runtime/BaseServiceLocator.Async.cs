#if !DISABLE_SL_ASYNC && (!ENABLE_UNITASK || DISABLE_SL_UNITASK)

#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nonatomic.ServiceLocator
{
	/// <summary>
	///     Provides standard System.Threading.Tasks-based asynchronous methods
	///     for retrieving services from the BaseServiceLocator.
	/// </summary>
	public abstract partial class BaseServiceLocator
	{
		/// <summary>
		///     Asynchronously retrieves a service of the specified type using Task.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <param name="cancellation">Optional cancellation token.</param>
		/// <returns>A Task that resolves to the requested service instance.</returns>
		public virtual async Task<T> GetServiceAsync<T>(CancellationToken cancellation = default) where T : class
		{
			var serviceType = typeof(T);
			TaskCompletionSource<object> taskCompletion;

			lock (Lock)
			{
				// Check if service is already registered
				if (ServiceMap.TryGetValue(serviceType, out var service))
				{
					return (T)service;
				}

				// Service not found, create or retrieve a TaskCompletionSource
				taskCompletion =
					new(TaskCreationOptions
						.RunContinuationsAsynchronously); // Ensure continuations don't block the lock
				if (!PromiseMap.TryGetValue(serviceType, out var taskList))
				{
					taskList = new();
					PromiseMap[serviceType] = taskList;
				}

				taskList.Add(taskCompletion);
			}

			// Handle cancellation outside the lock
			CancellationTokenRegistration cancellationRegistration = default;
			if (cancellation.CanBeCanceled)
			{
				// Register callback to handle cancellation
				cancellationRegistration = cancellation.Register(() =>
				{
					lock (Lock)
					{
						// Check if the TCS still exists and is pending before attempting cancellation
						if (PromiseMap.TryGetValue(serviceType, out var taskList) && taskList.Contains(taskCompletion))
						{
							// Try to cancel the task and remove it from the list
							taskCompletion.TrySetCanceled(cancellation);
							taskList.Remove(taskCompletion);
							if (taskList.Count == 0)
							{
								PromiseMap.Remove(serviceType); // Clean up dictionary entry if list is empty
							}
						}
						// If not found, it was likely already resolved or cancelled differently
					}
				});
			}

			try
			{
				// Await the completion source's task
				return (T)await taskCompletion.Task;
			}
			finally
			{
				// Dispose the cancellation registration if it was created
				#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER // Or appropriate framework check
                    await cancellationRegistration.DisposeAsync();
				#else
				cancellationRegistration.Dispose();
				#endif

				// Clean up the list entry after the task completes (successfully, faulted, or cancelled by external source other than token)
				// This handles cases where the task completes before the cancellation token is triggered.
				lock (Lock)
				{
					if (PromiseMap.TryGetValue(serviceType, out var taskList) && taskList.Contains(taskCompletion))
					{
						taskList.Remove(taskCompletion);
						if (taskList.Count == 0)
						{
							PromiseMap.Remove(serviceType);
						}
					}
				}
			}
		}

		/// <summary>
		///     Asynchronously retrieves two services of the specified types using Task.
		///     This is an overload calling the GetServicesAsync implementation.
		/// </summary>
		/// <typeparam name="T1">Type of the first service.</typeparam>
		/// <typeparam name="T2">Type of the second service.</typeparam>
		/// <param name="cancellation">Optional cancellation token.</param>
		/// <returns>A Task resolving to a tuple containing the two service instances.</returns>
		public virtual Task<(T1, T2)> GetServiceAsync<T1, T2>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
		{
			// Calls the specific implementation method below
			return GetServicesAsync<T1, T2>(cancellation);
		}

		/// <summary>
		///     Implementation for asynchronously retrieving two services of the specified types using Task.
		///     Uses Task.WhenAll to retrieve services concurrently.
		/// </summary>
		public virtual async Task<(T1, T2)> GetServicesAsync<T1, T2>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
		{
			// Using a linked token source ensures that if one GetServiceAsync call is cancelled
			// (or the external token is cancelled), the other task is also cancelled promptly.
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			var linkedToken = linkedCts.Token;

			try
			{
				// Start both tasks concurrently
				var task1 = GetServiceAsync<T1>(linkedToken);
				var task2 = GetServiceAsync<T2>(linkedToken);

				// Await both tasks completing
				await Task.WhenAll(task1, task2);

				// Return the results once both are available
				// Accessing .Result is safe here because we've awaited Task.WhenAll
				return (task1.Result, task2.Result);
			}
			catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
			{
				// If the original token caused cancellation, just rethrow.
				throw;
			}
			catch (Exception)
			{
				// If any task fails or is cancelled by the linked token (due to timeout/external cancel),
				// ensure the linked CTS is cancelled to stop the other task if it's still running.
				// Task.WhenAll will throw the first exception encountered.
				if (!linkedCts.IsCancellationRequested)
				{
					linkedCts.Cancel();
				}

				throw; // Re-throw the exception caught by WhenAll (or the cancellation)
			}
		}

		/// <summary>
		///     Asynchronously retrieves three services of the specified types using Task.
		/// </summary>
		public virtual Task<(T1, T2, T3)> GetServiceAsync<T1, T2, T3>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
		{
			return GetServicesAsync<T1, T2, T3>(cancellation);
		}

		/// <summary>
		///     Implementation for asynchronously retrieving three services using Task.WhenAll.
		/// </summary>
		public virtual async Task<(T1, T2, T3)> GetServicesAsync<T1, T2, T3>(
			CancellationToken cancellation = default)
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
				return (task1.Result, task2.Result, task3.Result);
			}
			catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception)
			{
				if (!linkedCts.IsCancellationRequested)
				{
					linkedCts.Cancel();
				}

				throw;
			}
		}

		/// <summary>
		///     Asynchronously retrieves four services of the specified types using Task.
		/// </summary>
		public virtual Task<(T1, T2, T3, T4)> GetServiceAsync<T1, T2, T3, T4>(CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			return GetServicesAsync<T1, T2, T3, T4>(cancellation);
		}

		/// <summary>
		///     Implementation for asynchronously retrieving four services using Task.WhenAll.
		/// </summary>
		public virtual async Task<(T1, T2, T3, T4)> GetServicesAsync<T1, T2, T3, T4>(
			CancellationToken cancellation = default)
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
				return (task1.Result, task2.Result, task3.Result, task4.Result);
			}
			catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception)
			{
				if (!linkedCts.IsCancellationRequested)
				{
					linkedCts.Cancel();
				}

				throw;
			}
		}

		/// <summary>
		///     Asynchronously retrieves five services of the specified types using Task.
		/// </summary>
		public virtual Task<(T1, T2, T3, T4, T5)> GetServiceAsync<T1, T2, T3, T4, T5>(
			CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			return GetServicesAsync<T1, T2, T3, T4, T5>(cancellation);
		}

		/// <summary>
		///     Implementation for asynchronously retrieving five services using Task.WhenAll.
		/// </summary>
		public virtual async Task<(T1, T2, T3, T4, T5)> GetServicesAsync<T1, T2, T3, T4, T5>(
			CancellationToken cancellation = default)
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
				return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result);
			}
			catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception)
			{
				if (!linkedCts.IsCancellationRequested)
				{
					linkedCts.Cancel();
				}

				throw;
			}
		}

		/// <summary>
		///     Asynchronously retrieves six services of the specified types using Task.
		/// </summary>
		public virtual Task<(T1, T2, T3, T4, T5, T6)> GetServiceAsync<T1, T2, T3, T4, T5, T6>(
			CancellationToken cancellation = default)
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			return GetServicesAsync<T1, T2, T3, T4, T5, T6>(cancellation);
		}

		/// <summary>
		///     Implementation for asynchronously retrieving six services using Task.WhenAll.
		/// </summary>
		public virtual async Task<(T1, T2, T3, T4, T5, T6)> GetServicesAsync<T1, T2, T3, T4, T5, T6>(
			CancellationToken cancellation = default)
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
				return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result, task6.Result);
			}
			catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception)
			{
				if (!linkedCts.IsCancellationRequested)
				{
					linkedCts.Cancel();
				}

				throw;
			}
		}
	}
}
// End of the conditional compilation block for standard Task-based async methods
#endif // !DISABLE_SL_ASYNC && (!ENABLE_UNITASK || DISABLE_SL_UNITASK)