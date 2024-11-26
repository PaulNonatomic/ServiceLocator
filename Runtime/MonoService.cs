using System;
using UnityEngine;

namespace Nonatomic.ServiceLocator
{
	public abstract class MonoService<T> : MonoBehaviour where T : class
	{
		[SerializeField] protected ServiceLocator ServiceLocator;

		protected virtual void Awake()
		{
			if (this is not T)
			{
				throw new InvalidOperationException($"{GetType().Name} must implement the {typeof(T).Name} interface.");
			}
		}
		
		/// <summary>
		/// To be called once a service is initialized and ready to be registered with the ServiceLocator.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		protected virtual void ServiceReady()
		{
			if(!ServiceLocator)
			{
				throw new InvalidOperationException($"{GetType().Name} requires a reference to a ServiceLocator.");
			}
			
			ServiceLocator.Register<T>(this as T);
		}

		protected virtual void OnDestroy()
		{
			if(!ServiceLocator)
			{
				throw new InvalidOperationException($"{GetType().Name} requires a reference to a ServiceLocator.");
			}
			
			ServiceLocator.Unregister<T>();
		}
	}
}