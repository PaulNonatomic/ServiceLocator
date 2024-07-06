using System;
using System.Threading.Tasks;

namespace Nonatomic.ServiceLocator
{
	public class ServicePromise<T> : IServicePromise<T>
	{
		private readonly TaskCompletionSource<T> _taskCompletion = new TaskCompletionSource<T>();

		public void Resolve(T value) => _taskCompletion.TrySetResult(value);
		public void Reject(Exception ex) => _taskCompletion.TrySetException(ex);

		public ServicePromise<TResult> Then<TResult>(Func<T, TResult> onFulfilled)
		{
			var resultPromise = new ServicePromise<TResult>();
			_taskCompletion.Task.ContinueWith(task =>
			{
				if (task.IsCompletedSuccessfully)
				{
					try
					{
						var result = onFulfilled(task.Result);
						resultPromise.Resolve(result);
					}
					catch (Exception ex)
					{
						resultPromise.Reject(ex);
					}
				}
				else
				{
					resultPromise.Reject(task.Exception);
				}
			});
			return resultPromise;
		}
		
		public ServicePromise<T> Catch(Action<Exception> onRejected)
		{
			var resultPromise = new ServicePromise<T>();
			_taskCompletion.Task.ContinueWith(task =>
			{
				if (task.IsFaulted)
				{
					try
					{
						onRejected(task.Exception);
						resultPromise.Resolve(default);
					}
					catch (Exception ex)
					{
						resultPromise.Reject(ex);
					}
				}
				else
				{
					resultPromise.Resolve(task.Result);
				}
			});
			return resultPromise;
		}
	}
}