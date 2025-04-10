#if !DISABLE_SL_SCENE_TRACKING
using System;
using System.Collections;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.CoreTests
{
	/// <summary>
	///     Tests for the scene tracking functionality of the ServiceLocator.
	///     These tests directly manipulate the ServiceSceneMap instead of relying on MonoBehaviour scene detection.
	/// </summary>
	[TestFixture]
	public class ServiceLocatorSceneTrackingDirectTests
	{
		[SetUp]
		public void Setup()
		{
			_serviceLocator = ScriptableObject.CreateInstance<TestSceneTrackingServiceLocator>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_serviceLocator);
		}

		private TestSceneTrackingServiceLocator _serviceLocator;
		private const string FirstSceneName = "FirstScene";
		private const string SecondSceneName = "SecondScene";

		/// <summary>
		///     This test verifies that services are correctly associated with scene names.
		/// </summary>
		[Test]
		public void SceneSpecificServices_TrackedCorrectly()
		{
			// Create a service
			var service = new TestService();

			// Register the service
			_serviceLocator.Register(service);

			// Manually associate the service with a scene
			_serviceLocator.AssociateServiceWithScene(typeof(TestService), FirstSceneName);

			// Verify service is registered
			Assert.IsTrue(_serviceLocator.TryGetService(out TestService retrievedService));
			Assert.AreEqual(service, retrievedService);

			// Verify scene name is tracked correctly
			var sceneName = _serviceLocator.GetSceneNameForService(typeof(TestService));
			Assert.AreEqual(FirstSceneName, sceneName);
		}

		/// <summary>
		///     This test verifies that services are properly removed when a scene is unloaded.
		/// </summary>
		[Test]
		public void SceneUnload_RemovesAssociatedServices()
		{
			// Create services
			var service1 = new TestService();
			var service2 = new AnotherTestService();

			// Register services
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);

			// Associate services with different scenes
			_serviceLocator.AssociateServiceWithScene(typeof(TestService), FirstSceneName);
			_serviceLocator.AssociateServiceWithScene(typeof(AnotherTestService), SecondSceneName);

			// Verify both services are registered
			Assert.IsTrue(_serviceLocator.TryGetService(out TestService _));
			Assert.IsTrue(_serviceLocator.TryGetService(out AnotherTestService _));

			// Verify scene names
			Assert.AreEqual(FirstSceneName, _serviceLocator.GetSceneNameForService(typeof(TestService)));
			Assert.AreEqual(SecondSceneName, _serviceLocator.GetSceneNameForService(typeof(AnotherTestService)));

			// Simulate unloading the first scene
			_serviceLocator.UnregisterServicesFromScene(FirstSceneName);

			// Verify first service is gone but second service remains
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService _),
				"Service from unloaded scene should be removed");
			Assert.IsTrue(_serviceLocator.TryGetService(out AnotherTestService _),
				"Service from other scene should remain");

			// Simulate unloading the second scene
			_serviceLocator.UnregisterServicesFromScene(SecondSceneName);

			// Verify both services are gone
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService _));
			Assert.IsFalse(_serviceLocator.TryGetService(out AnotherTestService _));
		}

		/// <summary>
		///     This test verifies that non-scene services are not affected by scene unloading.
		/// </summary>
		[Test]
		public void NonSceneService_NotAffectedBySceneUnload()
		{
			// Register two services
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);

			// Only associate one with a scene
			_serviceLocator.AssociateServiceWithScene(typeof(TestService), FirstSceneName);

			// Verify both services are registered
			Assert.IsTrue(_serviceLocator.TryGetService(out TestService _));
			Assert.IsTrue(_serviceLocator.TryGetService(out AnotherTestService _));

			// Check scene names
			var sceneService1Name = _serviceLocator.GetSceneNameForService(typeof(TestService));
			var plainService2Name = _serviceLocator.GetSceneNameForService(typeof(AnotherTestService));

			Assert.AreEqual(FirstSceneName, sceneService1Name, "Scene service should have scene name");
			Assert.AreEqual("No Scene", plainService2Name, "Plain service should have 'No Scene'");

			// Simulate unloading the scene
			_serviceLocator.UnregisterServicesFromScene(FirstSceneName);

			// Verify scene service is gone but plain service remains
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService _),
				"Scene service should be unregistered");
			Assert.IsTrue(_serviceLocator.TryGetService(out AnotherTestService _),
				"Plain service should still be registered");
		}

		/// <summary>
		///     This test verifies that multiple services from the same scene are all removed on unload.
		/// </summary>
		[Test]
		public void MultipleServicesInScene_AllRemovedOnUnload()
		{
			// Create services
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();

			// Register all services
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);

			// Associate all services with the same scene
			_serviceLocator.AssociateServiceWithScene(typeof(TestService), FirstSceneName);
			_serviceLocator.AssociateServiceWithScene(typeof(AnotherTestService), FirstSceneName);
			_serviceLocator.AssociateServiceWithScene(typeof(ThirdTestService), FirstSceneName);

			// Verify all services are registered
			Assert.IsTrue(_serviceLocator.TryGetService(out TestService _));
			Assert.IsTrue(_serviceLocator.TryGetService(out AnotherTestService _));
			Assert.IsTrue(_serviceLocator.TryGetService(out ThirdTestService _));

			// Get all services count
			var allServices = _serviceLocator.GetAllServices();
			Assert.AreEqual(3, allServices.Count, "Should have 3 services registered");

			// Simulate unloading the scene
			_serviceLocator.UnregisterServicesFromScene(FirstSceneName);

			// Verify all services are gone
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService _));
			Assert.IsFalse(_serviceLocator.TryGetService(out AnotherTestService _));
			Assert.IsFalse(_serviceLocator.TryGetService(out ThirdTestService _));

			// Get all services count after unload
			allServices = _serviceLocator.GetAllServices();
			Assert.AreEqual(0, allServices.Count, "Should have 0 services registered after scene unload");
		}

		/// <summary>
		///     This test demonstrates a real MonoBehaviour in the current scene being tracked.
		/// </summary>
		[UnityTest]
		public IEnumerator MonoBehaviour_AssociatedWithCurrentScene()
		{
			// Create a GameObject with a MonoBehaviour
			var gameObject = new GameObject("SceneTrackingTestObject");
			var component = gameObject.AddComponent<SceneTrackingTestService>();

			// Register the MonoBehaviour as a service
			_serviceLocator.Register<ISceneTrackingService>(component);

			// Verify service is registered
			Assert.IsTrue(_serviceLocator.TryGetService(out ISceneTrackingService retrievedService));
			Assert.AreEqual(component, retrievedService);

			// Check scene name
			var sceneName = _serviceLocator.GetSceneNameForService(typeof(ISceneTrackingService));

			// The current scene name will be the active test scene, which we can't predict
			// But it should not be "No Scene" or empty
			Assert.IsFalse(string.IsNullOrEmpty(sceneName), "Scene name should not be empty");
			Assert.AreNotEqual("No Scene", sceneName, "MonoBehaviour should have a valid scene name");

			// Clean up
			Object.Destroy(gameObject);
			yield return null;
		}
	}

	#region Test Helpers

	// Test class that exposes protected members for testing
	public class TestSceneTrackingServiceLocator : ServiceLocator
	{
		// This method allows us to manually set the scene name for a service
		public void AssociateServiceWithScene(Type serviceType, string sceneName)
		{
			lock (Lock)
			{
				ServiceSceneMap[serviceType] = sceneName;
			}
		}
	}

	// Basic service classes
	public class TestService
	{
	}

	public class AnotherTestService
	{
	}

	public class ThirdTestService
	{
	}

	// Interface and MonoBehaviour for real scene test
	public interface ISceneTrackingService
	{
	}

	public class SceneTrackingTestService : MonoBehaviour, ISceneTrackingService
	{
	}

	#endregion
}
#endif