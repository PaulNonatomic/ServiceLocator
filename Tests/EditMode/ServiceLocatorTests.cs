using UnityEngine;

namespace Tests.EditMode
{
	using NUnit.Framework;
	using Nonatomic.ServiceLocator;

	public class ServiceLocatorTests
	{
		private ServiceLocator _serviceLocator;

		[SetUp]
		public void SetUp()
		{
			_serviceLocator = ScriptableObject.CreateInstance<ServiceLocator>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_serviceLocator == null) return;
			ScriptableObject.DestroyImmediate(_serviceLocator);
		}

		[Test]
		public void RegisterService_ServiceIsRegistered()
		{
			var service = new object();
			_serviceLocator.Register(service);

			// Assert that the service is registered
			var isServiceRegistered = _serviceLocator.TryGetService<object>(out var retrievedService);
			Assert.IsTrue(isServiceRegistered);
			Assert.AreEqual(service, retrievedService);
		}

		[Test]
		public void UnregisterService_ServiceIsUnregistered()
		{
			var service = new object();
			_serviceLocator.Register(service);
			_serviceLocator.Unregister(service);

			// Assert that the service is no longer registered
			var isServiceRegistered = _serviceLocator.TryGetService<object>(out var retrievedService);
			Assert.IsFalse(isServiceRegistered);
		}

		[Test]
		public void GetService_RegisteredService_ReturnsFulfilledPromise()
		{
			var service = new object();
			_serviceLocator.Register(service);

			var promise = _serviceLocator.GetService<object>();
			Assert.IsTrue(promise.Task.IsCompleted);
			Assert.AreEqual(service, promise.Task.Result);
		}

		[Test]
		public void GetService_UnregisteredService_ReturnsUnfulfilledPromise()
		{
			var promise = _serviceLocator.GetService<object>();
			Assert.IsFalse(promise.Task.IsCompleted);
		}

		[Test]
		public void RegisterService_FulfillsPendingPromise()
		{
			var service = new object();
			var promise = _serviceLocator.GetService<object>();

			_serviceLocator.Register(service);

			Assert.IsTrue(promise.Task.IsCompleted);
			Assert.AreEqual(service, promise.Task.Result);
		}
	}
}