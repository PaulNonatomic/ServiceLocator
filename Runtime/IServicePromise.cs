using System;
using System.Threading;

namespace Nonatomic.ServiceLocator
{
	public interface IServicePromise<T> : IDisposable
	{
		void Resolve(T value);
		void Reject(Exception ex);
		void WithCancellation(CancellationToken cancellationToken);

		ServicePromise<TResult> Then<TResult>(Func<T, TResult> onFulfilled);
		ServicePromise<T> Then(Action<T> onFulfilled);
		ServicePromise<T> Catch(Action<Exception> onRejected);
	}
}