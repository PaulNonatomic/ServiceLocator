using System;

namespace Nonatomic.ServiceLocator
{
	public interface IServicePromise<T>
	{
		void Resolve(T value);
		void Reject(Exception ex);

		ServicePromise<TResult> Then<TResult>(Func<T, TResult> onFulfilled);
		ServicePromise<T> Catch(Action<Exception> onRejected);
	}
}