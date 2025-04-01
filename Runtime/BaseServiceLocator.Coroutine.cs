#nullable enable
using System;
using System.Collections;
using System.Linq;

namespace Nonatomic.ServiceLocator
{
	public abstract partial class BaseServiceLocator
	{
		/// <summary>
		///     Retrieves a service using a coroutine.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <param name="callback">Action to be called with the retrieved service.</param>
		/// <returns>An IEnumerator for use with StartCoroutine.</returns>
		public virtual IEnumerator GetServiceCoroutine<T>(Action<T?> callback) where T : class
		{
			var serviceType = typeof(T);
			var isPending = false;
			Action<object?> wrappedCallback = null!;

			lock (Lock)
			{
				if (ServiceMap.TryGetValue(serviceType, out var service))
				{
					callback((T)service);
					yield break;
				}

				wrappedCallback = obj => callback(obj as T);
				PendingCoroutines.Add((serviceType, wrappedCallback));
				isPending = true;
			}

			while (isPending)
			{
				lock (Lock)
				{
					if (ServiceMap.TryGetValue(serviceType, out var service))
					{
						PendingCoroutines.RemoveAll(x => x.Item1 == serviceType && x.Item2 == wrappedCallback);
						callback((T)service);
						yield break;
					}

					if (!PendingCoroutines.Any(x => x.Item1 == serviceType && x.Item2 == wrappedCallback))
					{
						isPending = false;
						yield break;
					}
				}

				yield return null;
			}
		}
	}
}