using System;
using UnityEngine;

namespace Nonatomic.ServiceLocator
{
	public abstract class MonoService<T> : MonoBehaviour where T : class
	{
		[SerializeField] private ServiceLocator _serviceLocator;

		protected virtual void Awake()
		{
			if (this is not T)
			{
				throw new InvalidOperationException($"{GetType().Name} must implement the {typeof(T).Name} interface.");
			}
			
			_serviceLocator.Register<T>(this as T);
		}

		protected virtual void OnDestroy()
		{
			_serviceLocator.Unregister<T>();
		}
	}
}