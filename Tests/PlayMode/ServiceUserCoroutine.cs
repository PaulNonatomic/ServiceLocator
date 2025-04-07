using System.Collections;
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
       private Coroutine _serviceCoroutine;

       // Flag to track if the ServiceLocator has been cleaned up
       private bool _serviceLocatorCleaned;

       // Flags for test verification
       public bool ServiceRetrieved { get; private set; }

       public bool CoroutineStarted { get; private set; }

       public ServiceLocatorTestUtils.TestService RetrievedService { get; private set; }

       public void Initialize(BaseServiceLocator serviceLocator)
       {
          _serviceLocator = serviceLocator;

          // Subscribe to the OnChange event to detect cleanup
          if (_serviceLocator != null)
          {
             _serviceLocator.OnChange += CheckServiceLocatorState;
          }
       }

       private void CheckServiceLocatorState()
       {
          // Check if the ServiceLocator has been cleaned up
          if (_serviceLocator != null && _serviceLocator.GetAllServices().Count == 0)
          {
             _serviceLocatorCleaned = true;
             StopServiceCoroutine();
          }
       }

       #if ENABLE_SL_COROUTINES || !DISABLE_SL_COROUTINES
       private void Start()
       {
          // Start the coroutine to get the service
          _serviceCoroutine = StartCoroutine(GetServiceRoutine());
       }

       private IEnumerator GetServiceRoutine()
       {
          CoroutineStarted = true;

          // Use the ServiceLocator coroutine to get the service
          var locatorCoroutine = _serviceLocator.GetServiceCoroutine<ServiceLocatorTestUtils.TestService>(service =>
          {
             // Check if ServiceLocator has been cleaned up
             if (!_serviceLocatorCleaned)
             {
                RetrievedService = service;
                ServiceRetrieved = service != null;

                if (RetrievedService != null)
                {
                   Debug.Log($"Service retrieved via coroutine: {RetrievedService.Message}");
                }
                else
                {
                   Debug.LogWarning("Service retrieval via coroutine returned null");
                }
             }
             else
             {
                Debug.LogWarning("Service retrieved after ServiceLocator cleanup, ignoring result");
             }
          });

          yield return StartCoroutine(locatorCoroutine);
       }

       // Method to forcibly stop the service coroutine
       public void StopServiceCoroutine()
       {
          if (_serviceCoroutine != null)
          {
             StopCoroutine(_serviceCoroutine);
             _serviceCoroutine = null;
          }
       }

       private void OnDestroy()
       {
          // Unsubscribe from events
          if (_serviceLocator != null)
          {
             _serviceLocator.OnChange -= CheckServiceLocatorState;
          }

          StopServiceCoroutine();
       }
       #else
       private void Start()
       {
          // Fallback when coroutines are disabled
          CoroutineStarted = true;
          
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
    }
}