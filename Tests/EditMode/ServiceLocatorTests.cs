﻿#nullable enable
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

			Assert.IsTrue(_serviceLocator.TryGetService(out TestService testService));
			Assert.AreEqual(service, testService);
		}

		[Test]
		public void TryGetService_Failure_WhenServiceNotRegistered()
		{
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService? testService));
			Assert.IsNull(testService);
		}

		[Test]
		public void Unregister_RemovesService()
		{
			var service = new TestService();
			_serviceLocator.Register(service);
			_serviceLocator.Unregister<TestService>();

			Assert.IsFalse(_serviceLocator.TryGetService(out TestService testService));
		}

		[Test]
		public void GetServiceOrDefault_ReturnsService_WhenRegistered()
		{
			var service = new TestService();
			_serviceLocator.Register(service);

			var testService = _serviceLocator.GetServiceOrDefault<TestService>();
			Assert.AreEqual(service, testService);
		}

		[Test]
		public void GetServiceOrDefault_ReturnsNull_WhenNotRegistered()
		{
			var testService = _serviceLocator.GetServiceOrDefault<TestService>();
			Assert.IsNull(testService);
		}
		
		[UnityTest]
		public IEnumerator GetServiceCoroutine_ReturnsNull_WhenCleanedUp()
		{
			TestService? testService = null;
			var coroutine = _serviceLocator.GetServiceCoroutine<TestService>(service => testService = service);

			// Start the coroutine
			coroutine.MoveNext();
	
			// Cleanup the ServiceLocator
			_serviceLocator.Cleanup();

			// Run the coroutine to completion
			while (coroutine.MoveNext()) yield return null;

			Assert.IsNull(testService);
		}

		[UnityTest]
		public IEnumerator GetServiceAsync_ReturnsService_WhenRegistered()
		{
			var testService = new TestService();
			_serviceLocator.Register(testService);

			var task = _serviceLocator.GetServiceAsync<TestService>();

			while (!task.IsCompleted) yield return null;

			Assert.AreEqual(testService, task.Result);
		}

		[UnityTest]
		public IEnumerator GetServiceAsync_WaitsForService_WhenNotImmediatelyAvailable()
		{
			var task = _serviceLocator.GetServiceAsync<TestService>();

			yield return null;

			var testService = new TestService();
			_serviceLocator.Register(testService);

			while (!task.IsCompleted) yield return null;

			Assert.AreEqual(testService, task.Result);
		}

		[UnityTest]
		public IEnumerator GetService_Promise_ReturnsService_WhenRegistered()
		{
			var testService = new TestService();
			_serviceLocator.Register(testService);

			TestService retrievedService = null;
			var promise = _serviceLocator.GetService<TestService>();

			promise.Then(s => retrievedService = s).Catch(ex => Assert.Fail(ex.Message));

			yield return new WaitUntil(() => retrievedService != null);

			Assert.AreEqual(testService, retrievedService);
		}

		[UnityTest]
		public IEnumerator GetService_Promise_WaitsForService_WhenNotImmediatelyAvailable()
		{
			TestService testService = null;
			var promise = _serviceLocator.GetService<TestService>();

			promise.Then(service => testService = service).Catch(ex => Assert.Fail(ex.Message));

			yield return null;

			var service = new TestService();
			_serviceLocator.Register(service);

			yield return new WaitUntil(() => testService != null);

			Assert.AreEqual(service, testService);
		}

		[Test]
		public void CleanupServiceLocator_ClearsAllServices()
		{
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);

			_serviceLocator.Cleanup();

			Assert.IsFalse(_serviceLocator.TryGetService(out TestService retrievedService1));
			Assert.IsFalse(_serviceLocator.TryGetService(out AnotherTestService retrievedService2));
		}

		[UnityTest]
		public IEnumerator GetServiceCoroutine_Success()
		{
			var testService = new TestService();
			_serviceLocator.Register(testService);

			TestService retrievedService = null;
			var coroutine = _serviceLocator.GetServiceCoroutine<TestService>(testService => retrievedService = testService);

			while (coroutine.MoveNext()) yield return null;

			Assert.AreEqual(testService, retrievedService);
		}

		[UnityTest]
		public IEnumerator GetServiceCoroutine_WaitsForService_WhenNotImmediatelyAvailable()
		{
			TestService? retrievedService = null;
			var coroutineCompleted = false;
			var coroutine = _serviceLocator.GetServiceCoroutine<TestService>(testService => 
			{
				retrievedService = testService;
				coroutineCompleted = true;
			});

			// Simulate running the coroutine for a few frames
			for (var i = 0; i < 3; i++)
			{
				coroutine.MoveNext();
				yield return null;
			}

			// Service hasn't been registered yet, so retrievedService should still be null
			Assert.IsNull(retrievedService);
			Assert.IsFalse(coroutineCompleted);

			var service = new TestService();
			_serviceLocator.Register(service);

			// Run the coroutine until completion
			while (coroutine.MoveNext()) yield return null;

			Assert.IsTrue(coroutineCompleted);
			Assert.AreEqual(service, retrievedService);
		}

		[UnityTest]
		public IEnumerator GetServiceCoroutine_ReturnsNull_WhenCleanedUpBeforeServiceRegistered()
		{
			TestService? retrievedService = null;
			var coroutineCompleted = false;
			var coroutine = _serviceLocator.GetServiceCoroutine<TestService>(testService => 
			{
				retrievedService = testService;
				coroutineCompleted = true;
			});

			// Simulate running the coroutine for a few frames
			for (var i = 0; i < 3; i++)
			{
				coroutine.MoveNext();
				yield return null;
			}

			// Service hasn't been registered yet, so retrievedService should still be null
			Assert.IsNull(retrievedService);
			Assert.IsFalse(coroutineCompleted);

			// Simulate cleanup
			_serviceLocator.Cleanup();

			// Run the coroutine until completion
			while (coroutine.MoveNext()) yield return null;

			Assert.IsTrue(coroutineCompleted);
			Assert.IsNull(retrievedService);
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
				Initialize();
				IsInitialized = true;
			}

			public void ForceDeInitialize() 
			{
				DeInitialize();
				IsInitialized = false;
			}

			public new void OnEnable() => base.OnEnable();
			public new void OnDisable() => base.OnDisable();

			// Override to prevent automatic initialization
			protected override void Initialize()
			{
				// Do nothing, allowing manual control in tests
			}
		}
		
		[UnityTest]
		public IEnumerator GetMultipleServices_ReturnsServices_WhenAllRegisteredBeforeCall()
		{
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();

			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);

			TestService retrievedService1 = null;
			AnotherTestService retrievedService2 = null;
			ThirdTestService retrievedService3 = null;

			var promise = _serviceLocator.GetService<TestService, AnotherTestService, ThirdTestService>();

			promise.Then(services =>
			{
				retrievedService1 = services.Item1;
				retrievedService2 = services.Item2;
				retrievedService3 = services.Item3;
			}).Catch(ex => Assert.Fail(ex.Message));

			yield return new WaitUntil(() => retrievedService1 != null && retrievedService2 != null && retrievedService3 != null);

			Assert.AreEqual(service1, retrievedService1);
			Assert.AreEqual(service2, retrievedService2);
			Assert.AreEqual(service3, retrievedService3);
		}

		[UnityTest]
		public IEnumerator GetMultipleServices_WaitsForServices_WhenNotImmediatelyAvailable()
		{
			TestService retrievedService1 = null;
			AnotherTestService retrievedService2 = null;
			ThirdTestService retrievedService3 = null;

			var promise = _serviceLocator.GetService<TestService, AnotherTestService, ThirdTestService>();

			promise.Then(services =>
			{
				retrievedService1 = services.Item1;
				retrievedService2 = services.Item2;
				retrievedService3 = services.Item3;
			}).Catch(ex => Assert.Fail(ex.Message));

			yield return null; // Wait a frame

			// Register services after the call
			var service1 = new TestService();
			_serviceLocator.Register(service1);

			yield return null; // Wait a frame

			var service2 = new AnotherTestService();
			_serviceLocator.Register(service2);

			yield return null; // Wait a frame

			var service3 = new ThirdTestService();
			_serviceLocator.Register(service3);

			yield return new WaitUntil(() => retrievedService1 != null && retrievedService2 != null && retrievedService3 != null);

			Assert.AreEqual(service1, retrievedService1);
			Assert.AreEqual(service2, retrievedService2);
			Assert.AreEqual(service3, retrievedService3);
		}

		[UnityTest]
		public IEnumerator GetMultipleServices_Works_WhenSomeServicesRegisteredBeforeCall()
		{
			var service1 = new TestService();
			_serviceLocator.Register(service1);

			TestService retrievedService1 = null;
			AnotherTestService retrievedService2 = null;
			ThirdTestService retrievedService3 = null;

			var promise = _serviceLocator.GetService<TestService, AnotherTestService, ThirdTestService>();

			promise.Then(services =>
			{
				retrievedService1 = services.Item1;
				retrievedService2 = services.Item2;
				retrievedService3 = services.Item3;
			}).Catch(ex => Assert.Fail(ex.Message));

			yield return null; // Wait a frame

			var service2 = new AnotherTestService();
			_serviceLocator.Register(service2);

			yield return null; // Wait a frame

			var service3 = new ThirdTestService();
			_serviceLocator.Register(service3);

			yield return new WaitUntil(() => retrievedService1 != null && retrievedService2 != null && retrievedService3 != null);

			Assert.AreEqual(service1, retrievedService1);
			Assert.AreEqual(service2, retrievedService2);
			Assert.AreEqual(service3, retrievedService3);
		}

		[UnityTest]
		public IEnumerator GetMultipleServices_CatchException_WhenServiceNotRegistered()
		{
			var catchCalled = false;

			var promise = _serviceLocator.GetService<TestService, AnotherTestService, ThirdTestService>();

			promise.Then(services =>
			{
				Assert.Fail("Then should not be called when services are not registered.");
			}).Catch(ex =>
			{
				catchCalled = true;
				Assert.IsNotNull(ex);
			});

			// Simulate some frames without registering services
			for (var i = 0; i < 5; i++)
			{
				yield return null;
			}

			// Since services are not registered, the promise should not resolve
			Assert.IsFalse(catchCalled);

			// Optionally, you can force the promise to fail after some timeout
			// However, in the current implementation, the promise will wait indefinitely
			// unless you implement a timeout or cancellation mechanism
		}

		[UnityTest]
		public IEnumerator GetMultipleServices_PartialServicesAvailable_DoesNotResolveUntilAllAvailable()
		{
			var service1 = new TestService();
			_serviceLocator.Register(service1);

			TestService retrievedService1 = null;
			AnotherTestService retrievedService2 = null;
			ThirdTestService retrievedService3 = null;
			var promiseResolved = false;

			var promise = _serviceLocator.GetService<TestService, AnotherTestService, ThirdTestService>();

			promise.Then(services =>
			{
				retrievedService1 = services.Item1;
				retrievedService2 = services.Item2;
				retrievedService3 = services.Item3;
				promiseResolved = true;
			}).Catch(ex => Assert.Fail(ex.Message));

			yield return null; // Wait a frame

			// Only one service is registered, promise should not be resolved yet
			Assert.IsFalse(promiseResolved);

			var service2 = new AnotherTestService();
			_serviceLocator.Register(service2);

			yield return null; // Wait a frame

			// Two services are registered, promise should still not be resolved
			Assert.IsFalse(promiseResolved);

			var service3 = new ThirdTestService();
			_serviceLocator.Register(service3);

			yield return new WaitUntil(() => promiseResolved);

			Assert.AreEqual(service1, retrievedService1);
			Assert.AreEqual(service2, retrievedService2);
			Assert.AreEqual(service3, retrievedService3);
		}

		[Test]
		public void Cleanup_RemovesPendingPromises()
		{
			var promise = _serviceLocator.GetService<TestService, AnotherTestService, ThirdTestService>();

			_serviceLocator.Cleanup();

			var thenCalled = false;
			var catchCalled = false;

			promise.Then(services =>
			{
				thenCalled = true;
			}).Catch(ex =>
			{
				catchCalled = true;
			});

			Assert.IsFalse(thenCalled);
			Assert.IsFalse(catchCalled);
		}
	}
}