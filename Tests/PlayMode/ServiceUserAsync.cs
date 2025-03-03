using System;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using Tests.PlayMode;
using UnityEngine;

public class ServiceUserAsync : MonoBehaviour
{
	private BaseServiceLocator _serviceLocator;
	private TestService _service;
	private Task<TestService> _serviceTask;

	public void Initialize(BaseServiceLocator serviceLocator)
	{
		_serviceLocator = serviceLocator;
	}

	private async void Start()
	{
		try
		{
			_serviceTask = _serviceLocator.GetServiceAsync<TestService>(destroyCancellationToken);
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

	public TestService GetRetrievedService() => _service;
	public Task<TestService> GetServiceTask() => _serviceTask;
}