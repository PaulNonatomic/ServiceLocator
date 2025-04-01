using Nonatomic.ServiceLocator;
using UnityEngine;

namespace Tests.PlayMode
{
	/// <summary>
	///     Test MonoBehaviour that retrieves services using coroutines
	/// </summary>
	public class ServiceUserCoroutine : MonoBehaviour
	{
		private BaseServiceLocator _serviceLocator;

		// Flags for test verification
		public bool ServiceRetrieved { get; private set; }

		public bool CoroutineStarted { get; }

		public ServiceLocatorTestUtils.TestService RetrievedService { get; private set; }

		public void Initialize(BaseServiceLocator serviceLocator)
		{
			_serviceLocator = serviceLocator;
		}

		#if !DISABLE_SL_COROUTINES
        private void Start()
        {
            // Start the coroutine to get the service
            StartCoroutine(GetServiceRoutine());
        }

        private IEnumerator GetServiceRoutine()
        {
            _coroutineStarted = true;
            
            // Use the ServiceLocator coroutine to get the service
            yield return StartCoroutine(_serviceLocator.GetServiceCoroutine<TestService>(service => {
                _service = service;
                _serviceRetrieved = service != null;
                
                if (_service != null)
                {
                    Debug.Log($"Service retrieved via coroutine: {_service.Message}");
                }
                else
                {
                    Debug.LogWarning("Service retrieval via coroutine returned null");
                }
            }));
        }
		#else
		private void Start()
		{
			// Fallback when coroutines are disabled
			if (_serviceLocator.TryGetService(out ServiceLocatorTestUtils.TestService service))
			{
				RetrievedService = service;
				ServiceRetrieved = true;
				Debug.Log($"Service retrieved directly: {RetrievedService.Message}");
			}
			else
			{
				ServiceRetrieved = false;
				Debug.LogWarning("Service retrieval via TryGetService failed");
			}
		}
		#endif

		// Method for testing purposes to retrieve the service
		public ServiceLocatorTestUtils.TestService GetRetrievedService()
		{
			return RetrievedService;
		}
	}
}