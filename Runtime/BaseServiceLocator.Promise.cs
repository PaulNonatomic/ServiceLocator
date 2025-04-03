#if !DISABLE_SL_PROMISES

#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nonatomic.ServiceLocator
{
	public abstract partial class BaseServiceLocator
	{
		/// <summary>
		///     Retrieves a service using a promise-based approach.
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
					taskList = new();
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
							{
								return;
							}

							taskCompletion.TrySetCanceled();
							taskList.Remove(taskCompletion);

							if (taskList.Count != 0)
							{
								return;
							}

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
		///     Retrieves two services using a promise-based approach.
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
		///     Retrieves three services using a promise-based approach.
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
		///     Retrieves four services using a promise-based approach.
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
		///     Retrieves five services using a promise-based approach.
		/// </summary>
		public virtual IServicePromise<(T1, T2, T3, T4, T5)> GetService<T1, T2, T3, T4, T5>(
			CancellationToken token = default)
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
		///     Retrieves six services using a promise-based approach.
		/// </summary>
		public virtual IServicePromise<(T1, T2, T3, T4, T5, T6)> GetService<T1, T2, T3, T4, T5, T6>(
			CancellationToken token = default)
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
	}
}
#endif