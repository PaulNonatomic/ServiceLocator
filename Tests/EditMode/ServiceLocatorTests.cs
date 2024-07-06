using System.Collections;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.EditMode
{
	[TestFixture]
	public class ServiceLocatorTests
	{
		private TestServiceLocator _serviceLocator;

		[SetUp]
		public void Setup()
		{
			_serviceLocator = ScriptableObject.CreateInstance<TestServiceLocator>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_serviceLocator);
		}

		[Test]
		public void Register_And_TryGetService_Success()
		{
			var service = new TestService();
			_serviceLocator.Register(service);

			Assert.IsTrue(_serviceLocator.TryGetService(out TestService retrievedService));
			Assert.AreEqual(service, retrievedService);
		}

		[Test]
		public void TryGetService_Failure_WhenServiceNotRegistered()
		{
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService service));
			Assert.IsNull(service);
		}

		[Test]
		public void Unregister_RemovesService()
		{
			var service = new TestService();
			_serviceLocator.Register(service);
			_serviceLocator.Unregister<TestService>();

			Assert.IsFalse(_serviceLocator.TryGetService(out TestService retrievedService));
		}

		[Test]
		public void GetServiceOrDefault_ReturnsService_WhenRegistered()
		{
			var service = new TestService();
			_serviceLocator.Register(service);

			var retrievedService = _serviceLocator.GetServiceOrDefault<TestService>();
			Assert.AreEqual(service, retrievedService);
		}

		[Test]
		public void GetServiceOrDefault_ReturnsNull_WhenNotRegistered()
		{
			var retrievedService = _serviceLocator.GetServiceOrDefault<TestService>();
			Assert.IsNull(retrievedService);
		}

		[UnityTest]
		public IEnumerator GetServiceAsync_ReturnsService_WhenRegistered()
		{
			var service = new TestService();
			_serviceLocator.Register(service);

			var task = _serviceLocator.GetServiceAsync<TestService>();

			while (!task.IsCompleted) yield return null;

			Assert.AreEqual(service, task.Result);
		}

		[UnityTest]
		public IEnumerator GetServiceAsync_WaitsForService_WhenNotImmediatelyAvailable()
		{
			var task = _serviceLocator.GetServiceAsync<TestService>();

			yield return null;

			var service = new TestService();
			_serviceLocator.Register(service);

			while (!task.IsCompleted) yield return null;

			Assert.AreEqual(service, task.Result);
		}

		[UnityTest]
		public IEnumerator GetService_Promise_ReturnsService_WhenRegistered()
		{
			var service = new TestService();
			_serviceLocator.Register(service);

			TestService retrievedService = null;
			var promise = _serviceLocator.GetService<TestService>();

			promise.Then(s => retrievedService = s).Catch(ex => Assert.Fail(ex.Message));

			yield return new WaitUntil(() => retrievedService != null);

			Assert.AreEqual(service, retrievedService);
		}

		[UnityTest]
		public IEnumerator GetService_Promise_WaitsForService_WhenNotImmediatelyAvailable()
		{
			TestService retrievedService = null;
			var promise = _serviceLocator.GetService<TestService>();

			promise.Then(s => retrievedService = s).Catch(ex => Assert.Fail(ex.Message));

			yield return null;

			var service = new TestService();
			_serviceLocator.Register(service);

			yield return new WaitUntil(() => retrievedService != null);

			Assert.AreEqual(service, retrievedService);
		}

		[Test]
		public void CleanupServiceLocator_ClearsAllServices()
		{
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);

			_serviceLocator.CleanupServiceLocator();

			Assert.IsFalse(_serviceLocator.TryGetService(out TestService retrievedService1));
			Assert.IsFalse(_serviceLocator.TryGetService(out AnotherTestService retrievedService2));
		}

		[UnityTest]
		public IEnumerator GetServiceCoroutine_Success()
		{
			var service = new TestService();
			_serviceLocator.Register(service);

			TestService retrievedService = null;
			var coroutine = _serviceLocator.GetServiceCoroutine<TestService>(s => retrievedService = s);

			while (coroutine.MoveNext()) yield return null;

			Assert.AreEqual(service, retrievedService);
		}

		[UnityTest]
		public IEnumerator GetServiceCoroutine_WaitsForService_WhenNotImmediatelyAvailable()
		{
			TestService retrievedService = null;
			var coroutine = _serviceLocator.GetServiceCoroutine<TestService>(s => retrievedService = s);

			// Simulate running the coroutine for a few frames
			for (var i = 0; i < 3; i++)
			{
				coroutine.MoveNext();
				yield return null;
			}

			// Service hasn't been registered yet, so retrievedService should still be null
			Assert.IsNull(retrievedService);

			var service = new TestService();
			_serviceLocator.Register(service);

			// Run the coroutine until completion
			while (coroutine.MoveNext()) yield return null;

			Assert.AreEqual(service, retrievedService);
		}
		
		[Test]
		public void InitializeAndDeInitializeServiceLocator_ChangesState()
		{
			// Initially, the ServiceLocator should not be initialized
			Assert.IsFalse(_serviceLocator.IsInitialized);

			_serviceLocator.ForceInitialize();
			// After initialization, scene cleanup should be registered
			Assert.IsTrue(_serviceLocator.IsInitialized);

			_serviceLocator.ForceDeInitialize();
			// After de-initialization, scene cleanup should not be registered
			Assert.IsFalse(_serviceLocator.IsInitialized);
		}

		private class TestService { }
		private class AnotherTestService { }

		// Helper class to expose protected methods for testing
		private class TestServiceLocator : BaseServiceLocator
		{
			public void ForceInitialize() 
			{
				InitializeServiceLocator();
				IsInitialized = true;
			}

			public void ForceDeInitialize() 
			{
				DeInitializeServiceLocator();
				IsInitialized = false;
			}

			public new void OnEnable() => base.OnEnable();
			public new void OnDisable() => base.OnDisable();

			// Override to prevent automatic initialization
			protected override void InitializeServiceLocator()
			{
				// Do nothing, allowing manual control in tests
			}
		}
	}
}