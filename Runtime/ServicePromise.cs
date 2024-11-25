using System;
using System.Threading.Tasks;

namespace Nonatomic.ServiceLocator
{
	public class ServicePromise<T> : IServicePromise<T>
	{
		private T _result;
		private Exception _error;
		private bool _isResolved;
		private bool _isRejected;
		private readonly TaskCompletionSource<T> _taskCompletion = new();

		public void Resolve(T value)
		{
			if (_isResolved || _isRejected) return;
			
			_result = value;
			_isResolved = true;
			_taskCompletion.TrySetResult(value);
		}

		public void Reject(Exception ex)
		{
			if (_isResolved || _isRejected) return;
			
			_error = ex;
			_isRejected = true;
			_taskCompletion.TrySetException(ex);
		}

		public ServicePromise<TResult> Then<TResult>(Func<T, TResult> onFulfilled)
		{
			var resultPromise = new ServicePromise<TResult>();

			if (_isResolved)
			{
				try
				{
					var result = onFulfilled(_result);
					resultPromise.Resolve(result);
				}
				catch (Exception ex)
				{
					resultPromise.Reject(ex);
				}
				return resultPromise;
			}

			if (_isRejected)
			{
				resultPromise.Reject(_error);
				return resultPromise;
			}
			
			_taskCompletion.Task.ContinueWith(task =>
			{
				UnitySynchronizationContext.Context.Post(_ =>
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
				}, null);
			});

			return resultPromise;
		}

		public ServicePromise<T> Then(Action<T> onFulfilled)
		{
			var resultPromise = new ServicePromise<T>();

			if (_isResolved)
			{
				try
				{
					onFulfilled(_result);
					resultPromise.Resolve(_result);
				}
				catch (Exception ex)
				{
					resultPromise.Reject(ex);
				}
				return resultPromise;
			}

			if (_isRejected)
			{
				resultPromise.Reject(_error);
				return resultPromise;
			}

			_taskCompletion.Task.ContinueWith(task =>
			{
				UnitySynchronizationContext.Context.Post(_ =>
				{
					if (task.IsCompletedSuccessfully)
					{
						try
						{
							onFulfilled(task.Result);
							resultPromise.Resolve(task.Result);
						}
						catch (Exception ex)
						{
							resultPromise.Reject(ex);
						}
					}
					else
					{
						resultPromise.Reject(task.Exception ?? new Exception("Task failed."));
					}
				}, null);
			});

			return resultPromise;
		}

		public ServicePromise<T> Catch(Action<Exception> onRejected)
		{
			var resultPromise = new ServicePromise<T>();

			if (_isRejected)
			{
				try
				{
					onRejected(_error);
					resultPromise.Resolve(default);
				}
				catch (Exception ex)
				{
					resultPromise.Reject(ex);
				}
				return resultPromise;
			}

			if (_isResolved)
			{
				resultPromise.Resolve(_result);
				return resultPromise;
			}

			_taskCompletion.Task.ContinueWith(task =>
			{
				UnitySynchronizationContext.Context.Post(_ =>
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
				}, null);
			});

			return resultPromise;
		}
	}
}