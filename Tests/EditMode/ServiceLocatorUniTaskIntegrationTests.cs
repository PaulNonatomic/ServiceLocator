using System.Threading;
using Nonatomic.ServiceLocator.Extensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#if !DISABLE_SL_UNITASK && ENABLE_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace Tests.EditMode
{
	[TestFixture]
	public class ServiceLocatorUniTaskIntegrationTests
	{
		[SetUp]
		public void Setup()
		{
			_serviceLocator = ScriptableObject.CreateInstance<TestServiceLocator>();
			_serviceLocator.ForceInitialize();
			LogAssert.ignoreFailingMessages = true; // Ignore Debug.LogError messages during tests
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_serviceLocator);
			LogAssert.ignoreFailingMessages = false;
		}

		private TestServiceLocator _serviceLocator;

		#if !DISABLE_SL_UNITASK && ENABLE_UNITASK
		[Test]
		public async UniTask ServiceRegisteredAfterRequest_WithErrorHandling_Success()
		{
			// Create a service but don't register it yet
			var service = new TestService();

			// Start async request
			var serviceTask = _serviceLocator.GetServiceAsync<TestService>()
				.WithErrorHandling(null);

			// Register the service after a small delay (simulating async registration)
			await UniTask.Delay(100);
			_serviceLocator.Register(service);

			// Await the task result
			var result = await serviceTask;

			// Assert
			Assert.AreEqual(service, result, "Should return the service registered after request started");
		}

		[Test]
		public async UniTask ServiceUnregisteredDuringRequest_WithErrorHandling_ReturnsDefault()
		{
			// Register a service
			var service = new TestService();
			_serviceLocator.Register(service);

			// Create a default service to return on failure
			var defaultService = new TestService();

			// Start async request with a delay
			var delayedRequest = UniTask.Delay(200)
				.ContinueWith(() => _serviceLocator.GetServiceAsync<TestService>())
				.WithErrorHandling(defaultService);

			// Unregister the service while the request is in progress
			await UniTask.Delay(100);
			_serviceLocator.Unregister<TestService>();

			// Await the result
			var result = await delayedRequest;

			// Assert
			Assert.AreEqual(defaultService, result,
				"Should return default value when service is unregistered during request");
		}

		[Test]
		public async UniTask MultipleServicesRegisteredAtDifferentTimes_WithErrorHandling()
		{
			// Register first service
			var service1 = new TestService();
			_serviceLocator.Register(service1);

			// Start the request for both services
			var servicesTask = _serviceLocator.GetServiceAsync<TestService, AnotherTestService>()
				.WithErrorHandling((null, null));

			// Register second service after a delay
			await UniTask.Delay(100);
			var service2 = new AnotherTestService();
			_serviceLocator.Register(service2);

			// Await the result
			var (result1, result2) = await servicesTask;

			// Assert
			Assert.AreEqual(service1, result1, "Should return the first service");
			Assert.AreEqual(service2, result2, "Should return the second service registered during request");
		}

		[Test]
		public async UniTask CancelledServiceRequest_WithErrorHandling_ReturnsDefaultValue()
		{
			// Create cancellation source
			using var cts = new CancellationTokenSource();

			// Start request with cancellation token and error handling
			var defaultService = new TestService();
			var serviceTask = _serviceLocator.GetServiceAsync<TestService>(cts.Token)
				.WithErrorHandling(defaultService);

			// Cancel the request
			await UniTask.Delay(50);
			cts.Cancel();

			// Await the result
			var result = await serviceTask;

			// Assert
			Assert.AreEqual(defaultService, result, "Should return default value when request is cancelled");
		}

		[Test]
		public async UniTask MultipleParallelRequests_SomeSucceed_SomeFail()
		{
			// Register only some services
			_serviceLocator.Register(new TestService());
			_serviceLocator.Register(new ThirdTestService());

			// Start multiple parallel requests with error handling
			var task1 = _serviceLocator.GetServiceAsync<TestService>();

			var task2 = _serviceLocator.GetServiceAsync<AnotherTestService>()
				.WithErrorHandling(new());

			var task3 = _serviceLocator.GetServiceAsync<ThirdTestService>();

			// Handle each task separately to avoid tuple issues
			TestService service1 = null;
			AnotherTestService service2 = null;
			ThirdTestService service3 = null;

			try
			{
				service1 = await task1;
			}
			catch
			{
				// Handle failure
			}

			// service2 already has error handling
			service2 = await task2;

			try
			{
				service3 = await task3;
			}
			catch
			{
				// Handle failure
			}

			// Assert
			Assert.IsNotNull(service1, "First service should be retrieved successfully");
			Assert.IsNotNull(service2, "Second service should fail but return default");
			Assert.IsNotNull(service3, "Third service should be retrieved successfully");
		}

		[Test]
		public async UniTask ServiceLocatorCleanup_DuringRequest_WithErrorHandling_ReturnsDefault()
		{
			// Create a default service
			var defaultService = new TestService();

			// Start request with delay
			var delayedRequest = UniTask.Delay(200)
				.ContinueWith(() => _serviceLocator.GetServiceAsync<TestService>())
				.WithErrorHandling(defaultService);

			// Clean up service locator during request
			await UniTask.Delay(100);
			_serviceLocator.Cleanup();

			// Await result
			var result = await delayedRequest;

			// Assert
			Assert.AreEqual(defaultService, result, "Should return default value when ServiceLocator is cleaned up");
		}

		[Test]
		public async UniTask MultiServiceOperations_WithSomeFailures()
		{
			// Register a service that will be retrieved successfully
			var service1 = new TestService();
			_serviceLocator.Register(service1);

			// Start a multi-service request
			var compositeTask = _serviceLocator.GetServiceAsync<TestService, AnotherTestService>();

			// Register a service during the request
			await UniTask.Delay(100);

			// Don't register service2, let it fail

			// This will throw because one service couldn't be resolved
			var exceptionThrown = false;
			try
			{
				var result = await compositeTask;
			}
			catch
			{
				exceptionThrown = true;
			}

			// Assert
			Assert.IsTrue(exceptionThrown, "Should throw exception when any service can't be resolved");
		}
		#endif
	}
}