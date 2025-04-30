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

		protected virtual void OnDestroy()
		{
			if (!ServiceLocator)
			{
				throw new InvalidOperationException($"{GetType().Name} requires a reference to a ServiceLocator.");
			}

			ServiceLocator.Unregister<T>();
		}
		
		/// <summary>
		///     To be called once a service is initialized and ready to be registered with the ServiceLocator.
		/// </summary>
		/// <returns>True if service was successfully registered, false otherwise.</returns>
		protected virtual bool ServiceReady()
		{
			if (!ServiceLocator)
			{
				Debug.LogError($"{GetType().Name} requires a reference to a ServiceLocator.");
				return false;
			}

			return ServiceLocator.Register(this as T);
		}
	}
}