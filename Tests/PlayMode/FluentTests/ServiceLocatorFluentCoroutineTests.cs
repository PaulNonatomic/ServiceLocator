#if !DISABLE_SL_COROUTINES
using System.Collections;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.FluentTests
{
	[TestFixture]
	public class ServiceLocatorFluentCoroutineTests
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
		public IEnumerator ServiceUser_CanRetrieveService_ViaFluentCoroutine()
		{
			// Register the service first
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Create GameObject with coroutine-based service user
			var gameObject = new GameObject("CoroutineServiceUser");
			var serviceUser = gameObject.AddComponent<FluentServiceUserCoroutine>();
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
			Object.DestroyImmediate(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator FluentCoroutine_WaitsForRegistration()
		{
			// Create GameObject with coroutine-based service user
			var gameObject = new GameObject("CoroutineServiceUser");
			var serviceUser = gameObject.AddComponent<FluentServiceUserCoroutine>();
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
			Object.DestroyImmediate(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator FluentCoroutine_CancelledOnCleanup()
		{
			// Create GameObject with coroutine-based service user
			var gameObject = new GameObject("CoroutineServiceUser");
			var serviceUser = gameObject.AddComponent<FluentServiceUserCoroutine>();
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
			Object.DestroyImmediate(gameObject);
			yield return null;

			// Verify service was NOT retrieved (because coroutine was cancelled)
			Assert.IsFalse(serviceUser.ServiceRetrieved, "Service should not be retrieved after cleanup");
			Assert.IsNull(serviceUser.RetrievedService, "Retrieved service should remain null after cleanup");
		}

		[UnityTest]
		public IEnumerator FluentCoroutine_NotAffectedByGameObjectDestruction()
		{
			// Unlike Async/Promise which can use CancellationTokens, coroutines don't automatically
			// get cancelled when their GameObject is destroyed, so we test for this difference

			var serviceRetrieved = false;
			ServiceLocatorTestUtils.TestService retrievedService = null;

			// Start a coroutine manually using fluent API
			var coroutine = _serviceLocator
				.GetCoroutine<ServiceLocatorTestUtils.TestService>()
				.WithCallback(service =>
				{
					retrievedService = service;
					serviceRetrieved = true;
				});

			// Process one step to start the coroutine
			coroutine.MoveNext();

			// Create and destroy a GameObject (shouldn't affect our manual coroutine)
			var gameObject = new GameObject("TemporaryObject");
			Object.DestroyImmediate(gameObject);
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
		public IEnumerator FluentCoroutine_MultipleServices_AllResolveWhenRegistered()
		{
			// Create GameObject to run our coroutine
			var gameObject = new GameObject("MultiServiceTester");

			// Register services before starting coroutine
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());
			_serviceLocator.Register(new ServiceLocatorTestUtils.AnotherTestService());

			// Setup variables to track service resolution
			ServiceLocatorTestUtils.TestService service1 = null;
			ServiceLocatorTestUtils.AnotherTestService service2 = null;
			var allServicesRetrieved = false;

			// Create MonoBehaviour helper
			var monoHelper = gameObject.AddComponent<MonoBehaviourHelper>();

			// Start coroutine using fluent API
			monoHelper.StartCoroutine(
				GetServicesRoutine()
			);

			// Wait for the callback to be called
			yield return new WaitUntil(() => allServicesRetrieved);

			// Verify both services were retrieved
			Assert.IsNotNull(service1, "First service should be retrieved");
			Assert.IsNotNull(service2, "Second service should be retrieved");

			// Cleanup
			Object.DestroyImmediate(gameObject);
			yield return null;

			IEnumerator GetServicesRoutine()
			{
				yield return _serviceLocator
					.GetCoroutine<ServiceLocatorTestUtils.TestService>()
					.And<ServiceLocatorTestUtils.AnotherTestService>()
					.WithCallback((s1, s2) =>
					{
						service1 = s1;
						service2 = s2;
						allServicesRetrieved = true;
					});
			}
		}

		[UnityTest]
		public IEnumerator FluentCoroutine_ThreeServices_AllResolveWhenRegistered()
		{
			// Create GameObject to run our coroutine
			var gameObject = new GameObject("MultiServiceTester");

			// Register services before starting coroutine
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());
			_serviceLocator.Register(new ServiceLocatorTestUtils.AnotherTestService());
			_serviceLocator.Register(new ServiceLocatorTestUtils.ThirdTestService());

			// Setup variables to track service resolution
			ServiceLocatorTestUtils.TestService service1 = null;
			ServiceLocatorTestUtils.AnotherTestService service2 = null;
			ServiceLocatorTestUtils.ThirdTestService service3 = null;
			var allServicesRetrieved = false;

			// Create MonoBehaviour helper
			var monoHelper = gameObject.AddComponent<MonoBehaviourHelper>();

			// Start coroutine using fluent API
			monoHelper.StartCoroutine(
				GetServicesRoutine()
			);

			// Wait for the callback to be called
			yield return new WaitUntil(() => allServicesRetrieved);

			// Verify all services were retrieved
			Assert.IsNotNull(service1, "First service should be retrieved");
			Assert.IsNotNull(service2, "Second service should be retrieved");
			Assert.IsNotNull(service3, "Third service should be retrieved");

			// Cleanup
			Object.DestroyImmediate(gameObject);
			yield return null;

			IEnumerator GetServicesRoutine()
			{
				yield return _serviceLocator
					.GetCoroutine<ServiceLocatorTestUtils.TestService>()
					.And<ServiceLocatorTestUtils.AnotherTestService>()
					.And<ServiceLocatorTestUtils.ThirdTestService>()
					.WithCallback((s1, s2, s3) =>
					{
						service1 = s1;
						service2 = s2;
						service3 = s3;
						allServicesRetrieved = true;
					});
			}
		}
	}

	/// <summary>
	///     MonoBehaviour that uses the fluent coroutine API to retrieve services
	/// </summary>
	public class FluentServiceUserCoroutine : MonoBehaviour
	{
		private BaseServiceLocator _serviceLocator;
		private Coroutine _serviceCoroutine;

		// Flag to track if the ServiceLocator has been cleaned up
		private bool _serviceLocatorCleaned;

		// Flags for test verification
		public bool ServiceRetrieved { get; private set; }
		public bool CoroutineStarted { get; private set; }
		public ServiceLocatorTestUtils.TestService RetrievedService { get; private set; }

		public void Initialize(BaseServiceLocator serviceLocator)
		{
			_serviceLocator = serviceLocator;

			// Subscribe to the OnChange event to detect cleanup
			if (_serviceLocator != null)
			{
				_serviceLocator.OnChange += CheckServiceLocatorState;
			}
		}

		private void CheckServiceLocatorState()
		{
			// Check if the ServiceLocator has been cleaned up
			if (_serviceLocator != null && _serviceLocator.GetAllServices().Count == 0)
			{
				_serviceLocatorCleaned = true;
				StopServiceCoroutine();
			}
		}

		#if !DISABLE_SL_COROUTINES
		private void Start()
		{
			// Start the coroutine to get the service using fluent API
			_serviceCoroutine = StartCoroutine(GetServiceRoutine());
		}

		private IEnumerator GetServiceRoutine()
		{
			CoroutineStarted = true;

			// Use the ServiceLocator fluent coroutine API
			yield return _serviceLocator
				.GetCoroutine<ServiceLocatorTestUtils.TestService>()
				.WithCallback(service =>
				{
					// Check if ServiceLocator has been cleaned up
					if (!_serviceLocatorCleaned)
					{
						RetrievedService = service;
						ServiceRetrieved = service != null;

						if (RetrievedService != null)
						{
							Debug.Log($"Service retrieved via coroutine: {RetrievedService.Message}");
						}
						else
						{
							Debug.LogWarning("Service retrieval via coroutine returned null");
						}
					}
					else
					{
						Debug.LogWarning("Service retrieved after ServiceLocator cleanup, ignoring result");
					}
				});
		}

		// Method to forcibly stop the service coroutine
		public void StopServiceCoroutine()
		{
			if (_serviceCoroutine != null)
			{
				StopCoroutine(_serviceCoroutine);
				_serviceCoroutine = null;
			}
		}

		private void OnDestroy()
		{
			// Unsubscribe from events
			if (_serviceLocator != null)
			{
				_serviceLocator.OnChange -= CheckServiceLocatorState;
			}

			StopServiceCoroutine();
		}
		#else
        private void Start()
        {
            // Fallback when coroutines are disabled
            if (_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService service))
            {
                RetrievedService = service;
                ServiceRetrieved = true;
                Debug.Log($"Service retrieved directly: {RetrievedService.Message}");
            }
            else
            {
                ServiceRetrieved = false;
                Debug.LogWarning("Service retrieval via TryGetService failed");
            }
        }
		#endif

		// Method for testing purposes to retrieve the service
		public ServiceLocatorTestUtils.TestService GetRetrievedService()
		{
			return RetrievedService;
		}
	}

	// Helper MonoBehaviour for tests that need to start coroutines
	public class MonoBehaviourHelper : MonoBehaviour
	{
	}
}
#endif