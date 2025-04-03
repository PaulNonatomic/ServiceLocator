using System;
using System.Threading;
using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Tests.PlayMode
{
	/// <summary>
	///     Test MonoBehaviour that retrieves services using promises
	/// </summary>
	public class ServiceUserPromise : MonoBehaviour
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
			// Get the service using promises
			_serviceLocator
				.GetService<
					ServiceLocatorTestUtils.TestService>(
					destroyCancellationToken) // Fixed: added namespace to TestService
				.Then(service =>
				{
					_service = service;
					ThenCalled = true; // Fixed: using property instead of undefined _thenCalled
					Debug.Log($"Service retrieved via promise: {_service.Message}");
				})
				.Catch(ex =>
				{
					CatchCalled = true; // Fixed: using property instead of undefined _catchCalled
					CaughtException = ex; // Fixed: using property instead of undefined _caughtException
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