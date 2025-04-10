using System.Collections;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.FluentTests
{
	/// <summary>
	///     Tests that verify the integration between different fluent API approaches
	///     and special cases that involve multiple retrieval patterns.
	/// </summary>
	[TestFixture]
	public class ServiceLocatorFluentMultiTest
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

		#if !DISABLE_SL_ASYNC
		[UnityTest]
		public IEnumerator Fluent_IsServiceValid_WorksWithFluentAsyncReferences()
		{
			// Register a service
			var service = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(service);

			// Get reference with fluent API
			var task = _serviceLocator
				.GetAsync<ServiceLocatorTestUtils.TestService>()
				.WithCancellation();

			// Wait for task to complete
			yield return new WaitUntil(() => task.IsCompleted);
			var retrievedService = task.Result;

			// Validate the reference
			var isValidByReference = _serviceLocator.IsServiceValid(retrievedService);
			Assert.IsTrue(isValidByReference, "IsServiceValid should return true for reference from fluent API");

			// Replace the service
			var newService = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(newService);

			// Reference should now be invalid
			isValidByReference = _serviceLocator.IsServiceValid(retrievedService);
			Assert.IsFalse(isValidByReference,
				"IsServiceValid should return false for stale reference from fluent API");
		}
		#endif

		#if !DISABLE_SL_PROMISES && !DISABLE_SL_ASYNC
		[UnityTest]
		public IEnumerator Fluent_PromiseAndAsyncIntegration_BothResolveCorrectly()
		{
			// Track results
			ServiceLocatorTestUtils.TestService asyncResult = null;
			ServiceLocatorTestUtils.TestService promiseResult = null;
			var promiseResolved = false;

			// Start concurrent operations with different APIs
			var asyncTask = _serviceLocator
				.GetAsync<ServiceLocatorTestUtils.TestService>()
				.WithCancellation();

			_serviceLocator
				.Get<ServiceLocatorTestUtils.TestService>()
				.WithCancellation()
				.Then(service =>
				{
					promiseResult = service;
					promiseResolved = true;
					return true;
				});

			// Give it a frame to start both operations
			yield return null;

			// Register the service
			var service = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(service);

			// Wait for both to complete
			yield return new WaitUntil(() => asyncTask.IsCompleted && promiseResolved);

			// Get the async result
			asyncResult = asyncTask.Result;

			// Verify both operations received the same service
			Assert.IsNotNull(asyncResult, "Async operation should have a result");
			Assert.IsNotNull(promiseResult, "Promise operation should have a result");
			Assert.AreEqual(asyncResult, promiseResult, "Both operations should receive the same service instance");
			Assert.AreEqual(service, asyncResult, "Async result should match the registered service");
			Assert.AreEqual(service, promiseResult, "Promise result should match the registered service");
		}
		#endif

		#if !DISABLE_SL_COROUTINES && !DISABLE_SL_ASYNC
		[UnityTest]
		public IEnumerator Fluent_CoroutineAndAsyncIntegration_BothResolveCorrectly()
		{
			// Track results
			ServiceLocatorTestUtils.TestService asyncResult = null;
			ServiceLocatorTestUtils.TestService coroutineResult = null;
			var coroutineResolved = false;

			// Start async operation
			var asyncTask = _serviceLocator
				.GetAsync<ServiceLocatorTestUtils.TestService>()
				.WithCancellation();

			// Create GameObject for coroutine
			var gameObject = new GameObject("CoroutineHelper");
			var monoHelper = gameObject.AddComponent<MonoBehaviourHelper>();

			// Start coroutine operation
			monoHelper.StartCoroutine(
				_serviceLocator
					.GetCoroutine<ServiceLocatorTestUtils.TestService>()
					.WithCallback(service =>
					{
						coroutineResult = service;
						coroutineResolved = true;
					})
			);

			// Give it a frame to start both operations
			yield return null;

			// Register the service
			var service = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(service);

			// Wait for both to complete
			yield return new WaitUntil(() => asyncTask.IsCompleted && coroutineResolved);

			// Get the async result
			asyncResult = asyncTask.Result;

			// Verify both operations received the same service
			Assert.IsNotNull(asyncResult, "Async operation should have a result");
			Assert.IsNotNull(coroutineResult, "Coroutine operation should have a result");
			Assert.AreEqual(asyncResult, coroutineResult, "Both operations should receive the same service instance");
			Assert.AreEqual(service, asyncResult, "Async result should match the registered service");
			Assert.AreEqual(service, coroutineResult, "Coroutine result should match the registered service");

			// Cleanup
			Object.DestroyImmediate(gameObject);
			yield return null;
		}
		#endif

		#if !DISABLE_SL_COROUTINES && !DISABLE_SL_PROMISES
		[UnityTest]
		public IEnumerator Fluent_CoroutineAndPromiseIntegration_BothResolveCorrectly()
		{
			// Track results
			ServiceLocatorTestUtils.TestService promiseResult = null;
			ServiceLocatorTestUtils.TestService coroutineResult = null;
			var promiseResolved = false;
			var coroutineResolved = false;

			// Start promise operation
			_serviceLocator
				.Get<ServiceLocatorTestUtils.TestService>()
				.WithCancellation()
				.Then(service =>
				{
					promiseResult = service;
					promiseResolved = true;
					return true;
				});

			// Create GameObject for coroutine
			var gameObject = new GameObject("CoroutineHelper");
			var monoHelper = gameObject.AddComponent<MonoBehaviourHelper>();

			// Start coroutine operation
			monoHelper.StartCoroutine(
				_serviceLocator
					.GetCoroutine<ServiceLocatorTestUtils.TestService>()
					.WithCallback(service =>
					{
						coroutineResult = service;
						coroutineResolved = true;
					})
			);

			// Give it a frame to start both operations
			yield return null;

			// Register the service
			var service = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(service);

			// Wait for both to complete
			yield return new WaitUntil(() => promiseResolved && coroutineResolved);

			// Verify both operations received the same service
			Assert.IsNotNull(promiseResult, "Promise operation should have a result");
			Assert.IsNotNull(coroutineResult, "Coroutine operation should have a result");
			Assert.AreEqual(promiseResult, coroutineResult, "Both operations should receive the same service instance");
			Assert.AreEqual(service, promiseResult, "Promise result should match the registered service");
			Assert.AreEqual(service, coroutineResult, "Coroutine result should match the registered service");

			// Cleanup
			Object.DestroyImmediate(gameObject);
			yield return null;
		}
		#endif

		#if !DISABLE_SL_ASYNC && !DISABLE_SL_PROMISES && !DISABLE_SL_COROUTINES
		[UnityTest]
		public IEnumerator Fluent_AllThreeApproaches_ResolveCorrectly()
		{
			// Track results
			ServiceLocatorTestUtils.TestService asyncResult = null;
			ServiceLocatorTestUtils.TestService promiseResult = null;
			ServiceLocatorTestUtils.TestService coroutineResult = null;
			var promiseResolved = false;
			var coroutineResolved = false;

			// Start async operation
			var asyncTask = _serviceLocator
				.GetAsync<ServiceLocatorTestUtils.TestService>()
				.WithCancellation();

			// Start promise operation
			_serviceLocator
				.Get<ServiceLocatorTestUtils.TestService>()
				.WithCancellation()
				.Then(service =>
				{
					promiseResult = service;
					promiseResolved = true;
					return true;
				});

			// Create GameObject for coroutine
			var gameObject = new GameObject("CoroutineHelper");
			var monoHelper = gameObject.AddComponent<MonoBehaviourHelper>();

			// Start coroutine operation
			monoHelper.StartCoroutine(
				_serviceLocator
					.GetCoroutine<ServiceLocatorTestUtils.TestService>()
					.WithCallback(service =>
					{
						coroutineResult = service;
						coroutineResolved = true;
					})
			);

			// Give it a frame to start all operations
			yield return null;

			// Register the service
			var service = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(service);

			// Wait for all to complete
			yield return new WaitUntil(() => asyncTask.IsCompleted && promiseResolved && coroutineResolved);

			// Get the async result
			asyncResult = asyncTask.Result;

			// Verify all operations received the same service
			Assert.IsNotNull(asyncResult, "Async operation should have a result");
			Assert.IsNotNull(promiseResult, "Promise operation should have a result");
			Assert.IsNotNull(coroutineResult, "Coroutine operation should have a result");

			Assert.AreEqual(asyncResult, promiseResult, "Async and Promise should receive the same service instance");
			Assert.AreEqual(asyncResult, coroutineResult,
				"Async and Coroutine should receive the same service instance");
			Assert.AreEqual(promiseResult, coroutineResult,
				"Promise and Coroutine should receive the same service instance");

			Assert.AreEqual(service, asyncResult, "Async result should match the registered service");
			Assert.AreEqual(service, promiseResult, "Promise result should match the registered service");
			Assert.AreEqual(service, coroutineResult, "Coroutine result should match the registered service");

			// Cleanup
			Object.DestroyImmediate(gameObject);
			yield return null;
		}
		#endif

		#if !DISABLE_SL_SCENE_TRACKING && !DISABLE_SL_ASYNC
		[UnityTest]
		public IEnumerator Fluent_SceneTracking_WorksWithFluentAPI()
		{
			// Create MonoBehaviour service
			var gameObject = new GameObject("SceneService");
			var monoBehaviourService = gameObject.AddComponent<MonoBehaviourTestService>();

			// Register the service
			_serviceLocator.Register<IMonoBehaviourTestService>(monoBehaviourService);

			// Get the service using fluent API
			var task = _serviceLocator
				.GetAsync<IMonoBehaviourTestService>()
				.WithCancellation();

			// Wait for task to complete
			yield return new WaitUntil(() => task.IsCompleted);
			var retrievedService = task.Result;

			// Verify service was retrieved correctly
			Assert.IsNotNull(retrievedService, "Service should be retrieved using fluent API");
			Assert.AreEqual(monoBehaviourService, retrievedService,
				"Retrieved service should match registered service");

			// Verify scene name is tracked correctly
			var sceneName = _serviceLocator.GetSceneNameForService(typeof(IMonoBehaviourTestService));
			Assert.IsFalse(string.IsNullOrEmpty(sceneName), "Scene name should not be empty");
			Assert.AreNotEqual("No Scene", sceneName, "Scene name should not be 'No Scene'");

			// Cleanup
			Object.DestroyImmediate(gameObject);
			yield return null;
		}
		#endif
	}
}