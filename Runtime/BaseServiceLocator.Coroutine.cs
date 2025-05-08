#if !DISABLE_SL_COROUTINES

#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
// Required for Stopwatch

// Required for Debug (if logging is enabled)

namespace Nonatomic.ServiceLocator
{
	///BaseServiceLocator.Coroutine.cs
	public abstract partial class BaseServiceLocator
	{
		/// <summary>
		///     Retrieves a service using a coroutine.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <param name="callback">Action to be called with the retrieved service, or null if timed out or unavailable.</param>
		/// <param name="timeout">Optional timeout period for the retrieval.</param>
		/// <returns>An IEnumerator for use with StartCoroutine.</returns>
		public virtual IEnumerator GetServiceCoroutine<T>(Action<T?> callback, TimeSpan? timeout = null) where T : class
		{
			var serviceType = typeof(T);
			Action<object?> wrappedCallback;
			(Type ServiceType, Action<object?> Callback, System.Diagnostics.Stopwatch? Stopwatch, TimeSpan? Timeout)
				pendingCoroutineEntry;
			var isPending = false; // To control the while loop locally

			lock (Lock)
			{
				if (ServiceMap.TryGetValue(serviceType, out var service))
				{
					callback(service as T);
					yield break;
				}

				wrappedCallback = obj => callback(obj as T);
				Stopwatch? stopwatch = null;
				if (timeout.HasValue)
				{
					stopwatch = Stopwatch.StartNew();
				}

				pendingCoroutineEntry = (serviceType, wrappedCallback, stopwatch, timeout);
				PendingCoroutines.Add(pendingCoroutineEntry);
				isPending = true; // Mark that this specific request is now pending
			}

			// This loop runs outside the main lock, polling for the service or timeout
			while (isPending)
			{
				bool stillExistsInPendingList;
				object? resolvedService = null;
				var foundInServiceMap = false;

				lock (Lock)
				{
					// Check if service became available
					if (ServiceMap.TryGetValue(serviceType, out resolvedService))
					{
						foundInServiceMap = true;
					}

					// Check if this specific pending coroutine is still in the list
					// It might have been removed if the service was registered, or timed out by UpdatePendingCoroutines
					stillExistsInPendingList = PendingCoroutines.Contains(pendingCoroutineEntry);
				}

				if (foundInServiceMap)
				{
					// Service became available
					lock (Lock)
					{
						PendingCoroutines.Remove(pendingCoroutineEntry);
					}

					pendingCoroutineEntry.Callback(resolvedService as T);
					isPending = false; // Stop this coroutine's loop
					yield break;
				}

				if (!stillExistsInPendingList)
				{
					// Coroutine was removed from the list, possibly by registration, explicit cancellation, or UpdatePendingCoroutines timeout
					// If it was due to registration, the callback would have been invoked.
					// If due to timeout from UpdatePendingCoroutines, callback(null) was called.
					// If due to Unregister<T> or scene unload, callback(null) was called.
					// So, we can just break.
					isPending = false;
					yield break;
				}

				// Manual timeout check for this specific coroutine instance,
				// as a fallback or if EditorApplication.update is not frequent enough.
				// UpdatePendingCoroutines is the primary mechanism for timeouts.
				if (pendingCoroutineEntry.Timeout.HasValue && pendingCoroutineEntry.Stopwatch != null &&
					pendingCoroutineEntry.Stopwatch.Elapsed > pendingCoroutineEntry.Timeout.Value)
				{
					#if !DISABLE_SL_LOGGING
                UnityEngine.Debug.LogWarning($"Service {serviceType.Name} retrieval (instance) timed out after {pendingCoroutineEntry.Timeout.Value}.");
					#endif
					lock (Lock)
					{
						PendingCoroutines.Remove(pendingCoroutineEntry);
					}

					pendingCoroutineEntry.Callback(null); // Timed out
					isPending = false; // Stop this coroutine's loop
					yield break;
				}

				yield return null; // Wait for the next frame
			}
		}
	}
}
#endif