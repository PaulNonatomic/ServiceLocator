using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nonatomic.ServiceLocator
{
	/// <summary>
	/// A ScriptableObject-based service locator for managing and accessing services throughout the application.
	/// </summary>
	public abstract class BaseServiceLocator<T1> : ScriptableObject
	{
		private Dictionary<Type, T1> _serviceMap = new ();
		private Dictionary<Type, IServicePromise> _promiseMap = new ();
		
		/// <summary>
		/// Registers a service of type T.
		/// </summary>
		/// <typeparam name="T">The type of the service to be registered.</typeparam>
		/// <param name="service">The service instance to register.</param>
		/// <returns>Returns the registered service instance.</returns>
		/// <remarks>
		/// If there is an existing promise for the service type, it will be fulfilled
		/// with the provided service instance and then removed from the promise map.
		/// </remarks>
		public T Register<T>(T service) where T : T1
		{
			var serviceType = typeof(T);
			_serviceMap[serviceType] = service;

			if (!_promiseMap.TryGetValue(serviceType, out var promise)) return service;
			
			promise.Fulfill(service);
			_promiseMap.Remove(serviceType);

			return service;
		}
		
		/// <summary>
		/// Unregisters a service of type T.
		/// </summary>
		/// <typeparam name="T">The type of the service to be unregistered.</typeparam>
		/// <param name="service">The service instance to unregister.</param>
		/// <remarks>
		/// Removes the service of type T from the service map. Does not affect
		/// any promises that may have been made for the service.
		/// </remarks>
		public void Unregister<T>(T service) where T : T1
		{
			_serviceMap.Remove(typeof(T));
		}
		
		/// <summary>
		/// Retrieves a promise for a service of type T.
		/// </summary>
		/// <typeparam name="T">The type of the service to retrieve.</typeparam>
		/// <returns>A ServicePromise for the requested service type.</returns>
		/// <remarks>
		/// If the service of type T is already registered, the promise is
		/// immediately fulfilled with it. Otherwise, the promise is stored
		/// and will be fulfilled when the service is registered.
		/// </remarks>
		public ServicePromise<T> GetService<T>() where T : T1
		{
			var serviceType = typeof(T);
			var promise = new ServicePromise<T>();

			if (_serviceMap.TryGetValue(serviceType, out var service))
			{
				promise.Fulfill((T)service);
			}
			else if (_promiseMap.TryGetValue(serviceType, out var storedPromise))
			{
				var storedServicePromise = storedPromise as ServicePromise<T1>;
				if (storedServicePromise == null) return promise;
				
				storedServicePromise.Task.ContinueWith(t =>
				{
					if (!t.IsCompletedSuccessfully) return;
					promise.Fulfill((T)t.Result);
				});
			}
			else
			{
				_promiseMap[serviceType] = promise;
			}

			return promise;
		}
		
		/// <summary>
		/// Tries to retrieve a service of type T.
		/// </summary>
		/// <typeparam name="T">The type of the service to retrieve.</typeparam>
		/// <param name="service">The service instance, if found.</param>
		/// <returns>True if the service is found, false otherwise.</returns>
		/// <remarks>
		/// This method does not wait for the service to be registered and
		/// immediately returns false if the service is not currently available.
		/// </remarks>
		public bool TryGetService<T>(out T service) where T : T1
		{
			service = default(T);
			
			var serviceType = typeof(T);
			if (!_serviceMap.TryGetValue(serviceType, out var foundService)) return false;
			
			service = (T) foundService;
			return true;
		}
	}
}