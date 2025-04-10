#nullable enable
using System.Threading;

namespace Nonatomic.ServiceLocator
{
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

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IServicePromise<T1> WithCancellation(CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1>(cancellationToken);
			}

			public PromiseServiceBuilder<T1, T2> And<T2>() where T2 : class
			{
				return new(_serviceLocator);
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

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IServicePromise<(T1, T2)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1, T2>(cancellationToken);
			}

			public PromiseServiceBuilder<T1, T2, T3> And<T3>() where T3 : class
			{
				return new(_serviceLocator);
			}
		}

		/// <summary>
		///     Promise builder for three services
		/// </summary>
		public class PromiseServiceBuilder<T1, T2, T3>
			where T1 : class
			where T2 : class
			where T3 : class
		{
			private readonly BaseServiceLocator _serviceLocator;

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IServicePromise<(T1, T2, T3)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1, T2, T3>(cancellationToken);
			}

			public PromiseServiceBuilder<T1, T2, T3, T4> And<T4>() where T4 : class
			{
				return new(_serviceLocator);
			}
		}

		/// <summary>
		///     Promise builder for four services
		/// </summary>
		public class PromiseServiceBuilder<T1, T2, T3, T4>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
		{
			private readonly BaseServiceLocator _serviceLocator;

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IServicePromise<(T1, T2, T3, T4)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1, T2, T3, T4>(cancellationToken);
			}

			public PromiseServiceBuilder<T1, T2, T3, T4, T5> And<T5>() where T5 : class
			{
				return new(_serviceLocator);
			}
		}

		/// <summary>
		///     Promise builder for five services
		/// </summary>
		public class PromiseServiceBuilder<T1, T2, T3, T4, T5>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
		{
			private readonly BaseServiceLocator _serviceLocator;

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IServicePromise<(T1, T2, T3, T4, T5)> WithCancellation(CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1, T2, T3, T4, T5>(cancellationToken);
			}

			public PromiseServiceBuilder<T1, T2, T3, T4, T5, T6> And<T6>() where T6 : class
			{
				return new(_serviceLocator);
			}
		}

		/// <summary>
		///     Promise builder for six services
		/// </summary>
		public class PromiseServiceBuilder<T1, T2, T3, T4, T5, T6>
			where T1 : class
			where T2 : class
			where T3 : class
			where T4 : class
			where T5 : class
			where T6 : class
		{
			private readonly BaseServiceLocator _serviceLocator;

			public PromiseServiceBuilder(BaseServiceLocator serviceLocator)
			{
				_serviceLocator = serviceLocator;
			}

			public IServicePromise<(T1, T2, T3, T4, T5, T6)> WithCancellation(
				CancellationToken cancellationToken = default)
			{
				return _serviceLocator.GetService<T1, T2, T3, T4, T5, T6>(cancellationToken);
			}
		}
		#endif
	}
}