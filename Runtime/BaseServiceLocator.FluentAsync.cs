#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
// Required for TimeSpan

namespace Nonatomic.ServiceLocator
{
	///BaseServiceLocator.FluentAsync.cs
	public abstract partial class BaseServiceLocator
	{
		/// <summary>
		///     Gets a service asynchronously and returns a builder for chaining
		/// </summary>
		public virtual ServiceBuilder<T1> GetAsync<T1>() where T1 : class
		{
			return new(this);
		}

		/// <summary>
		///     Builder class for fluent async service retrieval
		/// </summary>
		public class ServiceBuilder<T1> where T1 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout; // Store timeout

			public ServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			/// <summary>
			///     Specifies a timeout for the service retrieval operation.
			/// </summary>
			/// <param name="timeout">The duration to wait before timing out.</param>
			/// <returns>The current builder instance for chaining.</returns>
			public ServiceBuilder<T1> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			/// <summary>
			///     Executes the service retrieval with an optional cancellation token.
			/// </summary>
			/// <param name="cancellationToken">Optional token to cancel the operation.</param>
			/// <returns>A task representing the asynchronous operation, with the service as its result.</returns>
			public async Task<T1> WithCancellation(CancellationToken cancellationToken = default)
			{
				// Pass the stored timeout to GetServiceAsync
				return await _serviceLocator.GetServiceAsync<T1>(cancellationToken, _timeout);
			}

			public ServiceBuilder<T1, T2> AndAsync<T2>() where T2 : class
			{
				// Pass the current timeout to the next builder if it's set
				return new(_serviceLocator, _timeout);
			}
		}

		/// <summary>
		///     Builder for two services
		/// </summary>
		public class ServiceBuilder<T1, T2>
			where T1 : class
			where T2 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout;

			public ServiceBuilder(BaseServiceLocator serviceLocator,
				TimeSpan? timeout = null) // Accept timeout from previous builder
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public ServiceBuilder<T1, T2> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public async Task<(T1, T2)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return await _serviceLocator.GetServicesAsync<T1, T2>(cancellationToken, _timeout);
			}

			public ServiceBuilder<T1, T2, T3> AndAsync<T3>() where T3 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		/// <summary>
		///     Builder for three services
		/// </summary>
		public class ServiceBuilder<T1, T2, T3>
			where T1 : class
			where T2 : class
			where T3 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout;

			public ServiceBuilder(BaseServiceLocator serviceLocator, TimeSpan? timeout = null)
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public ServiceBuilder<T1, T2, T3> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public async Task<(T1, T2, T3)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return await _serviceLocator.GetServicesAsync<T1, T2, T3>(cancellationToken, _timeout);
			}

			public ServiceBuilder<T1, T2, T3, T4> AndAsync<T4>() where T4 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		/// <summary>
		///     Builder for four services
		/// </summary>
		public class ServiceBuilder<T1, T2, T3, T4>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout;

			public ServiceBuilder(BaseServiceLocator serviceLocator, TimeSpan? timeout = null)
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public ServiceBuilder<T1, T2, T3, T4> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public async Task<(T1, T2, T3, T4)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return await _serviceLocator.GetServicesAsync<T1, T2, T3, T4>(cancellationToken, _timeout);
			}

			public ServiceBuilder<T1, T2, T3, T4, T5> AndAsync<T5>() where T5 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		/// <summary>
		///     Builder for five services
		/// </summary>
		public class ServiceBuilder<T1, T2, T3, T4, T5>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout;

			public ServiceBuilder(BaseServiceLocator serviceLocator, TimeSpan? timeout = null)
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public ServiceBuilder<T1, T2, T3, T4, T5> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public async Task<(T1, T2, T3, T4, T5)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return await _serviceLocator.GetServicesAsync<T1, T2, T3, T4, T5>(cancellationToken, _timeout);
			}

			public ServiceBuilder<T1, T2, T3, T4, T5, T6> AndAsync<T6>() where T6 : class
			{
				return new(_serviceLocator, _timeout);
			}
		}

		/// <summary>
		///     Builder for six services
		/// </summary>
		public class ServiceBuilder<T1, T2, T3, T4, T5, T6>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			private readonly BaseServiceLocator _serviceLocator;
			private TimeSpan? _timeout;

			public ServiceBuilder(BaseServiceLocator serviceLocator, TimeSpan? timeout = null)
			{
				_serviceLocator = serviceLocator;
				_timeout = timeout;
			}

			public ServiceBuilder<T1, T2, T3, T4, T5, T6> WithTimeout(TimeSpan timeout)
			{
				_timeout = timeout;
				return this;
			}

			public async Task<(T1, T2, T3, T4, T5, T6)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return await _serviceLocator.GetServicesAsync<T1, T2, T3, T4, T5, T6>(cancellationToken, _timeout);
			}
		}
	}
}