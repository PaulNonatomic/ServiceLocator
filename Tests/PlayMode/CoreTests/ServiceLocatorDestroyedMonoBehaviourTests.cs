using System.Collections;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.CoreTests
{
	[TestFixture]
	public class ServiceLocatorDestroyedMonoBehaviourTests
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
		public IEnumerator Register_WithDestroyedMonoBehaviour_ReturnsFalse()
		{
			// Create GameObject with MonoBehaviour service
			var gameObject = new GameObject("DestroyedMonoBehaviourTest");
			var monoService = gameObject.AddComponent<MonoBehaviourTestService>();

			// Destroy the GameObject
			Object.Destroy(gameObject);

			// Wait a frame for Unity to process the destruction
			yield return null;

			// Try to register the destroyed MonoBehaviour
			var registrationResult = _serviceLocator.Register<IMonoBehaviourTestService>(monoService);

			// The registration should fail
			Assert.IsFalse(registrationResult, "Registration should fail with destroyed MonoBehaviour");

			// Verify service is not registered
			Assert.IsFalse(_serviceLocator.TryGetService(out IMonoBehaviourTestService retrievedService),
				"Destroyed MonoBehaviour service should not be registered");
		}

		[UnityTest]
		public IEnumerator Register_WithBeingDestroyedMonoBehaviour_ReturnsFalse()
		{
			// Arrange
			var gameObject = new GameObject("BeingDestroyedMonoBehaviourTest");
			var monoService = gameObject.AddComponent<MonoBehaviourTestService>(); // Assuming MonoBehaviourTestService implements IMonoBehaviourTestService

			// Act
			Object.Destroy(gameObject);

			// *** WAIT FOR UNITY TO PROCESS DESTRUCTION ***
			// Yield until the end of the frame, when Destroy operations are typically processed.
			yield return new WaitForEndOfFrame();

			// Now, attempt registration AFTER the object state should reflect destruction
			var registrationResult = _serviceLocator.Register<IMonoBehaviourTestService>(monoService);

			// Assert (Phase 1 - Registration Result)
			// The registration should now fail because the object is recognized as destroyed
			Assert.IsFalse(registrationResult, "Registration should fail with MonoBehaviour being destroyed");

			// Optional: Wait another frame just to be absolutely sure, though WaitForEndOfFrame should suffice
			// yield return null;

			// Assert (Phase 2 - Service Not Registered)
			// Verify service is definitely not in the locator
			Assert.IsFalse(_serviceLocator.TryGetService(out IMonoBehaviourTestService retrievedService),
				"Being destroyed MonoBehaviour service should not be registered");

			// Cleanup (If GameObject wasn't destroyed properly by the test, ensure it here, though Destroy should handle it)
			// if (gameObject != null) Object.DestroyImmediate(gameObject); // Use DestroyImmediate in tests if needed for immediate cleanup after yield
		}

		// Dummy interface and implementation for the test example:
		public interface IMonoBehaviourTestService { }
		public class MonoBehaviourTestService : MonoBehaviour, IMonoBehaviourTestService { }
		
		[UnityTest]
		public IEnumerator Register_WithDestroyedGameObject_ReturnsFalse()
		{
			// Create GameObject with MonoBehaviour service
			var gameObject = new GameObject("DestroyedGameObjectTest");
			var monoService = gameObject.AddComponent<MonoBehaviourTestService>();

			// Destroy only the GameObject, not directly the component
			Object.Destroy(gameObject);

			// Wait a frame for Unity to process the destruction
			yield return null;

			// Try to register the component from the destroyed GameObject
			var registrationResult = _serviceLocator.Register<IMonoBehaviourTestService>(monoService);

			// The registration should fail
			Assert.IsFalse(registrationResult, "Registration should fail with component from destroyed GameObject");

			// Verify service is not registered
			Assert.IsFalse(_serviceLocator.TryGetService(out IMonoBehaviourTestService retrievedService),
				"Component from destroyed GameObject should not be registered");
		}

		[UnityTest]
		public IEnumerator Register_WithValidMonoBehaviour_ReturnsTrue()
		{
			// Create GameObject with MonoBehaviour service
			var gameObject = new GameObject("ValidMonoBehaviourTest");
			var monoService = gameObject.AddComponent<MonoBehaviourTestService>();

			// Try to register the valid MonoBehaviour
			var registrationResult = _serviceLocator.Register<IMonoBehaviourTestService>(monoService);

			// The registration should succeed
			Assert.IsTrue(registrationResult, "Registration should succeed with valid MonoBehaviour");

			// Verify service is registered
			Assert.IsTrue(_serviceLocator.TryGetService(out IMonoBehaviourTestService retrievedService),
				"Valid MonoBehaviour service should be registered");

			// Verify it's the same instance
			Assert.AreEqual(monoService, retrievedService, "Retrieved service should be the same instance");

			// Clean up
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator MonoService_ServiceReady_FailsWhenDestroyed()
		{
			// Create GameObject with MonoService
			var gameObject = new GameObject("MonoServiceTest");
			var monoService = gameObject.AddComponent<TestMonoService>();

			// Initialize the MonoService
			monoService.SetServiceLocator(_serviceLocator);

			// Destroy the GameObject
			Object.Destroy(gameObject);

			// Wait a frame for Unity to process the destruction
			yield return null;

			// Try to register through ServiceReady
			var serviceReadyResult = monoService.PublicServiceReady();

			// The ServiceReady call should fail
			Assert.IsFalse(serviceReadyResult, "ServiceReady should fail when MonoService is destroyed");

			// Verify service is not registered
			Assert.IsFalse(_serviceLocator.TryGetService(out IMonoBehaviourTestService retrievedService),
				"Destroyed MonoService should not be registered");
		}
	}

	// Interface for the test service
	public interface IMonoBehaviourTestService
	{
	}

	// MonoBehaviour that implements the interface
	public class MonoBehaviourTestService : MonoBehaviour, IMonoBehaviourTestService
	{
	}

	// TestMonoService that exposes the ServiceReady method publicly for testing
	public class TestMonoService : MonoService<IMonoBehaviourTestService>, IMonoBehaviourTestService
	{
		public void SetServiceLocator(ServiceLocator serviceLocator)
		{
			ServiceLocator = serviceLocator;
		}

		public bool PublicServiceReady()
		{
			return ServiceReady();
		}
	}
}