#nullable enable
using System;
using System.Collections;

// For Stopwatch if needed, though BaseServiceLocator.Coroutine handles it

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
			private TimeSpan? _timeout; // Store timeout

			public CoroutineServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			/// <summary>
			///     Specifies a timeout for the service retrieval operation.
			/// </summary>
			/// <param name="timeout">The duration to wait before timing out.</param>
			/// <returns>The current builder instance for chaining.</returns>
			public CoroutineServiceBuilder<T1> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			/// <summary>
			///     Executes the service retrieval with the specified callback.
			/// </summary>
			/// <param name="callback">Action to be called with the retrieved service, or null if timed out/unavailable.</param>
			/// <returns>An IEnumerator for use with StartCoroutine.</returns>
			public IEnumerator WithCallback(Action<T1?> callback)
			{
				// Pass the stored timeout to GetServiceCoroutine
				return _serviceLocator.GetServiceCoroutine<T1>(callback, _timeout);
			}

			public CoroutineServiceBuilder<T1, T2> And<T2>() where T2 : class
			{
				// Pass current timeout to the next builder
				return new(_serviceLocator, _timeout);
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
			private bool _timedOutOrFailed;
			private TimeSpan? _timeout;


			public CoroutineServiceBuilder(BaseServiceLocator serviceLocator,
				TimeSpan? timeout = null) // Accept timeout
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public CoroutineServiceBuilder<T1, T2> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public IEnumerator WithCallback(Action<T1?, T2?> callback)
			{
				_finalCallback = callback;
				_servicesResolved = 0;
				_timedOutOrFailed = false;

				yield return _serviceLocator.GetServiceCoroutine<T1>(service1 =>
				{
					if (_timedOutOrFailed)
					{
						return;
					}

					if (service1 == null && _timeout.HasValue)
					{
						_timedOutOrFailed = true; // If timeout causes null, flag it.
					}

					_service1 = service1;
					IncrementResolvedServices();
				}, _timeout); // Pass timeout

				if (_timedOutOrFailed)
				{
					HandleFailure();
					yield break;
				}

				yield return _serviceLocator.GetServiceCoroutine<T2>(service2 =>
				{
					if (_timedOutOrFailed)
					{
						return;
					}

					if (service2 == null && _timeout.HasValue)
					{
						_timedOutOrFailed = true;
					}

					_service2 = service2;
					IncrementResolvedServices();
				}, _timeout); // Pass timeout

				if (_timedOutOrFailed)
				{
					HandleFailure();
				}
			}

			private void HandleFailure()
			{
				if (_servicesResolved < 2) // Ensure callback only if not all resolved due to timeout/failure
				{
					_finalCallback?.Invoke(default, default);
				}
			}

			private void IncrementResolvedServices()
			{
				if (_timedOutOrFailed)
				{
					return;
				}

				_servicesResolved++;
				if (_servicesResolved == 2)
				{
					_finalCallback?.Invoke(_service1, _service2);
				}
			}

			public CoroutineServiceBuilder<T1, T2, T3> And<T3>() where T3 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		// Similar updates for CoroutineServiceBuilder<T1, T2, T3> to <T1, T2, T3, T4, T5, T6>
		// Example for T3:
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
			private bool _timedOutOrFailed;
			private TimeSpan? _timeout;

			public CoroutineServiceBuilder(BaseServiceLocator serviceLocator, TimeSpan? timeout = null)
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public CoroutineServiceBuilder<T1, T2, T3> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public IEnumerator WithCallback(Action<T1?, T2?, T3?> callback)
			{
				_finalCallback = callback;
				_servicesResolved = 0;
				_timedOutOrFailed = false;

				yield return _serviceLocator.GetServiceCoroutine<T1>(service1 =>
				{
					if (_timedOutOrFailed)
					{
						return;
					}

					if (service1 == null && _timeout.HasValue)
					{
						_timedOutOrFailed = true;
					}

					_service1 = service1;
					IncrementResolvedServices();
				}, _timeout);
				if (_timedOutOrFailed)
				{
					HandleFailure();
					yield break;
				}

				yield return _serviceLocator.GetServiceCoroutine<T2>(service2 =>
				{
					if (_timedOutOrFailed)
					{
						return;
					}

					if (service2 == null && _timeout.HasValue)
					{
						_timedOutOrFailed = true;
					}

					_service2 = service2;
					IncrementResolvedServices();
				}, _timeout);
				if (_timedOutOrFailed)
				{
					HandleFailure();
					yield break;
				}

				yield return _serviceLocator.GetServiceCoroutine<T3>(service3 =>
				{
					if (_timedOutOrFailed)
					{
						return;
					}

					if (service3 == null && _timeout.HasValue)
					{
						_timedOutOrFailed = true;
					}

					_service3 = service3;
					IncrementResolvedServices();
				}, _timeout);
				if (_timedOutOrFailed)
				{
					HandleFailure();
				}
			}

			private void HandleFailure()
			{
				if (_servicesResolved < 3)
				{
					_finalCallback?.Invoke(default, default, default);
				}
			}

			private void IncrementResolvedServices()
			{
				if (_timedOutOrFailed)
				{
					return;
				}

				_servicesResolved++;
				if (_servicesResolved == 3)
				{
					_finalCallback?.Invoke(_service1, _service2, _service3);
				}
			}

			public CoroutineServiceBuilder<T1, T2, T3, T4> And<T4>() where T4 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		public class CoroutineServiceBuilder<T1, T2, T3, T4>
			where T1 : class where T2 : class where T3 : class where T4 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private bool _failed;
			private Action<T1?, T2?, T3?, T4?>? _finalCallback;
			private int _res;
			private T1? _s1;
			private T2? _s2;
			private T3? _s3;
			private T4? _s4;
			private TimeSpan? _timeout;

			public CoroutineServiceBuilder(BaseServiceLocator sl, TimeSpan? t = null)
			{
				_serviceLocator = sl;
				_timeout = t;
			}

			public CoroutineServiceBuilder<T1, T2, T3, T4> WithTimeout(TimeSpan t)
			{
				_timeout = t;
				return this;
			}

			public IEnumerator WithCallback(Action<T1?, T2?, T3?, T4?> cb)
			{
				_finalCallback = cb;
				_res = 0;
				_failed = false;
				yield return _serviceLocator.GetServiceCoroutine<T1>(s => ProcessService(ref _s1, s, 0), _timeout);
				if (_failed)
				{
					HandleFailure();
					yield break;
				}

				yield return _serviceLocator.GetServiceCoroutine<T2>(s => ProcessService(ref _s2, s, 1), _timeout);
				if (_failed)
				{
					HandleFailure();
					yield break;
				}

				yield return _serviceLocator.GetServiceCoroutine<T3>(s => ProcessService(ref _s3, s, 2), _timeout);
				if (_failed)
				{
					HandleFailure();
					yield break;
				}

				yield return _serviceLocator.GetServiceCoroutine<T4>(s => ProcessService(ref _s4, s, 3), _timeout);
				if (_failed)
				{
					HandleFailure();
				}
			}

			private void ProcessService<TSrv>(ref TSrv? sField, TSrv? sVal, int idx)
			{
				if (_failed)
				{
					return;
				}

				if (sVal == null && _timeout.HasValue)
				{
					_failed = true;
				}

				sField = sVal;
				IncRes();
			}

			private void HandleFailure()
			{
				if (_res < 4)
				{
					_finalCallback?.Invoke(default, default, default, default);
				}
			}

			private void IncRes()
			{
				if (_failed)
				{
					return;
				}

				_res++;
				if (_res == 4)
				{
					_finalCallback?.Invoke(_s1, _s2, _s3, _s4);
				}
			}

			public CoroutineServiceBuilder<T1, T2, T3, T4, T5> And<T5>() where T5 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		public class CoroutineServiceBuilder<T1, T2, T3, T4, T5>
			where T1 : class where T2 : class where T3 : class where T4 : class where T5 : class
		{
			private readonly BaseServiceLocator _sl;
			private bool _f;
			private Action<T1?, T2?, T3?, T4?, T5?>? _fcb;
			private int _r;
			private T1? _s1;
			private T2? _s2;
			private T3? _s3;
			private T4? _s4;
			private T5? _s5;
			private TimeSpan? _to;

			public CoroutineServiceBuilder(BaseServiceLocator sl, TimeSpan? t = null)
			{
				_sl = sl;
				_to = t;
			}

			public CoroutineServiceBuilder<T1, T2, T3, T4, T5> WithTimeout(TimeSpan t)
			{
				_to = t;
				return this;
			}

			public IEnumerator WithCallback(Action<T1?, T2?, T3?, T4?, T5?> cb)
			{
				_fcb = cb;
				_r = 0;
				_f = false;
				yield return _sl.GetServiceCoroutine<T1>(s => PS(ref _s1, s), _to);
				if (_f)
				{
					HF();
					yield break;
				}

				yield return _sl.GetServiceCoroutine<T2>(s => PS(ref _s2, s), _to);
				if (_f)
				{
					HF();
					yield break;
				}

				yield return _sl.GetServiceCoroutine<T3>(s => PS(ref _s3, s), _to);
				if (_f)
				{
					HF();
					yield break;
				}

				yield return _sl.GetServiceCoroutine<T4>(s => PS(ref _s4, s), _to);
				if (_f)
				{
					HF();
					yield break;
				}

				yield return _sl.GetServiceCoroutine<T5>(s => PS(ref _s5, s), _to);
				if (_f)
				{
					HF();
				}
			}

			private void PS<TS>(ref TS? sf, TS? sv)
			{
				if (_f)
				{
					return;
				}

				if (sv == null && _to.HasValue)
				{
					_f = true;
				}

				sf = sv;
				IR();
			}

			private void HF()
			{
				if (_r < 5)
				{
					_fcb?.Invoke(default, default, default, default, default);
				}
			}

			private void IR()
			{
				if (_f)
				{
					return;
				}

				_r++;
				if (_r == 5)
				{
					_fcb?.Invoke(_s1, _s2, _s3, _s4, _s5);
				}
			}

			public CoroutineServiceBuilder<T1, T2, T3, T4, T5, T6> And<T6>() where T6 : class
			{
				return new(_sl, _to);
			}
		}

		public class CoroutineServiceBuilder<T1, T2, T3, T4, T5, T6>
			where T1 : class where T2 : class where T3 : class where T4 : class where T5 : class where T6 : class
		{
			private readonly BaseServiceLocator _sl;
			private bool _f;
			private Action<T1?, T2?, T3?, T4?, T5?, T6?>? _fcb;
			private int _r;
			private T1? _s1;
			private T2? _s2;
			private T3? _s3;
			private T4? _s4;
			private T5? _s5;
			private T6? _s6;
			private TimeSpan? _to;

			public CoroutineServiceBuilder(BaseServiceLocator sl, TimeSpan? t = null)
			{
				_sl = sl;
				_to = t;
			}

			public CoroutineServiceBuilder<T1, T2, T3, T4, T5, T6> WithTimeout(TimeSpan t)
			{
				_to = t;
				return this;
			}

			public IEnumerator WithCallback(Action<T1?, T2?, T3?, T4?, T5?, T6?> cb)
			{
				_fcb = cb;
				_r = 0;
				_f = false;
				yield return _sl.GetServiceCoroutine<T1>(s => PS(ref _s1, s), _to);
				if (_f)
				{
					HF();
					yield break;
				}

				yield return _sl.GetServiceCoroutine<T2>(s => PS(ref _s2, s), _to);
				if (_f)
				{
					HF();
					yield break;
				}

				yield return _sl.GetServiceCoroutine<T3>(s => PS(ref _s3, s), _to);
				if (_f)
				{
					HF();
					yield break;
				}

				yield return _sl.GetServiceCoroutine<T4>(s => PS(ref _s4, s), _to);
				if (_f)
				{
					HF();
					yield break;
				}

				yield return _sl.GetServiceCoroutine<T5>(s => PS(ref _s5, s), _to);
				if (_f)
				{
					HF();
					yield break;
				}

				yield return _sl.GetServiceCoroutine<T6>(s => PS(ref _s6, s), _to);
				if (_f)
				{
					HF();
				}
			}

			private void PS<TS>(ref TS? sf, TS? sv)
			{
				if (_f)
				{
					return;
				}

				if (sv == null && _to.HasValue)
				{
					_f = true;
				}

				sf = sv;
				IR();
			}

			private void HF()
			{
				if (_r < 6)
				{
					_fcb?.Invoke(default, default, default, default, default, default);
				}
			}

			private void IR()
			{
				if (_f)
				{
					return;
				}

				_r++;
				if (_r == 6)
				{
					_fcb?.Invoke(_s1, _s2, _s3, _s4, _s5, _s6);
				}
			}
		}
		#endif
	}
}