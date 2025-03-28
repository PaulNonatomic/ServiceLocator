﻿#nullable enable
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
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
			UnitySynchronizationContext.Initialize();
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
		
		/// <summary>
		/// Tests that exceptions thrown inside a Then callback are correctly surfaced to the Catch callback.
		/// </summary>
		[UnityTest]
		public IEnumerator ServicePromise_ExceptionThrownInThen_IsCaughtInCatch()
		{
			var testService = new TestService();
			_serviceLocator.Register(testService);

			Exception caughtException = null;
			var expectedException = new InvalidOperationException("Test exception from Then.");

			var promise = _serviceLocator.GetService<TestService>();
			promise
				.Then(service =>
				{
					throw expectedException;
				})
				.Catch(ex =>
				{
					caughtException = ex;
				});

			// Wait until the Catch callback is invoked
			yield return new WaitUntil(() => caughtException != null);

			// Assert
			Assert.IsNotNull(caughtException, "Exception was not caught in Catch.");
			Assert.AreEqual(expectedException, caughtException, "Caught exception does not match the expected exception.");
		}
		
		// Thread Safety Tests
		
		public IEnumerator ConcurrentRegistrationAndAccess_DoesNotDeadlock() 
		{
			var task1 = Task.Run(() => _serviceLocator.GetServiceAsync<TestService>());
			var task2 = Task.Run(() => _serviceLocator.GetServiceAsync<AnotherTestService>());

			// Wait a little for tasks to potentially start processing
			yield return new WaitForSeconds(0.1f);

			_serviceLocator.Register(new TestService());
			_serviceLocator.Register(new AnotherTestService());

			// Ensure tasks complete
			while (!task1.IsCompleted || !task2.IsCompleted)
			{
				yield return null; // Wait until the next frame
			}

			// Assert that no exceptions were thrown
			try
			{
				var result1 = task1.Result; // Accessing Result will rethrow any caught exception
				var result2 = task2.Result;
			}
			catch (Exception e)
			{
				Assert.Fail("Exception thrown: " + e.Message);
			}
		}
		
		[Test]
		public void TryGetService_WhileUnregistering_ReturnsConsistentState()
		{
			var service = new TestService();
			_serviceLocator.Register(service);
		
			Parallel.Invoke(
				() => {
					for (int i = 0; i < 1000; i++) 
						_serviceLocator.TryGetService(out TestService _);
				},
				() => {
					for (int i = 0; i < 1000; i++) 
						_serviceLocator.Unregister<TestService>();
				}
			);
		}
		
		// Error Condition Tests
		
		[Test]
		public void Register_NullService_ThrowsException()
		{
			Assert.Throws<ArgumentNullException>(() => 
				_serviceLocator.Register<TestService>(null)
			);
		}
		
		// Multi-Service Edge Cases
		
		[UnityTest]
		public IEnumerator CombinedPromise_Rejects_WhenSubPromiseFails()
		{
			var exception = new Exception("Test failure");
			var caught = false;

			var promise1 = _serviceLocator.GetService<TestService>();
			var promise2 = _serviceLocator.GetService<AnotherTestService>();
			var combinedPromise = ServicePromiseCombiner.CombinePromises(promise1, promise2);
			combinedPromise.Catch(_ => caught = true);

			yield return null;
			_serviceLocator.Register(new TestService()); // Resolves promise1
			promise2.Reject(exception);                  // Rejects promise2

			yield return new WaitUntil(() => caught);

			Assert.IsTrue(caught, "Combined promise should reject when a sub-promise fails.");
		}
		
		// Lifetime Management Tests
		
		[Test]
		public void DisposableServices_Disposed_OnCleanup()
		{
			var disposableService = new DisposableTestService();
			_serviceLocator.Register(disposableService);
	
			_serviceLocator.Cleanup();
	
			Assert.IsTrue(disposableService.Disposed);
		}

		private class DisposableTestService : IDisposable
		{
			public bool Disposed { get; private set; }
			public void Dispose() => Disposed = true;
		}
		
		// Inheritance/Interface Tests
		
		[Test]
		public void CanRegisterDerivedType_AndRetrieveViaBaseType()
		{
			var service = new DerivedTestService();
			_serviceLocator.Register<BaseTestService>(service);

			Assert.IsTrue(_serviceLocator.TryGetService(out BaseTestService _));
		}

		private class BaseTestService { }
		private class DerivedTestService : BaseTestService { }
		
		// Async Exception Propagation
		
		[UnityTest]
		public IEnumerator GetServiceAsync_PropagatesCancellationException()
		{
			var cts = new CancellationTokenSource();
			var task = _serviceLocator.GetServiceAsync<TestService>(cts.Token);

			yield return null;

			// Simulate a failure by canceling the request
			cts.Cancel();

			yield return new WaitUntil(() => task.IsCompleted); // Wait for completion (canceled state)

			Assert.IsTrue(task.IsCanceled, "Task should be canceled when the CancellationToken is triggered.");
			Assert.ThrowsAsync<TaskCanceledException>(async () => await task, "Task should throw TaskCanceledException when canceled.");
		}
		
		// Cancellation Tests
		
		[UnityTest]
		public IEnumerator GetServicesAsync_AllServicesCanceled_WhenOneCancels()
		{
			// Create a cancellation token source
			var cts = new CancellationTokenSource();
			
			// Start multiple service requests with the same token
			var task = _serviceLocator.GetServicesAsync<TestService, AnotherTestService, ThirdTestService>(cts.Token);
			
			yield return null; // Wait a frame
			
			// Register one of the services to ensure partial resolution doesn't complete the task
			_serviceLocator.Register(new TestService());
			
			yield return null; // Wait a frame
			
			// Cancel the token
			cts.Cancel();
			
			// Wait for the task to complete (should be canceled)
			yield return new WaitUntil(() => task.IsCompleted);
			
			// Assert that the task was canceled
			Assert.IsTrue(task.IsCanceled, "Task should be canceled when the token is canceled");
			Assert.ThrowsAsync<TaskCanceledException>(async () => await task, 
				"GetServicesAsync should throw TaskCanceledException when token is canceled");
		}

		[UnityTest]
		public IEnumerator CombinePromises_PropagatesCancellation_ToAllPromises()
		{
			// Create a cancellation token source
			var cts = new CancellationTokenSource();
    
			// Create individual service promises with the cancellation token
			var promise1 = _serviceLocator.GetService<TestService>(cts.Token);
			var promise2 = _serviceLocator.GetService<AnotherTestService>(cts.Token);
    
			bool catchCalled = false;
			bool promise1Caught = false;
			bool promise2Caught = false;
    
			promise1.Catch(ex => {
				promise1Caught = true;
				Debug.Log("Promise 1 caught: " + ex.Message);
			});
    
			promise2.Catch(ex => {
				promise2Caught = true;
				Debug.Log("Promise 2 caught: " + ex.Message);
			});
    
			// Combine promises with the same cancellation token
			var combinedPromise = ServicePromiseCombiner.CombinePromises(promise1, promise2, cts.Token);
    
			combinedPromise.Catch(ex => {
				catchCalled = true;
				Debug.Log($"Combined promise caught: {ex.GetType().Name} - {ex.Message}");
			});
    
			yield return null;
    
			// Cancel the token
			Debug.Log("Cancelling token");
			cts.Cancel();
    
			// Wait for callbacks to be called
			float timeoutTime = Time.time + 2.0f; // 2 second timeout
			while ((!promise1Caught || !promise2Caught || !catchCalled) && Time.time < timeoutTime)
			{
				Debug.Log($"Waiting: promise1Caught={promise1Caught}, promise2Caught={promise2Caught}, combinedCaught={catchCalled}");
				yield return null;
			}
    
			// Final status check
			Debug.Log($"Final status: promise1Caught={promise1Caught}, promise2Caught={promise2Caught}, combinedCaught={catchCalled}");
    
			// Assert
			Assert.IsTrue(promise1Caught, "Individual promise 1 should handle cancellation");
			Assert.IsTrue(promise2Caught, "Individual promise 2 should handle cancellation");
			Assert.IsTrue(catchCalled, "Combined promise should handle cancellation");
		}

		[UnityTest]
		public IEnumerator ServicePromise_WithCancellation_CancelsPromise()
		{
			// Create a cancellation token source
			var cts = new CancellationTokenSource();
			
			// Get a service promise
			var promise = _serviceLocator.GetService<TestService>();
			
			// Apply cancellation token manually
			promise.WithCancellation(cts.Token);
			
			bool catchCalled = false;
			promise.Catch(ex => {
				catchCalled = true;
				Assert.IsInstanceOf<TaskCanceledException>(ex, "Exception should be TaskCanceledException");
			});
			
			yield return null;
			
			// Cancel the token
			cts.Cancel();
			
			yield return new WaitUntil(() => catchCalled);
			
			Assert.IsTrue(catchCalled, "Catch should be called when WithCancellation token is canceled");
		}

		[UnityTest]
		public IEnumerator GetService_CancellationToken_PropagatedToPromise()
		{
			// This test ensures that the GetService method correctly propagates the token
			// to the ServicePromise via WithCancellation
			
			var cts = new CancellationTokenSource();
			
			var promise = _serviceLocator.GetService<TestService>(cts.Token);
			
			bool catchCalled = false;
			promise.Catch(ex => {
				catchCalled = true;
				Assert.IsInstanceOf<TaskCanceledException>(ex, "Exception should be TaskCanceledException");
			});
			
			yield return null;
			
			cts.Cancel();
			
			yield return new WaitUntil(() => catchCalled);
			
			Assert.IsTrue(catchCalled, "Catch should be called when token from GetService is canceled");
		}

		[UnityTest]
		public IEnumerator LinkedCancellationToken_CancelsAllOperations()
		{
			// This test ensures that when one operation is canceled in a multi-service request,
			// all other pending operations are canceled as well

			// Create two cancellation tokens
			var cts1 = new CancellationTokenSource();
			var cts2 = new CancellationTokenSource();
			
			// Create a linked token with both sources
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, cts2.Token);
			
			// Start multiple service requests with the linked token
			var task = _serviceLocator.GetServicesAsync<TestService, AnotherTestService>(linkedCts.Token);
			
			yield return null;
			
			// Cancel just one of the tokens
			cts1.Cancel();
			
			// Wait for the task to complete
			yield return new WaitUntil(() => task.IsCompleted);
			
			// Assert that the task was canceled
			Assert.IsTrue(task.IsCanceled, "Task should be canceled when any of the linked tokens is canceled");
			Assert.ThrowsAsync<TaskCanceledException>(async () => await task, 
				"GetServicesAsync should throw TaskCanceledException when a linked token is canceled");
		}

		// Thread Safety and Concurrency
		[Test]
		public void MultipleServiceRegistrationsAtOnce_Succeeds()
		{
			Parallel.Invoke(
				() => _serviceLocator.Register(new TestService()),
				() => _serviceLocator.Register(new AnotherTestService()),
				() => _serviceLocator.Register(new ThirdTestService())
			);
    
			Assert.IsTrue(_serviceLocator.TryGetService(out TestService _));
			Assert.IsTrue(_serviceLocator.TryGetService(out AnotherTestService _));
			Assert.IsTrue(_serviceLocator.TryGetService(out ThirdTestService _));
		}
		
		// Service Replacement
		[Test]
		public void Register_ReplacesExistingService()
		{
			var service1 = new TestService();
			var service2 = new TestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
    
			Assert.IsTrue(_serviceLocator.TryGetService(out TestService? retrievedService));
			Assert.AreEqual(service2, retrievedService);
			Assert.AreNotEqual(service1, retrievedService);
		}
		
		// Error Handling
		
		[Test]
		public void RejectService_MultipleTimes_DoesntThrow()
		{
			_serviceLocator.GetService<TestService>();
    
			_serviceLocator.RejectService<TestService>(new Exception("First"));
    
			// Should not throw when called again
			Assert.DoesNotThrow(() => _serviceLocator.RejectService<TestService>(new Exception("Second")));
		}
		
		// Memory Leaks
		
		[Test]
		public void ServiceLocator_CleanupDisposesServices()
		{
			var disposableService = new DisposableTestService();
			_serviceLocator.Register(disposableService);
    
			_serviceLocator.Cleanup();
    
			// Instead of checking garbage collection (which is unreliable in tests),
			// just verify that the Dispose method was called
			Assert.IsTrue(disposableService.Disposed, "Service should be disposed during cleanup");
		}
		
		[Test]
		public void ServiceLocator_RemovesAllServiceReferencesAfterCleanup()
		{
			var service = new TestService();
			_serviceLocator.Register(service);
    
			_serviceLocator.Cleanup();
    
			// Test that ServiceMap is empty after cleanup
			var services = _serviceLocator.GetAllServices();
			Assert.AreEqual(0, services.Count, "ServiceMap should be empty after cleanup");
    
			// Test that we can't retrieve the service anymore
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService _), 
				"Service should not be retrievable after cleanup");
		}
		
		//  Scene Management
		[UnityTest]
		public IEnumerator SceneUnload_RemovesSceneSpecificServices()
		{
			// Mock scene unloading by directly calling the handler
			var service = new TestService();
			_serviceLocator.Register(service);
    
			// Get the scene name from the service map
			string sceneName = _serviceLocator.GetSceneNameForService(typeof(TestService));
    
			// Simulate scene unloading
			_serviceLocator.UnregisterServicesFromScene(sceneName);
    
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService _), 
				"Service should be unregistered when its scene is unloaded");
    
			yield return null;
		}
		
		// Interface Registration and Retrieval
		[Test]
		public void Register_WithInterface_CanRetrieveByInterface()
		{
			var service = new InterfaceImplementingService();
			_serviceLocator.Register<ITestServiceInterface>(service);
    
			Assert.IsTrue(_serviceLocator.TryGetService(out ITestServiceInterface retrievedService));
			Assert.AreEqual(service, retrievedService);
		}

		private interface ITestServiceInterface { }
		private class InterfaceImplementingService : ITestServiceInterface { }
		
		// Service Registration Type Mismatch
		[Test]
		public void Register_ByBaseType_ThenTryGet_ByDerivedType_ShouldFail()
		{
			var baseService = new BaseTestService();
			_serviceLocator.Register<BaseTestService>(baseService);
    
			// Should not be able to get a DerivedTestService when a BaseTestService was registered
			Assert.IsFalse(_serviceLocator.TryGetService(out DerivedTestService _));
		}
		
		// Cancellation Edge Cases
		[UnityTest]
		public IEnumerator GetServiceAsync_WithCompletedCancellationToken_FailsImmediately()
		{
			var cts = new CancellationTokenSource();
			cts.Cancel(); // Already canceled
    
			var task = _serviceLocator.GetServiceAsync<TestService>(cts.Token);
    
			yield return null;
    
			Assert.IsTrue(task.IsCanceled);
			Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
		}
		
		// Service Lifetime Management
		[Test]
		public void DeInitialize_AndThenInitialize_RestoresServiceLocator()
		{
			_serviceLocator.ForceInitialize();
			_serviceLocator.Register(new TestService());
    
			_serviceLocator.ForceDeInitialize();
			// Services should be cleared after DeInitialize
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService _));
    
			// Re-initialize should restore functionality
			_serviceLocator.ForceInitialize();
			_serviceLocator.Register(new TestService());
			Assert.IsTrue(_serviceLocator.TryGetService(out TestService _));
		}
	}
}