using System.Collections;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace Tests.PlayMode
{
	public class TestService
	{
		public string Message { get; set; } = "Hello from TestService!";
	}
	
	public class ServiceUser : MonoBehaviour
	{
		private BaseServiceLocator _serviceLocator;
		private TestService _service;

		// Inject the ServiceLocator instance
		public void Initialize(BaseServiceLocator serviceLocator)
		{
			_serviceLocator = serviceLocator;
			_serviceLocator.Register(new TestService());
		}

		private void Start()
		{
			// Try to get the service using the ServiceLocator's GetService method in Start
			var servicePromise = _serviceLocator.GetService<TestService>();

			// Use the Then method with explicit type specification for TResult
			servicePromise.Then<TestService>(service =>
			{
				_service = service;
				Debug.Log(_service.Message); // Optional: Log for verification
				return service;  // Return the service itself to satisfy the TResult requirement
			}).Catch(ex =>
			{
				Debug.LogError($"Failed to retrieve service: {ex.Message}");
			});
		}

		// Method for testing purposes to retrieve the service
		public TestService GetRetrievedService()
		{
			return _service;
		}
	}
	
	[TestFixture]
	public class ServiceLocatorPlayModeTests
	{
		[UnityTest]
		public IEnumerator ServiceRegisteredInAwake_AvailableInStart()
		{
			// // Create an instance of the ServiceLocator (use ScriptableObject.CreateInstance for ScriptableObjects)
			var serviceLocator = ScriptableObject.CreateInstance<ServiceLocator>();
			//
			// // Create a new GameObject with the ServiceUser component
			var gameObject = new GameObject();
			var serviceUser = gameObject.AddComponent<ServiceUser>();
			//
			// // Initialize the ServiceUser with the created service locator instance
			serviceUser.Initialize(serviceLocator);
			//
			// // Wait for 2 frames to simulate the passage of time for Start method execution
			// yield return null; // Wait one frame for Awake
			yield return null; // Wait another frame for Start
			//
			// // Now verify that the service was successfully retrieved
			var retrievedService = serviceUser.GetRetrievedService();
			//
			// // Assert that the service is not null, meaning it was retrieved in Start
			Assert.IsNotNull(retrievedService, "Service should be retrieved in the Start method.");
			Assert.AreEqual("Hello from TestService!", retrievedService.Message, "Service should contain the correct data.");
			
			yield return null;
		}
	}
}