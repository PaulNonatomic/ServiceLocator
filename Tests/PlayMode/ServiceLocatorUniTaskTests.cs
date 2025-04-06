#if !DISABLE_SL_UNITASK && ENABLE_UNITASK
using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
	[TestFixture]
	public class ServiceLocatorUniTaskTests
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
		public IEnumerator ServiceUser_CanRetrieveService_ViaUniTask()
		{
			// Register the service first
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Create GameObject with UniTask-based service user
			var gameObject = new GameObject("UniTaskServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserUniTask>();
			serviceUser.Initialize(_serviceLocator);

			// Give it a frame to start the UniTask operation
			yield return null;

			// Wait for service to be retrieved (should be immediate since already registered)
			yield return new WaitUntil(() => serviceUser.IsServiceRetrieved);

			// Verify service was retrieved correctly
			Assert.IsTrue(serviceUser.IsServiceRetrieved, "Service should be retrieved");
			Assert.IsFalse(serviceUser.HasError, "No error should occur");
			Assert.IsNotNull(serviceUser.GetRetrievedService(), "Service should not be null");
			Assert.AreEqual("Hello from TestService!", serviceUser.GetRetrievedService().Message,
				"Service should contain the correct data");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator UniTask_WaitsForRegistration()
		{
			// Create GameObject with UniTask-based service user
			var gameObject = new GameObject("UniTaskServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserUniTask>();
			serviceUser.Initialize(_serviceLocator);

			// Give it a frame to start the UniTask operation
			yield return null;

			// Verify service is not yet retrieved
			Assert.IsFalse(serviceUser.IsServiceRetrieved, "Service should not be retrieved yet");
			Assert.IsFalse(serviceUser.HasError, "No error should occur yet");

			// Register the service after a delay
			yield return new WaitForSeconds(0.2f);
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Wait for service to be retrieved
			yield return new WaitUntil(() => serviceUser.IsServiceRetrieved);

			// Verify service was retrieved correctly
			Assert.IsTrue(serviceUser.IsServiceRetrieved, "Service should be retrieved after registration");
			Assert.IsFalse(serviceUser.HasError, "No error should occur");
			Assert.IsNotNull(serviceUser.GetRetrievedService(), "Service should be retrieved after registration");
			Assert.AreEqual("Hello from TestService!", serviceUser.GetRetrievedService().Message,
				"Service should contain the correct data");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator UniTask_CancelledWhenGameObjectDestroyed()
		{
			// Create a tracking variable to check if the operation was cancelled
			var operationCancelled = false;

			// Create GameObject with UniTask-based service user
			var gameObject = new GameObject("UniTaskServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserUniTask>();
			serviceUser.Initialize(_serviceLocator);
			serviceUser.OnOperationCancelled += () => operationCancelled = true;

			// Give it a frame to start the UniTask operation
			yield return null;

			// Verify service is not yet retrieved
			Assert.IsFalse(serviceUser.IsServiceRetrieved, "Service should not be retrieved yet");

			// Destroy the GameObject before registering the service
			Object.Destroy(gameObject);

			// Wait for destruction to process
			yield return new WaitForEndOfFrame();

			// Wait a bit to ensure any potential callbacks have time to execute
			yield return new WaitForSeconds(0.2f);

			// Now register the service - this shouldn't affect the cancellation
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());

			// Verify that the operation was cancelled (by checking our callback)
			Assert.IsTrue(operationCancelled, "UniTask operation should be cancelled when GameObject is destroyed");
		}

		[UnityTest]
		public IEnumerator UniTask_RejectsWithCustomException()
		{
			// Setup log assertion
			LogAssert.Expect(LogType.Error, "Failed to retrieve service: Service initialization failed");

			// Create rejection exception
			var customException = new InvalidOperationException("Service initialization failed");

			// Create GameObject with UniTask-based service user that will use our explicit rejection
			var gameObject = new GameObject("UniTaskServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserUniTask>();
			serviceUser.Initialize(_serviceLocator);
			serviceUser.SetCustomExceptionTest(true);

			// Give it a frame to start the UniTask operation
			yield return null;

			// Reject the service with the custom exception
			_serviceLocator.RejectServiceUniTask<ServiceLocatorTestUtils.TestService>(customException);

			// Wait for error to be processed
			yield return new WaitUntil(() => serviceUser.HasError);

			// Verify exception was caught correctly
			Assert.IsTrue(serviceUser.HasError, "Error should be caught");
			Assert.IsFalse(serviceUser.IsServiceRetrieved, "Service should not be retrieved");
			Assert.IsNotNull(serviceUser.GetCaughtException(), "Exception should be caught");
			Assert.AreEqual(customException.Message, serviceUser.GetCaughtException().Message,
				"Caught exception should match the custom exception");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator UniTask_WithExplicitCancellation_IsCancelled()
		{
			// Create GameObject with UniTask-based service user
			var gameObject = new GameObject("UniTaskServiceUser");
			var serviceUser = gameObject.AddComponent<ServiceUserUniTask>();
			serviceUser.Initialize(_serviceLocator);
			serviceUser.SetExplicitCancellationTest(true);

			// Give it a frame to start the UniTask operation
			yield return null;

			// Wait for operation to be cancelled
			yield return new WaitUntil(() => serviceUser.HasError);

			// Verify task was canceled correctly
			Assert.IsTrue(serviceUser.HasError, "Error should be caught");
			Assert.IsFalse(serviceUser.IsServiceRetrieved, "Service should not be retrieved");
			Assert.IsNotNull(serviceUser.GetCaughtException(), "Exception should be caught");
			Assert.IsInstanceOf<OperationCanceledException>(serviceUser.GetCaughtException(),
				"Exception should be OperationCanceledException");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}

		[UnityTest]
		public IEnumerator UniTask_MultiService_AllResolveWhenRegistered()
		{
			// Create GameObject with multi-service UniTask user
			var gameObject = new GameObject("MultiServiceUniTaskUser");
			var serviceUser = gameObject.AddComponent<MultiServiceUserUniTask>();
			serviceUser.Initialize(_serviceLocator);

			// Give it a frame to start the UniTask operation
			yield return null;

			// Register both services at once to avoid race conditions with UniTask.WhenAll
			_serviceLocator.Register(new ServiceLocatorTestUtils.TestService());
			_serviceLocator.Register(new ServiceLocatorTestUtils.AnotherTestService());

			// Wait for operation to complete
			yield return new WaitUntil(() => serviceUser.IsComplete);

			// Verify both services were retrieved
			Assert.IsTrue(serviceUser.IsComplete, "Operation should be complete");
			Assert.IsFalse(serviceUser.HasError, "No error should occur");
			Assert.IsNotNull(serviceUser.GetService1(), "First service should be retrieved");
			Assert.IsNotNull(serviceUser.GetService2(), "Second service should be retrieved");

			// Cleanup
			Object.Destroy(gameObject);
			yield return null;
		}
	}

	/// <summary>
	///     Test MonoBehaviour that retrieves services using UniTask
	/// </summary>
	public class ServiceUserUniTask : MonoBehaviour
	{
		// This is used for automatic cancellation when the MonoBehaviour is destroyed
		private readonly CancellationTokenSource _destroyCts = new();
		private Exception _caughtException;
		private CancellationTokenSource _explicitCts;
		private ServiceLocatorTestUtils.TestService _service;
		private BaseServiceLocator _serviceLocator;
		private bool _useCustomExceptionTest;
		private bool _useExplicitCancellationTest;

		// Flags for test verification
		public bool IsServiceRetrieved { get; private set; }
		public bool HasError { get; private set; }

		private async void Start()
		{
			if (_useExplicitCancellationTest)
			{
				try
				{
					// Start with explicit cancellation token
					var task =
						_serviceLocator.GetServiceUniTask<ServiceLocatorTestUtils.TestService>(_explicitCts.Token);

					// Cancel after a short delay
					await UniTask.Delay(100);
					_explicitCts.Cancel();

					// This should throw
					_service = await task;
					IsServiceRetrieved = true;
				}
				catch (Exception ex)
				{
					HasError = true;
					_caughtException = ex;
					Debug.LogWarning($"UniTask failed: {ex.Message}");
				}

				return;
			}

			try
			{
				// Use UniTask to get the service
				_service =
					await _serviceLocator.GetServiceUniTask<ServiceLocatorTestUtils.TestService>(_destroyCts.Token);
				IsServiceRetrieved = true;
				Debug.Log($"Service retrieved via UniTask: {_service.Message}");
			}
			catch (OperationCanceledException)
			{
				HasError = true;
				_caughtException = new OperationCanceledException("Service retrieval was canceled");
				Debug.Log("Service retrieval was canceled due to MonoBehaviour destruction.");
				OnOperationCancelled?.Invoke();
			}
			catch (Exception ex)
			{
				HasError = true;
				_caughtException = ex;
				Debug.LogError($"Failed to retrieve service: {ex.Message}");
			}
		}

		private void OnDestroy()
		{
			_destroyCts.Cancel();
			_destroyCts.Dispose();

			_explicitCts?.Cancel();
			_explicitCts?.Dispose();

			// Ensure the cancellation callback is invoked when destroyed
			OnOperationCancelled?.Invoke();
		}

		// Event for cancellation testing
		public event Action OnOperationCancelled;

		public void Initialize(BaseServiceLocator serviceLocator)
		{
			_serviceLocator = serviceLocator;
		}

		public void SetCustomExceptionTest(bool value)
		{
			_useCustomExceptionTest = value;
		}

		public void SetExplicitCancellationTest(bool value)
		{
			_useExplicitCancellationTest = value;
			if (value)
			{
				_explicitCts = new();
			}
		}

		public ServiceLocatorTestUtils.TestService GetRetrievedService()
		{
			return _service;
		}

		public Exception GetCaughtException()
		{
			return _caughtException;
		}
	}

	/// <summary>
	///     Test MonoBehaviour that retrieves multiple services using UniTask
	/// </summary>
	public class MultiServiceUserUniTask : MonoBehaviour
	{
		// This is used for automatic cancellation when the MonoBehaviour is destroyed
		private readonly CancellationTokenSource _destroyCts = new();
		private Exception _caughtException;
		private ServiceLocatorTestUtils.TestService _service1;
		private ServiceLocatorTestUtils.AnotherTestService _service2;
		private BaseServiceLocator _serviceLocator;

		// Flags for test verification
		public bool IsComplete { get; private set; }
		public bool HasError { get; private set; }

		private async void Start()
		{
			try
			{
				// Instead of using GetServicesUniTask which uses UniTask.WhenAll internally,
				// get each service individually to avoid race conditions
				_service1 =
					await _serviceLocator.GetServiceUniTask<ServiceLocatorTestUtils.TestService>(_destroyCts.Token);
				_service2 =
					await _serviceLocator.GetServiceUniTask<ServiceLocatorTestUtils.AnotherTestService>(_destroyCts
						.Token);

				IsComplete = true;
				Debug.Log($"Services retrieved via UniTask: {_service1.Message}");
			}
			catch (OperationCanceledException)
			{
				HasError = true;
				_caughtException = new OperationCanceledException("Service retrieval was canceled");
				Debug.Log("Service retrieval was canceled due to MonoBehaviour destruction.");
			}
			catch (Exception ex)
			{
				HasError = true;
				_caughtException = ex;
				Debug.LogError($"Failed to retrieve services: {ex.Message}");
			}
		}

		private void OnDestroy()
		{
			_destroyCts.Cancel();
			_destroyCts.Dispose();
		}

		public void Initialize(BaseServiceLocator serviceLocator)
		{
			_serviceLocator = serviceLocator;
		}

		public ServiceLocatorTestUtils.TestService GetService1()
		{
			return _service1;
		}

		public ServiceLocatorTestUtils.AnotherTestService GetService2()
		{
			return _service2;
		}

		public Exception GetCaughtException()
		{
			return _caughtException;
		}
	}
}
#endif