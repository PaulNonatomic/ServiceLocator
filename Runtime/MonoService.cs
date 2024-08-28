using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Nonatomic.ServiceLocator
{
	public abstract class MonoService<T> : MonoBehaviour where T : class
	{
		[FormerlySerializedAs("_serviceLocator")] 
		[SerializeField] protected ServiceLocator ServiceLocator;

		protected virtual void Awake()
		{
			if (this is not T)
			{
				throw new InvalidOperationException($"{GetType().Name} must implement the {typeof(T).Name} interface.");
			}
			
			ServiceLocator.Register<T>(this as T);
		}

		protected virtual void OnDestroy()
		{
			ServiceLocator.Unregister<T>();
		}
	}
}