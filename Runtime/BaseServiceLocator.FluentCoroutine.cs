#nullable enable
using System;
using System.Collections;

namespace Nonatomic.ServiceLocator
{
	///BaseServiceLocator.FluentCoroutine.cs
	public abstract partial class BaseServiceLocator
	{
		#if !DISABLE_SL_COROUTINES
		/// <summary>
		///     Gets a service using coroutine and returns a builder for chaining
		/// </summary>
		public virtual CoroutineServiceBuilder<T1> GetCoroutine<T1>() where T1 : class
		{
			return new(this);
		}

		/// <summary>
		///     Builder class for fluent coroutine-based service retrieval
		/// </summary>
		public class CoroutineServiceBuilder<T1> where T1 : class
		{
			private readonly BaseServiceLocator _serviceLocator;

			public CoroutineServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IEnumerator WithCallback(Action<T1?> callback)
			{
				return _serviceLocator.GetServiceCoroutine<T1>(callback);
			}

			public CoroutineServiceBuilder<T1, T2> And<T2>() where T2 : class
			{
				return new(_serviceLocator);
			}
		}

		/// <summary>
		///     Coroutine builder for two services
		/// </summary>
		public class CoroutineServiceBuilder<T1, T2>
			where T1 : class
			where T2 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private Action<T1?, T2?>? _finalCallback;
			private T1? _service1;
			private T2? _service2;
			private int _servicesResolved;

			public CoroutineServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IEnumerator WithCallback(Action<T1?, T2?> callback)
			{
				_finalCallback = callback;
				_servicesResolved = 0;

				yield return _serviceLocator.GetServiceCoroutine<T1>(service1 =>
				{
					_service1 = service1;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T2>(service2 =>
				{
					_service2 = service2;
					IncrementResolvedServices();
				});
			}

			private void IncrementResolvedServices()
			{
				_servicesResolved++;
				if (_servicesResolved == 2)
				{
					_finalCallback?.Invoke(_service1, _service2);
				}
			}

			public CoroutineServiceBuilder<T1, T2, T3> And<T3>() where T3 : class
			{
				return new(_serviceLocator);
			}
		}

		/// <summary>
		///     Coroutine builder for three services
		/// </summary>
		public class CoroutineServiceBuilder<T1, T2, T3>
			where T1 : class
			where T2 : class
			where T3 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private Action<T1?, T2?, T3?>? _finalCallback;
			private T1? _service1;
			private T2? _service2;
			private T3? _service3;
			private int _servicesResolved;

			public CoroutineServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IEnumerator WithCallback(Action<T1?, T2?, T3?> callback)
			{
				_finalCallback = callback;
				_servicesResolved = 0;

				yield return _serviceLocator.GetServiceCoroutine<T1>(service1 =>
				{
					_service1 = service1;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T2>(service2 =>
				{
					_service2 = service2;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T3>(service3 =>
				{
					_service3 = service3;
					IncrementResolvedServices();
				});
			}

			private void IncrementResolvedServices()
			{
				_servicesResolved++;
				if (_servicesResolved == 3)
				{
					_finalCallback?.Invoke(_service1, _service2, _service3);
				}
			}

			public CoroutineServiceBuilder<T1, T2, T3, T4> And<T4>() where T4 : class
			{
				return new(_serviceLocator);
			}
		}

		/// <summary>
		///     Coroutine builder for four services
		/// </summary>
		public class CoroutineServiceBuilder<T1, T2, T3, T4>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private Action<T1?, T2?, T3?, T4?>? _finalCallback;
			private T1? _service1;
			private T2? _service2;
			private T3? _service3;
			private T4? _service4;
			private int _servicesResolved;

			public CoroutineServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IEnumerator WithCallback(Action<T1?, T2?, T3?, T4?> callback)
			{
				_finalCallback = callback;
				_servicesResolved = 0;

				yield return _serviceLocator.GetServiceCoroutine<T1>(service1 =>
				{
					_service1 = service1;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T2>(service2 =>
				{
					_service2 = service2;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T3>(service3 =>
				{
					_service3 = service3;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T4>(service4 =>
				{
					_service4 = service4;
					IncrementResolvedServices();
				});
			}

			private void IncrementResolvedServices()
			{
				_servicesResolved++;
				if (_servicesResolved == 4)
				{
					_finalCallback?.Invoke(_service1, _service2, _service3, _service4);
				}
			}

			public CoroutineServiceBuilder<T1, T2, T3, T4, T5> And<T5>() where T5 : class
			{
				return new(_serviceLocator);
			}
		}

		/// <summary>
		///     Coroutine builder for five services
		/// </summary>
		public class CoroutineServiceBuilder<T1, T2, T3, T4, T5>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private Action<T1?, T2?, T3?, T4?, T5?>? _finalCallback;
			private T1? _service1;
			private T2? _service2;
			private T3? _service3;
			private T4? _service4;
			private T5? _service5;
			private int _servicesResolved;

			public CoroutineServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IEnumerator WithCallback(Action<T1?, T2?, T3?, T4?, T5?> callback)
			{
				_finalCallback = callback;
				_servicesResolved = 0;

				yield return _serviceLocator.GetServiceCoroutine<T1>(service1 =>
				{
					_service1 = service1;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T2>(service2 =>
				{
					_service2 = service2;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T3>(service3 =>
				{
					_service3 = service3;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T4>(service4 =>
				{
					_service4 = service4;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T5>(service5 =>
				{
					_service5 = service5;
					IncrementResolvedServices();
				});
			}

			private void IncrementResolvedServices()
			{
				_servicesResolved++;
				if (_servicesResolved == 5)
				{
					_finalCallback?.Invoke(_service1, _service2, _service3, _service4, _service5);
				}
			}

			public CoroutineServiceBuilder<T1, T2, T3, T4, T5, T6> And<T6>() where T6 : class
			{
				return new(_serviceLocator);
			}
		}

		/// <summary>
		///     Coroutine builder for six services
		/// </summary>
		public class CoroutineServiceBuilder<T1, T2, T3, T4, T5, T6>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private Action<T1?, T2?, T3?, T4?, T5?, T6?>? _finalCallback;
			private T1? _service1;
			private T2? _service2;
			private T3? _service3;
			private T4? _service4;
			private T5? _service5;
			private T6? _service6;
			private int _servicesResolved;

			public CoroutineServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IEnumerator WithCallback(Action<T1?, T2?, T3?, T4?, T5?, T6?> callback)
			{
				_finalCallback = callback;
				_servicesResolved = 0;

				yield return _serviceLocator.GetServiceCoroutine<T1>(service1 =>
				{
					_service1 = service1;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T2>(service2 =>
				{
					_service2 = service2;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T3>(service3 =>
				{
					_service3 = service3;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T4>(service4 =>
				{
					_service4 = service4;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T5>(service5 =>
				{
					_service5 = service5;
					IncrementResolvedServices();
				});

				yield return _serviceLocator.GetServiceCoroutine<T6>(service6 =>
				{
					_service6 = service6;
					IncrementResolvedServices();
				});
			}

			private void IncrementResolvedServices()
			{
				_servicesResolved++;
				if (_servicesResolved == 6)
				{
					_finalCallback?.Invoke(_service1, _service2, _service3, _service4, _service5, _service6);
				}
			}
		}
		#endif
	}
}