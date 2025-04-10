namespace Nonatomic.ServiceLocator
{
	public static class ServicePromiseExtensions
	{
		public static IServicePromise<object> AsObjectPromise<T>(this IServicePromise<T> promise)
		{
			var objectPromise = new ServicePromise<object>();
			promise.Then(result => { objectPromise.Resolve(result); })
				.Catch(ex => { objectPromise.Reject(ex); });
			return objectPromise;
		}
	}
}