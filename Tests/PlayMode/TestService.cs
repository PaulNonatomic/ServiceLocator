using System;

namespace Tests.PlayMode
{
	/// <summary>
	///     Shared test classes and utilities for Service Locator tests.
	///     This keeps common test components in one place to avoid duplication.
	/// </summary>
	public static class ServiceLocatorTestUtils
	{
		/// <summary>
		///     Basic test service
		/// </summary>
		public class TestService
		{
			public string Message { get; set; } = "Hello from TestService!";
		}

		/// <summary>
		///     Another test service
		/// </summary>
		public class AnotherTestService
		{
		}

		/// <summary>
		///     Third test service
		/// </summary>
		public class ThirdTestService
		{
		}

		/// <summary>
		///     Test service that implements IDisposable
		/// </summary>
		public class DisposableTestService : IDisposable
		{
			public bool Disposed { get; private set; }

			public void Dispose()
			{
				Disposed = true;
			}
		}

		/// <summary>
		///     Base service for inheritance tests
		/// </summary>
		public class BaseTestService
		{
		}

		/// <summary>
		///     Derived service for inheritance tests
		/// </summary>
		public class DerivedTestService : BaseTestService
		{
		}

		/// <summary>
		///     Interface for interface testing
		/// </summary>
		public interface ITestServiceInterface
		{
		}

		/// <summary>
		///     Implementation of test interface
		/// </summary>
		public class InterfaceImplementingService : ITestServiceInterface
		{
		}
	}
}