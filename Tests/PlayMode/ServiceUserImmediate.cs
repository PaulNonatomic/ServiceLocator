using System;
using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Tests.PlayMode
{
	public class ServiceUserImmediate : MonoBehaviour
	{
		private BaseServiceLocator _serviceLocator;
		private TestService _service;

		// Inject the ServiceLocator instance and register immediately
		public void Initialize(BaseServiceLocator serviceLocator)
		{
			_serviceLocator = serviceLocator;
			_serviceLocator.Register(new TestService()); // Register immediately
		}

		private async void Start()
		{
			try
			{
				_service = await _serviceLocator.GetServiceAsync<TestService>(destroyCancellationToken);
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

		// Method for testing purposes to retrieve the service
		public TestService GetRetrievedService()
		{
			return _service;
		}
	}
}