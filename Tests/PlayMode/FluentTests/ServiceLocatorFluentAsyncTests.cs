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

namespace Tests.PlayMode.FluentTests
{
	[TestFixture]
	public class ServiceLocatorFluentAsyncTests
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
		public IEnumerator ServiceUser_CanRetrieveService_ViaFluentAsync()
		{
			// Register the service first
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Create GameObject with async-based service user
			var gameObject = new GameObject("AsyncServiceUser");
			var serviceUser = gameObject.AddComponent<FluentServiceUserAsync>();
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
		public IEnumerator FluentAsync_WaitsForRegistration()
		{
			// Create GameObject with async-based service user
			var gameObject = new GameObject("AsyncServiceUser");
			var serviceUser = gameObject.AddComponent<FluentServiceUserAsync>();
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
		public IEnumerator FluentAsync_CancelledWhenGameObjectDestroyed()
		{
			// Create GameObject with async-based service user
			var gameObject = new GameObject("AsyncServiceUser");
			var serviceUser = gameObject.AddComponent<FluentServiceUserAsync>();
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
		public IEnumerator FluentAsync_WithExplicitCancellation_IsCancelled()
		{
			// Create cancellation token
			using var cts = new CancellationTokenSource();

			// Start async operation with token
			var task = _serviceLocator
				.GetAsync<ServiceLocatorTestUtils.TestService>()
				.WithCancellation(cts.Token);

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
		public IEnumerator FluentAsync_HighConcurrency_StressTest()
		{
			const int iterations = 100; // Reduced from 1000 for faster test execution
			var tasks = new Task[iterations];

			for (var i = 0; i < iterations; i++)
			{
				tasks[i] = _serviceLocator
					.GetAsync<ServiceLocatorTestUtils.TestService>()
					.WithCancellation();
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
		public IEnumerator FluentAsync_MultiService_AllResolveWhenRegistered()
		{
			// Start multi-service async operation using fluent API
			var task = _serviceLocator
				.GetAsync<ServiceLocatorTestUtils.TestService>()
				.AndAsync<ServiceLocatorTestUtils.AnotherTestService>()
				.WithCancellation();

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
		public IEnumerator FluentAsync_ThreeServices_AllResolveWhenRegistered()
		{
			// Start multi-service async operation using fluent API
			var task = _serviceLocator
				.GetAsync<ServiceLocatorTestUtils.TestService>()
				.AndAsync<ServiceLocatorTestUtils.AnotherTestService>()
				.AndAsync<ServiceLocatorTestUtils.ThirdTestService>()
				.WithCancellation();

			yield return null;

			// Register services in sequence
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());
			yield return null;

			// Task should not be completed yet
			Assert.IsFalse(task.IsCompleted, "Task should not be completed after first service");

			_serviceLocator.Register(new ServiceLocatorTestUtils.AnotherTestService());
			yield return null;

			// Task should not be completed yet
			Assert.IsFalse(task.IsCompleted, "Task should not be completed after second service");

			_serviceLocator.Register(new ServiceLocatorTestUtils.ThirdTestService());

			// Wait for task to complete
			yield return new WaitUntil(() => task.IsCompleted);

			// Verify task completed successfully
			Assert.IsTrue(task.IsCompleted, "Task should be completed");
			Assert.IsFalse(task.IsFaulted, "Task should not be faulted");
			Assert.IsFalse(task.IsCanceled, "Task should not be canceled");

			// Verify services were retrieved
			var (service1, service2, service3) = task.Result;
			Assert.IsNotNull(service1, "First service should be retrieved");
			Assert.IsNotNull(service2, "Second service should be retrieved");
			Assert.IsNotNull(service3, "Third service should be retrieved");
		}
	}

	/// <summary>
	///     MonoBehaviour that uses the fluent async API to retrieve services
	/// </summary>
	public class FluentServiceUserAsync : MonoBehaviour
	{
		private BaseServiceLocator _serviceLocator;
		private ServiceLocatorTestUtils.TestService _service;
		private Task<ServiceLocatorTestUtils.TestService> _serviceTask;

		// This is used for automatic cancellation when the MonoBehaviour is destroyed
		public readonly CancellationTokenSource destroyCancellationTokenSource = new();
		public CancellationToken destroyCancellationToken => destroyCancellationTokenSource.Token;

		public void Initialize(BaseServiceLocator serviceLocator)
		{
			_serviceLocator = serviceLocator;
		}

		#if !DISABLE_SL_ASYNC
		private async void Start()
		{
			try
			{
				// Use fluent API to get the service
				_serviceTask = _serviceLocator
					.GetAsync<ServiceLocatorTestUtils.TestService>()
					.WithCancellation(destroyCancellationToken);

				_service = await _serviceTask;
				Debug.Log(_service.Message);
			}
			catch (OperationCanceledException)
			{
				Debug.Log("Service retrieval was canceled due to MonoBehaviour destruction.");
			}
			catch (Exception ex)
			{
				Debug.LogError($"Failed to retrieve service: {ex.Message}");
			}
		}
		#else
        private void Start()
        {
            // Alternative implementation when async services are disabled
            if (_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService service))
            {
                _service = service;
                Debug.Log(_service.Message);
            }
            else
            {
                Debug.LogError("Failed to retrieve service via TryGetService.");
            }
        }
		#endif

		public ServiceLocatorTestUtils.TestService GetRetrievedService()
		{
			return _service;
		}

		#if !DISABLE_SL_ASYNC
		public Task<ServiceLocatorTestUtils.TestService> GetServiceTask()
		{
			return _serviceTask;
		}
		#endif

		private void OnDestroy()
		{
			destroyCancellationTokenSource.Cancel();
			destroyCancellationTokenSource.Dispose();
		}
	}
}
#endif