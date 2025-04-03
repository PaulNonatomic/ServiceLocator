using System.Collections;
using System.Reflection;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
	[TestFixture]
	public class ServiceLocatorIsServiceValidTests
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
		public void IsServiceValid_WithNoService_ReturnsFalse()
		{
			// Service not registered
			var isValid = _serviceLocator.IsServiceValid<ServiceLocatorTestUtils.TestService>();

			// Should return false since no service exists
			Assert.IsFalse(isValid, "IsServiceValid should return false when service is not registered");
		}

		[Test]
		public void IsServiceValid_WithRegisteredService_ReturnsTrue()
		{
			// Register a regular C# service
			var service = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(service);

			// Check if service is valid
			var isValid = _serviceLocator.IsServiceValid<ServiceLocatorTestUtils.TestService>();

			// Should return true for valid service
			Assert.IsTrue(isValid, "IsServiceValid should return true for a registered and valid service");
		}

		[Test]
		public void IsServiceValid_AfterUnregister_ReturnsFalse()
		{
			// Register and then unregister a service
			var service = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(service);
			_serviceLocator.Unregister<ServiceLocatorTestUtils.TestService>();

			// Check if service is valid
			var isValid = _serviceLocator.IsServiceValid<ServiceLocatorTestUtils.TestService>();

			// Should return false after unregistering
			Assert.IsFalse(isValid, "IsServiceValid should return false after service is unregistered");
		}

		[UnityTest]
		public IEnumerator IsServiceValid_WithDestroyedMonoBehaviour_ReturnsFalse()
		{
			// Create GameObject with MonoBehaviour service
			var gameObject = new GameObject("MonoBehaviourService");
			var monoService = gameObject.AddComponent<MonoBehaviourTestService>();

			// Register the MonoBehaviour as a service
			_serviceLocator.Register<IMonoBehaviourTestService>(monoService);

			// Initially service should be valid
			var isValidBefore = _serviceLocator.IsServiceValid<IMonoBehaviourTestService>();
			Assert.IsTrue(isValidBefore, "Service should be valid before destruction");

			// Destroy the GameObject
			Object.Destroy(gameObject);

			// Wait a frame for Unity to process the destruction
			yield return null;

			// Check if service is valid after destruction
			var isValidAfter = _serviceLocator.IsServiceValid<IMonoBehaviourTestService>();

			// Should return false for destroyed MonoBehaviour
			Assert.IsFalse(isValidAfter, "IsServiceValid should return false for destroyed MonoBehaviour service");
		}

		[UnityTest]
		public IEnumerator IsServiceValid_WithDisabledMonoBehaviour_ReturnsTrue()
		{
			// Create GameObject with MonoBehaviour service
			var gameObject = new GameObject("MonoBehaviourService");
			var monoService = gameObject.AddComponent<MonoBehaviourTestService>();

			// Register the MonoBehaviour as a service
			_serviceLocator.Register<IMonoBehaviourTestService>(monoService);

			// Disable the GameObject (but don't destroy it)
			gameObject.SetActive(false);

			// Wait a frame
			yield return null;

			// Check if service is valid
			var isValid = _serviceLocator.IsServiceValid<IMonoBehaviourTestService>();

			// Should return true - disabled but not destroyed
			Assert.IsTrue(isValid, "IsServiceValid should return true for disabled but not destroyed MonoBehaviour");

			// Clean up
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator IsServiceValid_WithDestroyedMonoService_ReturnsFalse()
		{
			// Create GameObject with MonoService implementation
			var gameObject = new GameObject("MonoServiceObject");
			var monoService = gameObject.AddComponent<TestMonoService>();

			// Add reference to service locator for automatic registration
			monoService.SetServiceLocator(_serviceLocator);

			// Call ServiceReady to register with locator
			monoService.Initialize();

			// Check if service is valid after registration
			var isValidBefore = _serviceLocator.IsServiceValid<ITestMonoService>();
			Assert.IsTrue(isValidBefore, "Service should be valid after registration");

			// Destroy the GameObject
			Object.Destroy(gameObject);

			// Wait a frame for Unity to process the destruction
			yield return null;

			// Check if service is valid after destruction
			var isValidAfter = _serviceLocator.IsServiceValid<ITestMonoService>();

			// Should return false for destroyed MonoService
			Assert.IsFalse(isValidAfter, "IsServiceValid should return false for destroyed MonoService");
		}

		[Test]
		public void IsServiceValid_WithNullService_ReturnsFalse()
		{
			// Register null service (this would normally throw, but we're forcing it for testing)
			typeof(ServiceLocator)
				.GetMethod("Register", BindingFlags.NonPublic | BindingFlags.Instance)
				?.MakeGenericMethod(typeof(ServiceLocatorTestUtils.TestService))
				.Invoke(_serviceLocator, new object[] { null });

			// Check if service is valid
			var isValid = _serviceLocator.IsServiceValid<ServiceLocatorTestUtils.TestService>();

			// Should return false for null service
			Assert.IsFalse(isValid, "IsServiceValid should return false for null service");
		}

		[UnityTest]
		public IEnumerator IsServiceValid_WithDynamicallyDestroyedService_ReturnsFalse()
		{
			// Create a test component that contains another component
			var containerObj = new GameObject("Container");
			var container = containerObj.AddComponent<ServiceContainer>();

			// Create a child object with the actual service
			var serviceObj = new GameObject("Service");
			serviceObj.transform.SetParent(containerObj.transform);
			var childService = serviceObj.AddComponent<MonoBehaviourTestService>();

			// Setup the container with the service
			container.SetService(childService);
			container.SetServiceLocator(_serviceLocator);

			// Register the service through the container
			container.RegisterService();

			// Service should be valid initially
			var isValidBefore = _serviceLocator.IsServiceValid<IMonoBehaviourTestService>();
			Assert.IsTrue(isValidBefore, "Service should be valid after registration");

			// Destroy only the child service GameObject (not the container)
			Object.Destroy(serviceObj);

			// Wait a frame for Unity to process the destruction
			yield return null;

			// The container still exists but the service is destroyed
			// IsServiceValid should detect this
			var isValidAfter = _serviceLocator.IsServiceValid<IMonoBehaviourTestService>();

			// Should return false for destroyed service
			Assert.IsFalse(isValidAfter, "IsServiceValid should return false when child service is destroyed");

			// Clean up
			Object.Destroy(containerObj);
			yield return null;
		}
	}

	// Test interfaces and implementations

	public interface IMonoBehaviourTestService
	{
		string GetStatus();
	}

	public class MonoBehaviourTestService : MonoBehaviour, IMonoBehaviourTestService
	{
		public string GetStatus()
		{
			return "Active";
		}
	}

	public interface ITestMonoService
	{
		string GetValue();
	}

	public class TestMonoService : MonoService<ITestMonoService>, ITestMonoService
	{
		public string GetValue()
		{
			return "MonoService Value";
		}

		public void SetServiceLocator(ServiceLocator serviceLocator)
		{
			ServiceLocator = serviceLocator;
		}

		public void Initialize()
		{
			ServiceReady();
		}
	}

	public class ServiceContainer : MonoBehaviour
	{
		private IMonoBehaviourTestService _service;
		private ServiceLocator _serviceLocator;

		public void SetService(IMonoBehaviourTestService service)
		{
			_service = service;
		}

		public void SetServiceLocator(ServiceLocator serviceLocator)
		{
			_serviceLocator = serviceLocator;
		}

		public void RegisterService()
		{
			if (_service != null && _serviceLocator != null)
			{
				_serviceLocator.Register(_service);
			}
		}
	}
}