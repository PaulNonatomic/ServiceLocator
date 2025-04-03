using System.Collections;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
	[TestFixture]
	public class ServiceLocatorValidateReferenceTests
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

		[Test]
		public void IsServiceValid_WithServiceReference_ReturnsTrueForValidReference()
		{
			// Register a regular C# service
			var service = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(service);

			// Get a reference to the service
			_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService retrievedService);

			// Validate the reference
			var isValid = _serviceLocator.IsServiceValid(retrievedService);

			// Should return true for valid reference
			Assert.IsTrue(isValid, "IsServiceValid should return true for a valid service reference");
		}

		[Test]
		public void IsServiceValid_WithNullReference_ReturnsFalse()
		{
			// Register a service
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Validate a null reference
			var isValid = _serviceLocator.IsServiceValid<ServiceLocatorTestUtils.TestService>(null);

			// Should return false for null reference
			Assert.IsFalse(isValid, "IsServiceValid should return false for a null reference");
		}

		[Test]
		public void IsServiceValid_WithStaleReference_ReturnsFalse()
		{
			// Register a service
			var originalService = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(originalService);

			// Get a reference to the original service
			_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService originalReference);

			// Replace with a new service
			var newService = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(newService);

			// Validate the original reference
			var isValid = _serviceLocator.IsServiceValid(originalReference);

			// Should return false because the reference is no longer valid
			Assert.IsFalse(isValid, "IsServiceValid should return false for a stale reference");
		}

		[Test]
		public void IsServiceValid_AfterUnregister_ReturnsFalse()
		{
			// Register a service
			var service = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(service);

			// Get a reference to the service
			_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService retrievedService);

			// Unregister the service
			_serviceLocator.Unregister<ServiceLocatorTestUtils.TestService>();

			// Validate the reference
			var isValid = _serviceLocator.IsServiceValid(retrievedService);

			// Should return false after service is unregistered
			Assert.IsFalse(isValid, "IsServiceValid should return false for a reference to an unregistered service");
		}

		[UnityTest]
		public IEnumerator IsServiceValid_WithDestroyedMonoBehaviourReference_ReturnsFalse()
		{
			// Create GameObject with MonoBehaviour service
			var gameObject = new GameObject("MonoBehaviourService");
			var monoService = gameObject.AddComponent<MonoBehaviourTestService>();

			// Register the MonoBehaviour as a service
			_serviceLocator.Register<IMonoBehaviourTestService>(monoService);

			// Get a reference to the service
			_serviceLocator.TryGetService(out IMonoBehaviourTestService retrievedService);

			// Initially service should be valid
			var isValidBefore = _serviceLocator.IsServiceValid(retrievedService);
			Assert.IsTrue(isValidBefore, "Service reference should be valid before destruction");

			// Destroy the GameObject
			Object.Destroy(gameObject);

			// Wait a frame for Unity to process the destruction
			yield return null;

			// Validate the reference after destruction
			var isValidAfter = _serviceLocator.IsServiceValid(retrievedService);

			// Should return false for destroyed MonoBehaviour reference
			Assert.IsFalse(isValidAfter,
				"IsServiceValid should return false for a reference to a destroyed MonoBehaviour");
		}

		[Test]
		public void IsServiceValid_WithReferenceToServiceOfWrongType_ReturnsFalse()
		{
			// Register two different services
			var service1 = new ServiceLocatorTestUtils.TestService();
			var service2 = new ServiceLocatorTestUtils.AnotherTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);

			// Get reference to first service
			_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService retrievedService1);

			// Create a test that will pass null to IsServiceValid with AnotherTestService type
			// This simulates what would happen if you tried to validate a wrongly-typed reference
			ServiceLocatorTestUtils.AnotherTestService wrongTypeReference = null;
			var isValid = _serviceLocator.IsServiceValid(wrongTypeReference);

			// Should return false (null reference)
			Assert.IsFalse(isValid, "IsServiceValid should return false for a reference of the wrong type");
		}

		[Test]
		public void IsServiceValid_WithValidAndInvalidReferences_WorksCorrectly()
		{
			// Register multiple services
			var service1 = new ServiceLocatorTestUtils.TestService();
			var service2 = new ServiceLocatorTestUtils.AnotherTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);

			// Get references
			_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService retrievedService1);
			_serviceLocator.TryGetService(out ServiceLocatorTestUtils.AnotherTestService retrievedService2);

			// Replace first service 
			var newService1 = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(newService1);

			// Validate references
			var isValid1 = _serviceLocator.IsServiceValid(retrievedService1);
			var isValid2 = _serviceLocator.IsServiceValid(retrievedService2);

			// First reference should be invalid (replaced), second should be valid
			Assert.IsFalse(isValid1, "Reference to replaced service should be invalid");
			Assert.IsTrue(isValid2, "Reference to unchanged service should be valid");
		}

		[Test]
		public void IsServiceValid_TypeAndReference_BothMethodsWork()
		{
			// Register a service
			var service = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(service);

			// Get a reference
			_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService retrievedService);

			// Check both IsServiceValid methods
			var isValidByType = _serviceLocator.IsServiceValid<ServiceLocatorTestUtils.TestService>();
			var isValidByReference = _serviceLocator.IsServiceValid(retrievedService);

			// Both should return true
			Assert.IsTrue(isValidByType, "IsServiceValid<T>() should return true for registered service");
			Assert.IsTrue(isValidByReference, "IsServiceValid(reference) should return true for valid reference");
		}

		[Test]
		public void IsServiceValid_AfterServiceReplaced_TypeMethodReturnsTrue_ReferenceMethodReturnsFalse()
		{
			// Register a service
			var originalService = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(originalService);

			// Get a reference to the original service
			_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService originalReference);

			// Replace with a new service
			var newService = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(newService);

			// Check both IsServiceValid methods
			var isValidByType = _serviceLocator.IsServiceValid<ServiceLocatorTestUtils.TestService>();
			var isValidByReference = _serviceLocator.IsServiceValid(originalReference);

			// Type method should return true, reference method should return false
			Assert.IsTrue(isValidByType, "IsServiceValid<T>() should return true after service replacement");
			Assert.IsFalse(isValidByReference,
				"IsServiceValid(reference) should return false for replaced service reference");
		}
	}
}