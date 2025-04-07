#if ENABLE_SL_COROUTINES || !DISABLE_SL_COROUTINES
using System.Collections;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
	[TestFixture]
	public class ServiceLocatorCoroutineTests
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
		public IEnumerator ServiceUser_CanRetrieveService_ViaCoroutine()
		{
			// Register the service first
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Create GameObject with coroutine-based service user
			var gameObject = new GameObject("CoroutineServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserCoroutine>();
			serviceUser.Initialize(_serviceLocator);

			// Give it a frame to start the coroutine
			yield return null;

			// Verify coroutine started
			Assert.IsTrue(serviceUser.CoroutineStarted, "Coroutine should have started");

			// Wait for service to be retrieved (should be immediate since already registered)
			yield return new WaitUntil(() => serviceUser.ServiceRetrieved);

			// Verify service was retrieved correctly
			Assert.IsNotNull(serviceUser.RetrievedService, "Service should be retrieved");
			Assert.AreEqual("Hello from TestService!", serviceUser.RetrievedService.Message,
				"Service should contain the correct data");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator ServiceCoroutine_WaitsForRegistration()
		{
			// Create GameObject with coroutine-based service user
			var gameObject = new GameObject("CoroutineServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserCoroutine>();
			serviceUser.Initialize(_serviceLocator);

			// Service not registered yet

			// Give it a frame to start the coroutine
			yield return null;

			// Verify coroutine started but service not yet retrieved
			Assert.IsTrue(serviceUser.CoroutineStarted, "Coroutine should have started");
			Assert.IsFalse(serviceUser.ServiceRetrieved, "Service should not be retrieved yet");

			// Register the service after a delay
			yield return new WaitForSeconds(0.2f);
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Wait for service to be retrieved
			yield return new WaitUntil(() => serviceUser.ServiceRetrieved);

			// Verify service was retrieved correctly
			Assert.IsNotNull(serviceUser.RetrievedService, "Service should be retrieved after registration");
			Assert.AreEqual("Hello from TestService!", serviceUser.RetrievedService.Message,
				"Service should contain the correct data");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator ServiceCoroutine_CancelledOnCleanup()
		{
			// Create GameObject with coroutine-based service user
			var gameObject = new GameObject("CoroutineServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserCoroutine>();
			serviceUser.Initialize(_serviceLocator);

			// Give it a frame to start the coroutine
			yield return null;

			// Verify coroutine started but service not yet retrieved
			Assert.IsTrue(serviceUser.CoroutineStarted, "Coroutine should have started");
			Assert.IsFalse(serviceUser.ServiceRetrieved, "Service should not be retrieved yet");

			// Cleanup the ServiceLocator before registering the service
			_serviceLocator.Cleanup();

			// Now register the service - this shouldn't reach the coroutine
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Wait a bit to make sure the coroutine had time to process if it was still active
			yield return new WaitForSeconds(0.2f);

			// We need to destroy the game object to ensure the coroutine doesn't continue processing
			Object.Destroy(gameObject);
			yield return null;

			// Verify service was NOT retrieved (because coroutine was cancelled)
			Assert.IsFalse(serviceUser.ServiceRetrieved, "Service should not be retrieved after cleanup");
			Assert.IsNull(serviceUser.RetrievedService, "Retrieved service should remain null after cleanup");
		}

		[UnityTest]
		public IEnumerator ServiceCoroutine_NotAffectedByGameObjectDestruction()
		{
			// Unlike Async/Promise which can use CancellationTokens, coroutines don't automatically
			// get cancelled when their GameObject is destroyed, so we test for this difference

			var serviceRetrieved = false;
			ServiceLocatorTestUtils.TestService retrievedService = null;

			// Start a coroutine manually
			var coroutine = _serviceLocator.GetServiceCoroutine<ServiceLocatorTestUtils.TestService>(service =>
			{
				retrievedService = service;
				serviceRetrieved = true;
			});

			// Process one step to start the coroutine
			coroutine.MoveNext();

			// Create and destroy a GameObject (shouldn't affect our manual coroutine)
			var gameObject = new GameObject("TemporaryObject");
			Object.Destroy(gameObject);
			yield return null;

			// Register the service after GameObject destruction
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Process the remaining coroutine steps
			while (coroutine.MoveNext())
			{
				yield return null;
			}

			// Verify service was still retrieved despite GameObject destruction
			Assert.IsTrue(serviceRetrieved, "Service should be retrieved after GameObject destruction");
			Assert.IsNotNull(retrievedService, "Retrieved service should not be null");
			Assert.AreEqual("Hello from TestService!", retrievedService.Message,
				"Service should contain the correct data");
		}

		[UnityTest]
		public IEnumerator ServiceCoroutine_RetrieveMultipleServices_SequentialRequests()
		{
			// Register first service
			var service1 = new ServiceLocatorTestUtils.TestService { Message = "First Service" };
			_serviceLocator.Register(service1);

			// Retrieve first service
			ServiceLocatorTestUtils.TestService retrievedService1 = null;
			var coroutine1 =
				_serviceLocator.GetServiceCoroutine<ServiceLocatorTestUtils.TestService>(service =>
				{
					retrievedService1 = service;
				});

			// Process first coroutine to completion
			while (coroutine1.MoveNext())
			{
				yield return null;
			}

			// Verify first service was retrieved
			Assert.IsNotNull(retrievedService1, "First service should be retrieved");
			Assert.AreEqual("First Service", retrievedService1.Message,
				"First service should contain correct data");

			// Unregister first service and register second service
			_serviceLocator.Unregister<ServiceLocatorTestUtils.TestService>();
			var service2 = new ServiceLocatorTestUtils.TestService { Message = "Second Service" };
			_serviceLocator.Register(service2);

			// Retrieve second service
			ServiceLocatorTestUtils.TestService retrievedService2 = null;
			var coroutine2 =
				_serviceLocator.GetServiceCoroutine<ServiceLocatorTestUtils.TestService>(service =>
				{
					retrievedService2 = service;
				});

			// Process second coroutine to completion
			while (coroutine2.MoveNext())
			{
				yield return null;
			}

			// Verify second service was retrieved
			Assert.IsNotNull(retrievedService2, "Second service should be retrieved");
			Assert.AreEqual("Second Service", retrievedService2.Message,
				"Second service should contain correct data");

			// Verify services are different
			Assert.AreNotEqual(retrievedService1, retrievedService2,
				"Retrieved services should be different instances");
		}
	}
}
#endif