using System;
using System.Threading.Tasks;

namespace Nonatomic.ServiceLocator
{
	public class ServicePromise<T> : IServicePromise
	{
		private readonly TaskCompletionSource<T> _task = new TaskCompletionSource<T>();

		public Task<T> Task => _task.Task;

		public void Fulfill(object service)
		{
			if (service is not T typedService)
			{
				throw new InvalidCastException($"Cannot fulfill promise with service of type {service.GetType()} as {typeof(T)}");
			}
			
			_task.SetResult(typedService);
		}
	}
}