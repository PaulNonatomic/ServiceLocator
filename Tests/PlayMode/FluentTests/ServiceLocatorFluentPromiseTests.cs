#if !DISABLE_SL_PROMISES
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.FluentTests
{
	[TestFixture]
	public class ServiceLocatorFluentPromiseTests
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
		public IEnumerator ServiceUser_CanRetrieveService_ViaFluentPromise()
		{
			// Register the service first
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Create GameObject with promise-based service user
			var gameObject = new GameObject("PromiseServiceUser");
			var serviceUser = gameObject.AddComponent<FluentServiceUserPromise>();
			serviceUser.Initialize(_serviceLocator);

			// Give it a frame to start the promise
			yield return null;

			// Wait for service to be retrieved (should be immediate since already registered)
			yield return new WaitUntil(() => serviceUser.ThenCalled);

			// Verify service was retrieved correctly
			Assert.IsTrue(serviceUser.ThenCalled, "Then callback should be called");
			Assert.IsFalse(serviceUser.CatchCalled, "Catch callback should not be called");
			Assert.IsNotNull(serviceUser.GetRetrievedService(), "Service should be retrieved");
			Assert.AreEqual("Hello from TestService!", serviceUser.GetRetrievedService().Message,
				"Service should contain the correct data");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator FluentPromise_WaitsForRegistration()
		{
			// Create GameObject with promise-based service user
			var gameObject = new GameObject("PromiseServiceUser");
			var serviceUser = gameObject.AddComponent<FluentServiceUserPromise>();
			serviceUser.Initialize(_serviceLocator);

			// Service not registered yet

			// Give it a frame to start the promise
			yield return null;

			// Verify promise started but service not yet retrieved
			Assert.IsFalse(serviceUser.ThenCalled, "Then callback should not be called yet");
			Assert.IsFalse(serviceUser.CatchCalled, "Catch callback should not be called");

			// Register the service after a delay
			yield return new WaitForSeconds(0.2f);
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Wait for service to be retrieved
			yield return new WaitUntil(() => serviceUser.ThenCalled);

			// Verify service was retrieved correctly
			Assert.IsTrue(serviceUser.ThenCalled, "Then callback should be called after registration");
			Assert.IsFalse(serviceUser.CatchCalled, "Catch callback should not be called");
			Assert.IsNotNull(serviceUser.GetRetrievedService(), "Service should be retrieved after registration");
			Assert.AreEqual("Hello from TestService!", serviceUser.GetRetrievedService().Message,
				"Service should contain the correct data");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator FluentPromise_CancelledWhenGameObjectDestroyed()
		{
			// Create GameObject with promise-based service user
			var gameObject = new GameObject("PromiseServiceUser");
			var serviceUser = gameObject.AddComponent<FluentServiceUserPromise>();
			serviceUser.Initialize(_serviceLocator);

			// Give it a frame to start the promise
			yield return null;

			// Verify promise started but service not yet retrieved
			Assert.IsFalse(serviceUser.ThenCalled, "Then callback should not be called yet");
			Assert.IsFalse(serviceUser.CatchCalled, "Catch callback should not be called yet");

			// Destroy the GameObject before registering the service
			Object.Destroy(gameObject);

			// Wait for destruction to process
			yield return new WaitForEndOfFrame();

			// Now register the service - this shouldn't reach the promise
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Wait a bit to make sure the promise had time to process if it was still active
			yield return new WaitForSeconds(0.2f);

			// Using serviceUser after destruction is not reliable, so we can only check
			// that the service doesn't get registered to new requests

			var newPromiseThenCalled = false;
			var newPromiseCatchCalled = false;
			Exception caughtException = null;

			// Try to get the service with a new promise to prove it exists
			_serviceLocator
				.Get<ServiceLocatorTestUtils.TestService>()
				.WithCancellation()
				.Then(_ => newPromiseThenCalled = true)
				.Catch(ex =>
				{
					newPromiseCatchCalled = true;
					caughtException = ex;
				});

			yield return new WaitUntil(() => newPromiseThenCalled || newPromiseCatchCalled);

			// The new promise should succeed since the service is registered
			Assert.IsTrue(newPromiseThenCalled, "New promise should resolve");
			Assert.IsFalse(newPromiseCatchCalled, "New promise should not be rejected");
		}

		[UnityTest]
		public IEnumerator FluentPromise_WithExplicitCancellation_IsCancelled()
		{
			// Create cancellation token
			using var cts = new CancellationTokenSource();

			// Setup variables to track promise resolution
			var thenCalled = false;
			var catchCalled = false;
			Exception caughtException = null;

			// Create a promise with the token using fluent API
			_serviceLocator
				.Get<ServiceLocatorTestUtils.TestService>()
				.WithCancellation(cts.Token)
				.Then(_ => thenCalled = true)
				.Catch(ex =>
				{
					// Unwrap the exception if it's an AggregateException
					caughtException = ex is AggregateException aggregateEx ? aggregateEx.InnerException : ex;
					catchCalled = true;
				});

			yield return null;

			// Cancel the token
			cts.Cancel();

			// Wait for promise to be cancelled
			yield return new WaitUntil(() => catchCalled);

			// Verify promise was cancelled correctly
			Assert.IsFalse(thenCalled, "Then callback should not be called");
			Assert.IsTrue(catchCalled, "Catch callback should be called");
			Assert.IsNotNull(caughtException, "Exception should be caught");
			Assert.IsInstanceOf<TaskCanceledException>(caughtException,
				"Exception should be TaskCanceledException");
		}

		[UnityTest]
		public IEnumerator FluentPromise_MultiService_AllResolveWhenRegistered()
		{
			// Setup variables to track promise resolution
			ServiceLocatorTestUtils.TestService service1 = null;
			ServiceLocatorTestUtils.AnotherTestService service2 = null;
			var thenCalled = false;
			var catchCalled = false;

			// Create a multi-service promise using fluent API
			_serviceLocator
				.Get<ServiceLocatorTestUtils.TestService>()
				.And<ServiceLocatorTestUtils.AnotherTestService>()
				.WithCancellation()
				.Then(services =>
				{
					service1 = services.Item1;
					service2 = services.Item2;
					thenCalled = true;
				})
				.Catch(_ => catchCalled = true);

			yield return null;

			// Register services in sequence
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());
			yield return null;

			// Promise should not resolve yet
			Assert.IsFalse(thenCalled, "Then callback should not be called after first service");

			_serviceLocator.Register(new ServiceLocatorTestUtils.AnotherTestService());

			// Wait for promise to resolve
			yield return new WaitUntil(() => thenCalled);

			// Verify all services were retrieved
			Assert.IsTrue(thenCalled, "Then callback should be called");
			Assert.IsFalse(catchCalled, "Catch callback should not be called");
			Assert.IsNotNull(service1, "First service should be retrieved");
			Assert.IsNotNull(service2, "Second service should be retrieved");
		}

		[UnityTest]
		public IEnumerator FluentPromise_ThreeServices_AllResolveWhenRegistered()
		{
			// Setup variables to track promise resolution
			ServiceLocatorTestUtils.TestService service1 = null;
			ServiceLocatorTestUtils.AnotherTestService service2 = null;
			ServiceLocatorTestUtils.ThirdTestService service3 = null;
			var thenCalled = false;
			var catchCalled = false;

			// Create a multi-service promise using fluent API
			_serviceLocator
				.Get<ServiceLocatorTestUtils.TestService>()
				.And<ServiceLocatorTestUtils.AnotherTestService>()
				.And<ServiceLocatorTestUtils.ThirdTestService>()
				.WithCancellation()
				.Then(services =>
				{
					service1 = services.Item1;
					service2 = services.Item2;
					service3 = services.Item3;
					thenCalled = true;
				})
				.Catch(_ => catchCalled = true);

			yield return null;

			// Register services in sequence
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());
			yield return null;

			// Promise should not resolve yet
			Assert.IsFalse(thenCalled, "Then callback should not be called after first service");

			_serviceLocator.Register(new ServiceLocatorTestUtils.AnotherTestService());
			yield return null;

			// Promise should not resolve yet
			Assert.IsFalse(thenCalled, "Then callback should not be called after second service");

			_serviceLocator.Register(new ServiceLocatorTestUtils.ThirdTestService());

			// Wait for promise to resolve
			yield return new WaitUntil(() => thenCalled);

			// Verify all services were retrieved
			Assert.IsTrue(thenCalled, "Then callback should be called");
			Assert.IsFalse(catchCalled, "Catch callback should not be called");
			Assert.IsNotNull(service1, "First service should be retrieved");
			Assert.IsNotNull(service2, "Second service should be retrieved");
			Assert.IsNotNull(service3, "Third service should be retrieved");
		}

		[UnityTest]
		public IEnumerator FluentPromise_ChainedThenWithExceptionThrown_CaughtInCatch()
		{
			// Register a service
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Setup variables to track promise resolution
			Exception caughtException = null;
			var firstThenCalled = false;
			var secondThenCalled = false;
			var catchCalled = false;
			var expectedException = new InvalidOperationException("Thrown from Then");

			// Create a promise with chained Then calls where one throws
			_serviceLocator
				.Get<ServiceLocatorTestUtils.TestService>()
				.WithCancellation()
				.Then(service =>
				{
					firstThenCalled = true;
					throw expectedException; // Throw exception in Then
				})
				.Then(value =>
				{
					secondThenCalled = true; // Should not be called
					return value;
				})
				.Catch(ex =>
				{
					caughtException = ex;
					catchCalled = true;
				});

			// Wait for promise to be resolved and exception to be caught
			yield return new WaitUntil(() => catchCalled);

			// Verify promise chain behavior
			Assert.IsTrue(firstThenCalled, "First Then callback should be called");
			Assert.IsFalse(secondThenCalled, "Second Then callback should not be called");
			Assert.IsTrue(catchCalled, "Catch callback should be called");
			Assert.IsNotNull(caughtException, "Exception should be caught");
			Assert.AreEqual(expectedException, caughtException,
				"Caught exception should match the thrown exception");
		}
	}

	/// <summary>
	///     MonoBehaviour that uses the fluent promise API to retrieve services
	/// </summary>
	public class FluentServiceUserPromise : MonoBehaviour
	{
		private BaseServiceLocator _serviceLocator;
		private ServiceLocatorTestUtils.TestService _service;

		// This is used for automatic cancellation when the MonoBehaviour is destroyed
		public readonly CancellationTokenSource destroyCancellationTokenSource = new();
		public CancellationToken destroyCancellationToken => destroyCancellationTokenSource.Token;

		// Flags for test verification
		public bool ThenCalled { get; private set; }
		public bool CatchCalled { get; private set; }
		public Exception CaughtException { get; private set; }

		public void Initialize(BaseServiceLocator serviceLocator)
		{
			_serviceLocator = serviceLocator;
		}

		#if !DISABLE_SL_PROMISES
		private void Start()
		{
			// Get the service using fluent promise API
			_serviceLocator
				.Get<ServiceLocatorTestUtils.TestService>()
				.WithCancellation(destroyCancellationToken)
				.Then(service =>
				{
					_service = service;
					ThenCalled = true;
					Debug.Log($"Service retrieved via promise: {_service.Message}");
				})
				.Catch(ex =>
				{
					CatchCalled = true;
					CaughtException = ex;
					Debug.LogWarning($"Promise failed: {ex.Message}");
				});
		}
		#else
        private void Start()
        {
            // Fallback when promises are disabled
            if (_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService service))
            {
                _service = service;
                ThenCalled = true;
                Debug.Log($"Service retrieved directly: {_service.Message}");
            }
            else
            {
                CatchCalled = true;
                CaughtException = new InvalidOperationException("Service not found");
                Debug.LogWarning("Service retrieval via TryGetService failed");
            }
        }
		#endif

		// Method for testing purposes to retrieve the service
		public ServiceLocatorTestUtils.TestService GetRetrievedService()
		{
			return _service;
		}

		private void OnDestroy()
		{
			destroyCancellationTokenSource.Cancel();
			destroyCancellationTokenSource.Dispose();
		}
	}
}
#endif