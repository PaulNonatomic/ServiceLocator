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

namespace Tests.PlayMode
{
	public class TestService
	{
		public string Message { get; set; } = "Hello from TestService!";
	}

	[TestFixture]
	public class ServiceLocatorPlayModeTests
	{
		private ServiceLocator _serviceLocator;

		[SetUp]
		public void Setup()
		{
			UnitySynchronizationContext.Initialize();
			_serviceLocator = ScriptableObject.CreateInstance<ServiceLocator>();
		}
		
		[UnityTest]
		public IEnumerator ServiceRegisteredInAwake_AvailableInStart()
		{
			var gameObject = new GameObject();
			var serviceUser = gameObject.AddComponent<ServiceUserImmediate>(); // Use the new variant
			serviceUser.Initialize(_serviceLocator);
	
			yield return null;
	
			var retrievedService = serviceUser.GetRetrievedService();
	
			Assert.IsNotNull(retrievedService, "Service should be retrieved in the Start method.");
			Assert.AreEqual("Hello from TestService!", retrievedService.Message, "Service should contain the correct data.");
	
			yield return null;
		}
		
		[UnityTest]
		public IEnumerator PromiseCallbackRunsOnMainThread()
		{
			var mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

			TestService retrievedService = null;
			var callbackThreadId = -1;
			var promise = _serviceLocator.GetService<TestService>();

			promise.Then(service =>
			{
				retrievedService = service;
				callbackThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
			}).Catch(ex => Assert.Fail(ex.Message));

			yield return null;

			var service = new TestService();
			_serviceLocator.Register(service);

			yield return new WaitUntil(() => retrievedService != null);

			Assert.AreEqual(mainThreadId, callbackThreadId, "Promise callback did not run on the main thread.");

			yield return null;
		}
		
		[UnityTest]
		public IEnumerator HighConcurrency_StressTest()
		{
			const int iterations = 1000;
			var tasks = new Task[iterations];

			for (var i = 0; i < iterations; i++)
			{
				tasks[i] = _serviceLocator.GetServiceAsync<TestService>();
			}

			yield return new WaitForSeconds(0.5f);
			_serviceLocator.Register(new TestService());
	
			yield return new WaitUntil(() => tasks.All(t => t.IsCompleted));
	
			foreach (var task in tasks.Cast<Task<TestService>>())
			{
				Assert.IsNotNull(task.Result);
			}
		}
		
		// Test for Requirement of a CancellationToken
		
		[UnityTest]
		public IEnumerator GetServiceAsync_Cancels_WhenMonoBehaviourDestroyedBeforeResolution()
		{
			var gameObject = new GameObject("ServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserAsync>();
			serviceUser.Initialize(_serviceLocator);

			// Wait for Start to initialize the task
			yield return null; // Give Start a frame to run

			Task<TestService> awaitingTask = serviceUser.GetServiceTask();
			yield return new WaitUntil(() => awaitingTask != null); // Ensure task is assigned

			// Act: Destroy the MonoBehaviour naturally
			Debug.Log("Destroying GameObject with Object.Destroy");
			Object.Destroy(gameObject);

			// Wait for destruction to process and cancellation to occur
			yield return new WaitForEndOfFrame();
			yield return new WaitUntil(() => awaitingTask.IsCompleted);

			// Assert
			Debug.Log($"Task status: IsCompleted={awaitingTask.IsCompleted}, IsCanceled={awaitingTask.IsCanceled}, IsFaulted={awaitingTask.IsFaulted}");
			Assert.IsTrue(awaitingTask.IsCanceled, "Task should be canceled after MonoBehaviour destruction.");
			Assert.ThrowsAsync<TaskCanceledException>(async () => await awaitingTask, "Task should throw TaskCanceledException.");
		}
		
		[UnityTest]
		public IEnumerator GetService_Promise_Cancels_WhenMonoBehaviourDestroyedBeforeResolution()
		{
			// Arrange: Create a GameObject with a MonoBehaviour
			var gameObject = new GameObject("ServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserAsync>();
			serviceUser.Initialize(_serviceLocator);

			// Variables to track promise state
			var thenCalled = false;
			var catchCalled = false;
			var promise = _serviceLocator.GetService<TestService>(serviceUser.destroyCancellationToken);
			promise.Then(service => thenCalled = true).Catch(ex => catchCalled = true);

			yield return null;

			// Act: Destroy the MonoBehaviour naturally
			Debug.Log("Destroying GameObject with Object.Destroy");
			Object.Destroy(gameObject);

			// Wait for destruction to process
			yield return new WaitForEndOfFrame();
			yield return new WaitForSeconds(0.1f);

			// Assert: Catch should be called due to cancellation, Then should not
			Assert.IsFalse(thenCalled, "Then callback should not be called after MonoBehaviour destruction.");
			Assert.IsTrue(catchCalled, "Catch callback should be called due to cancellation after MonoBehaviour destruction.");

			// Register the service (shouldn’t affect canceled promise)
			_serviceLocator.Register(new TestService());
			yield return new WaitForSeconds(0.1f);

			// Assert: Promise remains canceled, doesn’t resolve
			Assert.IsFalse(thenCalled, "Then callback should not be called after service registration.");
			Assert.IsTrue(catchCalled, "Catch callback should remain true after service registration.");
		}
		
		[UnityTest]
		public IEnumerator HighConcurrency_WithDestruction_CancelsTasks()
		{
			const int count = 10;
			var gameObjects = new GameObject[count];
			var tasks = new Task<TestService>[count];
			var serviceUsers = new ServiceUserAsync[count];

			for (var i = 0; i < count; i++)
			{
				gameObjects[i] = new GameObject($"ServiceUser_{i}");
				serviceUsers[i] = gameObjects[i].AddComponent<ServiceUserAsync>();
				serviceUsers[i].Initialize(_serviceLocator);
			}

			yield return null; // Wait for Start to initialize tasks

			for (var i = 0; i < count; i++)
			{
				tasks[i] = serviceUsers[i].GetServiceTask();
			}

			yield return new WaitUntil(() => tasks.All(t => t != null));

			// Act: Destroy half of the GameObjects naturally
			Debug.Log("Destroying half of the GameObjects with Object.Destroy");
			for (var i = 0; i < count / 2; i++)
			{
				Object.Destroy(gameObjects[i]);
			}

			// Wait for destruction to process
			yield return new WaitForEndOfFrame();
			yield return new WaitForSeconds(0.1f);

			// Register the service for surviving tasks
			_serviceLocator.Register(new TestService());
			yield return new WaitForSeconds(0.1f);

			// Assert
			for (var i = 0; i < count; i++)
			{
				Debug.Log($"Task {i} status: IsCompleted={tasks[i].IsCompleted}, IsCanceled={tasks[i].IsCanceled}, IsFaulted={tasks[i].IsFaulted}");
				if (i < count / 2) // Destroyed
				{
					Assert.IsTrue(tasks[i].IsCompleted, $"Task {i} should complete (canceled) after MonoBehaviour destruction.");
					Assert.IsTrue(tasks[i].IsCanceled, $"Task {i} should be canceled after MonoBehaviour destruction.");
					Assert.ThrowsAsync<TaskCanceledException>(async () => await tasks[i], $"Task {i} should throw TaskCanceledException.");
				}
				else // Not destroyed
				{
					Assert.IsTrue(tasks[i].IsCompleted, $"Task {i} should complete for non-destroyed MonoBehaviour.");
					Assert.IsFalse(tasks[i].IsCanceled, $"Task {i} should not be canceled.");
					Assert.IsNotNull(tasks[i].Result, $"Task {i} should return a valid service.");
				}
			}

			// Cleanup remaining GameObjects
			for (var i = count / 2; i < count; i++)
			{
				Object.Destroy(gameObjects[i]);
			}

			yield return new WaitForEndOfFrame(); // Ensure cleanup completes
		}
		
		[UnityTest]
		public IEnumerator GetServiceAsync_PropagatesCancellationException()
		{
			var cts = new CancellationTokenSource();
			var task = _serviceLocator.GetServiceAsync<TestService>(cts.Token);

			yield return null;

			cts.Cancel();

			yield return new WaitUntil(() => task.IsCompleted);

			Assert.IsTrue(task.IsCanceled, "Task should be canceled when the CancellationToken is triggered.");
			Assert.ThrowsAsync<TaskCanceledException>(async () => await task, "Task should throw TaskCanceledException when canceled.");
		}

		[UnityTest]
		public IEnumerator GetServiceAsync_RejectsWithCustomException()
		{
			var customException = new InvalidOperationException("Service initialization failed");
			var task = _serviceLocator.GetServiceAsync<TestService>();

			yield return null;

			_serviceLocator.RejectService<TestService>(customException);

			yield return new WaitUntil(() => task.IsFaulted);

			Assert.IsTrue(task.IsFaulted, "Task should be faulted after RejectService is called.");
			Assert.AreEqual(customException, task.Exception?.InnerException, "Task should propagate the custom exception from RejectService.");
		}
		
		[UnityTest]
		public IEnumerator GetService_RejectsWithCustomException()
		{
			var customException = new InvalidOperationException("Service initialization failed");
			Exception receivedException = null;
    
			_serviceLocator.GetService<TestService>().Then((result) =>
			{
			}).Catch((exception) =>
			{
				receivedException = exception;
			});

			yield return null;

			_serviceLocator.RejectService<TestService>(customException);

			yield return new WaitUntil(() => receivedException != null);

			Assert.IsTrue(receivedException != null, "Task should be faulted after RejectService is called.");
			if (receivedException is AggregateException agg)
			{
				Assert.AreEqual(customException, agg.InnerException, "Inner exception should match the custom exception from RejectService.");
			}
			else
			{
				Assert.AreEqual(customException, receivedException, "Exception should match the custom exception from RejectService.");
			}
		}
		
		// Promise Edge Cases
		
		[UnityTest]
		public IEnumerator GetService_ResolvedAfterLongDelay_StillWorks()
		{
			var promise = _serviceLocator.GetService<TestService>();
			var resolved = false;
    
			promise.Then(_ => resolved = true);
    
			// Wait for a significant time
			yield return new WaitForSeconds(5.0f);
    
			// Register service after long delay
			_serviceLocator.Register(new TestService());
    
			yield return new WaitUntil(() => resolved);
    
			Assert.IsTrue(resolved, "Promise should resolve even after long delay");
		}
	}
}