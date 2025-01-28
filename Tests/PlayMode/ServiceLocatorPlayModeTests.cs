using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
			var serviceUser = gameObject.AddComponent<ServiceUser>();
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
	}
}