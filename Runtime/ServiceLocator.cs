using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nonatomic.ServiceLocator
{
	/// <summary>
	/// A ScriptableObject-based service locator for managing and accessing services throughout the application.
	/// </summary>
	[CreateAssetMenu(fileName = "ServiceLocator", menuName = "ServiceLocator", order = 1)]
	public class ServiceLocator : ScriptableObject
	{
		private Dictionary<Type, IService> _serviceMap = new ();
		
		/// <summary>
		/// Registers a service with the service locator.
		/// </summary>
		/// <typeparam name="T">The type of the service to register.</typeparam>
		/// <param name="service">The service instance to register.</param>
		/// <remarks>
		/// If a service of the same type is already registered, this method logs a warning and ignores the registration.
		/// </remarks>
		public void Register<T>(T service) where T : class, IService
		{
			var serviceType = typeof(T);
			if (_serviceMap.ContainsKey(serviceType))
			{
				Debug.LogWarning($"Service of type {serviceType} is already registered. Ignoring this registration.");
			}
			
			_serviceMap.Add(serviceType, service);
		}
		
		/// <summary>
		/// Registers or replaces a service in the service locator.
		/// </summary>
		/// <typeparam name="T">The type of the service to register or replace.</typeparam>
		/// <param name="service">The service instance to register or replace.</param>
		public void RegisterOrReplace<T>(T service) where T : class, IService
		{
			_serviceMap[typeof(T)] = service;
		}
		
		/// <summary>
		/// Unregisters a service from the service locator.
		/// </summary>
		/// <typeparam name="T">The type of the service to unregister.</typeparam>
		/// <param name="service">The service instance to unregister.</param>
		/// <remarks>
		/// If the service to unregister does not match the registered service, no action is taken.
		/// </remarks>
		public void Unregister<T>(T service) where T : class
		{
			var serviceType = typeof(T);
			if (!_serviceMap.ContainsKey(serviceType)) return;
			if (_serviceMap[serviceType] != service) return;
			
			_serviceMap.Remove(serviceType);
		}
		
		/// <summary>
		/// Retrieves a registered service of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <returns>The requested service.</returns>
		/// <exception cref="ArgumentException">Thrown if the service is not registered.</exception>
		public T GetService<T>() where T : class, IService
		{
			var serviceType = typeof(T);
			if (_serviceMap.TryGetValue(serviceType, out var service))
			{
				return (T) service;
			}
			
			throw new ArgumentException($"Service of type {serviceType} is not registered.");
		}
		
		/// <summary>
		/// Tries to retrieve a registered service of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of service to retrieve.</typeparam>
		/// <param name="service">The retrieved service or null if not found.</param>
		/// <returns>True if the service was found, otherwise false.</returns>
		public bool TryGetService<T>(out T service) where T : class, IService
		{
			service = null;
			
			var serviceType = typeof(T);
			if (_serviceMap.TryGetValue(serviceType, out var foundService))
			{
				service = (T) foundService;
				return true;
			}

			return false;
		}
	}
}