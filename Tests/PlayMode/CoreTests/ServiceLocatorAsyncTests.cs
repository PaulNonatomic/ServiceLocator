#if !DISABLE_SL_ASYNC
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.CoreTests
{
	[TestFixture]
	public class ServiceLocatorAsyncTests
	{
		[SetUp]
		public void Setup()
		{
			_serviceLocator = ScriptableObject.CreateInstance<ServiceLocator>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_serviceLocator);
		}

		private ServiceLocator _serviceLocator;

		[UnityTest]
		public IEnumerator ServiceUser_CanRetrieveService_ViaAsync()
		{
			// Register the service first
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Create GameObject with async-based service user
			var gameObject = new GameObject("AsyncServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserAsync>();
			serviceUser.Initialize(_serviceLocator);

			// Give it a frame to start the async operation
			yield return null;

			// Get the task
			var task = serviceUser.GetServiceTask();

			// Wait for task to complete
			yield return new WaitUntil(() => task.IsCompleted);

			// Verify service was retrieved correctly
			Assert.IsTrue(task.IsCompleted, "Task should be completed");
			Assert.IsFalse(task.IsFaulted, "Task should not be faulted");
			Assert.IsFalse(task.IsCanceled, "Task should not be canceled");

			var retrievedService = serviceUser.GetRetrievedService();
			Assert.IsNotNull(retrievedService, "Service should be retrieved");
			Assert.AreEqual("Hello from TestService!", retrievedService.Message,
				"Service should contain the correct data");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator Async_WaitsForRegistration()
		{
			// Create GameObject with async-based service user
			var gameObject = new GameObject("AsyncServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserAsync>();
			serviceUser.Initialize(_serviceLocator);

			// Give it a frame to start the async operation
			yield return null;

			// Get the task
			var task = serviceUser.GetServiceTask();

			// Task should not be completed yet
			Assert.IsFalse(task.IsCompleted, "Task should not be completed yet");

			// Register the service after a delay
			yield return new WaitForSeconds(0.2f);
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Wait for task to complete
			yield return new WaitUntil(() => task.IsCompleted);

			// Verify service was retrieved correctly
			Assert.IsTrue(task.IsCompleted, "Task should be completed after registration");
			Assert.IsFalse(task.IsFaulted, "Task should not be faulted");
			Assert.IsFalse(task.IsCanceled, "Task should not be canceled");

			var retrievedService = serviceUser.GetRetrievedService();
			Assert.IsNotNull(retrievedService, "Service should be retrieved after registration");
			Assert.AreEqual("Hello from TestService!", retrievedService.Message,
				"Service should contain the correct data");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator Async_CancelledWhenGameObjectDestroyed()
		{
			// Create GameObject with async-based service user
			var gameObject = new GameObject("AsyncServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserAsync>();
			serviceUser.Initialize(_serviceLocator);

			// Give it a frame to start the async operation
			yield return null;

			// Get the task
			var task = serviceUser.GetServiceTask();

			// Task should not be completed yet
			Assert.IsFalse(task.IsCompleted, "Task should not be completed yet");

			// Destroy the GameObject before registering the service
			Object.Destroy(gameObject);

			// Wait for destruction to process
			yield return new WaitForEndOfFrame();

			// Wait for task to complete (should be canceled)
			yield return new WaitUntil(() => task.IsCompleted);

			// Verify task was canceled
			Assert.IsTrue(task.IsCompleted, "Task should be completed");
			Assert.IsTrue(task.IsCanceled, "Task should be canceled");
			Assert.IsFalse(task.IsFaulted, "Task should not be faulted");

			// Now register the service - this shouldn't affect the canceled task
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Wait a bit to make sure the task doesn't change state
			yield return new WaitForSeconds(0.2f);

			// Verify task is still canceled
			Assert.IsTrue(task.IsCanceled, "Task should remain canceled after service registration");

			// Verify exception is TaskCanceledException
			Assert.ThrowsAsync<TaskCanceledException>(async () => await task,
				"Task should throw TaskCanceledException when awaited");
		}

		[UnityTest]
		public IEnumerator Async_RejectsWithCustomException()
		{
			// Create rejection exception
			var customException = new InvalidOperationException("Service initialization failed");

			// Start async operation
			var task = _serviceLocator.GetServiceAsync<ServiceLocatorTestUtils.TestService>();

			yield return null;

			// Reject the service with the custom exception
			_serviceLocator.RejectService<ServiceLocatorTestUtils.TestService>(customException);

			// Wait for task to complete (should be faulted)
			yield return new WaitUntil(() => task.IsCompleted);

			// Verify task was faulted correctly
			Assert.IsTrue(task.IsCompleted, "Task should be completed");
			Assert.IsTrue(task.IsFaulted, "Task should be faulted");
			Assert.IsFalse(task.IsCanceled, "Task should not be canceled");

			// Verify exception is correct
			Assert.IsNotNull(task.Exception, "Task should have an exception");
			Assert.AreEqual(customException, task.Exception.InnerException,
				"Task exception should match the custom exception");
		}

		[UnityTest]
		public IEnumerator Async_WithExplicitCancellation_IsCancelled()
		{
			// Create cancellation token
			using var cts = new CancellationTokenSource();

			// Start async operation with token
			var task = _serviceLocator.GetServiceAsync<ServiceLocatorTestUtils.TestService>(cts.Token);

			yield return null;

			// Cancel the token
			cts.Cancel();

			// Wait for task to complete (should be canceled)
			yield return new WaitUntil(() => task.IsCompleted);

			// Verify task was canceled correctly
			Assert.IsTrue(task.IsCompleted, "Task should be completed");
			Assert.IsTrue(task.IsCanceled, "Task should be canceled");
			Assert.IsFalse(task.IsFaulted, "Task should not be faulted");

			// Verify exception is TaskCanceledException
			Assert.ThrowsAsync<TaskCanceledException>(async () => await task,
				"Task should throw TaskCanceledException when awaited");
		}

		[UnityTest]
		public IEnumerator Async_ConcurrentRegistrationAndAccess_DoesNotDeadlock()
		{
			var task1 = Task.Run(() => _serviceLocator.GetServiceAsync<ServiceLocatorTestUtils.TestService>());
			var task2 = Task.Run(() => _serviceLocator.GetServiceAsync<ServiceLocatorTestUtils.AnotherTestService>());

			// Wait a little for tasks to potentially start processing
			yield return new WaitForSeconds(0.1f);

			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());
			_serviceLocator.Register(new ServiceLocatorTestUtils.AnotherTestService());

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

				Assert.IsNotNull(result1, "First result should not be null");
				Assert.IsNotNull(result2, "Second result should not be null");
			}
			catch (Exception e)
			{
				Assert.Fail("Exception thrown: " + e.Message);
			}
		}

		[UnityTest]
		public IEnumerator Async_HighConcurrency_StressTest()
		{
			const int iterations = 100; // Reduced from 1000 for faster test execution
			var tasks = new Task[iterations];

			for (var i = 0; i < iterations; i++)
			{
				tasks[i] = _serviceLocator.GetServiceAsync<ServiceLocatorTestUtils.TestService>();
			}

			yield return new WaitForSeconds(0.1f);
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			yield return new WaitUntil(() => tasks.All(t => t.IsCompleted));

			foreach (var task in tasks.Cast<Task<ServiceLocatorTestUtils.TestService>>())
			{
				Assert.IsNotNull(task.Result, "Task result should not be null");
				Assert.AreEqual("Hello from TestService!", task.Result.Message,
					"Service should contain the correct data");
			}
		}

		[UnityTest]
		public IEnumerator Async_MultiService_AllResolveWhenRegistered()
		{
			// Start multi-service async operation
			var task = _serviceLocator
				.GetServicesAsync<ServiceLocatorTestUtils.TestService, ServiceLocatorTestUtils.AnotherTestService>();

			yield return null;

			// Register services in sequence
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());
			yield return null;

			// Task should not be completed yet
			Assert.IsFalse(task.IsCompleted, "Task should not be completed after first service");

			_serviceLocator.Register(new ServiceLocatorTestUtils.AnotherTestService());

			// Wait for task to complete
			yield return new WaitUntil(() => task.IsCompleted);

			// Verify task completed successfully
			Assert.IsTrue(task.IsCompleted, "Task should be completed");
			Assert.IsFalse(task.IsFaulted, "Task should not be faulted");
			Assert.IsFalse(task.IsCanceled, "Task should not be canceled");

			// Verify services were retrieved
			var (service1, service2) = task.Result;
			Assert.IsNotNull(service1, "First service should be retrieved");
			Assert.IsNotNull(service2, "Second service should be retrieved");
		}

		[UnityTest]
		public IEnumerator Async_LinkedCancellationToken_CancelsAllOperations()
		{
			// Create two cancellation tokens
			var cts1 = new CancellationTokenSource();
			var cts2 = new CancellationTokenSource();

			// Create a linked token with both sources
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, cts2.Token);

			// Start multiple service requests with the linked token
			var task = _serviceLocator
				.GetServicesAsync<ServiceLocatorTestUtils.TestService, ServiceLocatorTestUtils.AnotherTestService>(
					linkedCts.Token);

			yield return null;

			// Cancel just one of the tokens
			cts1.Cancel();

			// Wait for the task to complete
			yield return new WaitUntil(() => task.IsCompleted);

			// Assert that the task was canceled
			Assert.IsTrue(task.IsCanceled, "Task should be canceled when any of the linked tokens is canceled");
			Assert.ThrowsAsync<TaskCanceledException>(async () => await task,
				"GetServicesAsync should throw TaskCanceledException when a linked token is canceled");

			// Clean up
			cts2.Dispose();
			cts1.Dispose();
		}
	}
}
#endif