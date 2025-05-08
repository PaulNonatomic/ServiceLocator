#nullable enable
using System;
using System.Threading;
// Required for TimeSpan

namespace Nonatomic.ServiceLocator
{
	///BaseServiceLocator.FluentPromise.cs
	public abstract partial class BaseServiceLocator
	{
		#if !DISABLE_SL_PROMISES
		/// <summary>
		///     Gets a service using promise pattern and returns a builder for chaining
		/// </summary>
		public virtual PromiseServiceBuilder<T1> Get<T1>() where T1 : class
		{
			return new(this);
		}

		/// <summary>
		///     Builder class for fluent promise-based service retrieval
		/// </summary>
		public class PromiseServiceBuilder<T1> where T1 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout; // Store timeout

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			/// <summary>
			///     Specifies a timeout for the service retrieval operation.
			/// </summary>
			/// <param name="timeout">The duration to wait before timing out.</param>
			/// <returns>The current builder instance for chaining.</returns>
			public PromiseServiceBuilder<T1> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			/// <summary>
			///     Retrieves the service with an optional cancellation token and previously set timeout.
			/// </summary>
			/// <param name="cancellationToken">Optional token to cancel the operation.</param>
			/// <returns>An IServicePromise for the requested service.</returns>
			public IServicePromise<T1> WithCancellation(CancellationToken cancellationToken = default)
			{
				// Pass the stored timeout to GetService
				return _serviceLocator.GetService<T1>(cancellationToken, _timeout);
			}

			public PromiseServiceBuilder<T1, T2> And<T2>() where T2 : class
			{
				// Pass current timeout to the next builder
				return new(_serviceLocator, _timeout);
			}
		}

		/// <summary>
		///     Promise builder for two services
		/// </summary>
		public class PromiseServiceBuilder<T1, T2>
			where T1 : class
			where T2 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout;

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator, TimeSpan? timeout = null) // Accept timeout
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public PromiseServiceBuilder<T1, T2> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public IServicePromise<(T1, T2)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1, T2>(cancellationToken, _timeout);
			}

			public PromiseServiceBuilder<T1, T2, T3> And<T3>() where T3 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		// Similar updates for PromiseServiceBuilder<T1, T2, T3> to <T1, T2, T3, T4, T5, T6>

		public class PromiseServiceBuilder<T1, T2, T3>
			where T1 : class
			where T2 : class
			where T3 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout;

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator, TimeSpan? timeout = null)
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public PromiseServiceBuilder<T1, T2, T3> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public IServicePromise<(T1, T2, T3)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1, T2, T3>(cancellationToken, _timeout);
			}

			public PromiseServiceBuilder<T1, T2, T3, T4> And<T4>() where T4 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		public class PromiseServiceBuilder<T1, T2, T3, T4>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout;

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator, TimeSpan? timeout = null)
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public PromiseServiceBuilder<T1, T2, T3, T4> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public IServicePromise<(T1, T2, T3, T4)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1, T2, T3, T4>(cancellationToken, _timeout);
			}

			public PromiseServiceBuilder<T1, T2, T3, T4, T5> And<T5>() where T5 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		public class PromiseServiceBuilder<T1, T2, T3, T4, T5>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout;

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator, TimeSpan? timeout = null)
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public PromiseServiceBuilder<T1, T2, T3, T4, T5> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public IServicePromise<(T1, T2, T3, T4, T5)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1, T2, T3, T4, T5>(cancellationToken, _timeout);
			}

			public PromiseServiceBuilder<T1, T2, T3, T4, T5, T6> And<T6>() where T6 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		public class PromiseServiceBuilder<T1, T2, T3, T4, T5, T6>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout;

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator, TimeSpan? timeout = null)
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public PromiseServiceBuilder<T1, T2, T3, T4, T5, T6> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public IServicePromise<(T1, T2, T3, T4, T5, T6)> WithCancellation(
				CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1, T2, T3, T4, T5, T6>(cancellationToken, _timeout);
			}
		}
		#endif
	}
}