using System;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Tests.PlayMode
{
	public class ServiceUserAsync : MonoBehaviour
	{
		private BaseServiceLocator _serviceLocator;
		private ServiceLocatorTestUtils.TestService _service;
		private Task<ServiceLocatorTestUtils.TestService> _serviceTask;

		// This is used for automatic cancellation when the MonoBehaviour is destroyed
		public readonly CancellationTokenSource destroyCancellationTokenSource = new();
		public CancellationToken destroyCancellationToken => destroyCancellationTokenSource.Token;

		public void Initialize(BaseServiceLocator serviceLocator)
		{
			_serviceLocator = serviceLocator;
		}

		#if !DISABLE_SL_ASYNC
		private async void Start()
		{
			try
			{
				_serviceTask =
					_serviceLocator.GetServiceAsync<ServiceLocatorTestUtils.TestService>(destroyCancellationToken);
				_service = await _serviceTask;
				Debug.Log(_service.Message);
			}
			catch (OperationCanceledException)
			{
				Debug.Log("Service retrieval was canceled due to MonoBehaviour destruction.");
			}
			catch (Exception ex)
			{
				Debug.LogError($"Failed to retrieve service: {ex.Message}");
			}
		}
		#else
        private void Start()
        {
            // Alternative implementation when async services are disabled
            if (_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService service))
            {
                _service = service;
                Debug.Log(_service.Message);
            }
            else
            {
                Debug.LogError("Failed to retrieve service via TryGetService.");
            }
        }
		#endif

		public ServiceLocatorTestUtils.TestService GetRetrievedService()
		{
			return _service;
		}

		#if !DISABLE_SL_ASYNC
		public Task<ServiceLocatorTestUtils.TestService> GetServiceTask()
		{
			return _serviceTask;
		}
		#endif

		private void OnDestroy()
		{
			destroyCancellationTokenSource.Cancel();
			destroyCancellationTokenSource.Dispose();
		}
	}
}