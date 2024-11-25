using System.Threading;
using UnityEngine;

namespace Nonatomic.ServiceLocator
{
	public static class UnitySynchronizationContext
	{
		public static SynchronizationContext Context { get; private set; }
		public static int MainThreadId { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		public static void Initialize()
		{
			Context = SynchronizationContext.Current ?? new SynchronizationContext();
			SynchronizationContext.SetSynchronizationContext(Context);
			MainThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		public static void Post(SendOrPostCallback callback, object state)
		{
			Context.Post(callback, state);
		}
	}
}