<div align=center>   

<p align="center">
  <img src="Readme~\logo.png" width="250">
</p>

### A flexible & efficient way to manage and access services in <a href="https://unity.com/">Unity</a>

ServiceLocator is based on a ScriptableObject implementation, offering various methods for registering, unregistering, and retrieving services.


[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![PullRequests](https://img.shields.io/badge/PRs-welcome-blueviolet)](http://makeapullrequest.com)
[![Releases](https://img.shields.io/github/v/release/PaulNonatomic/ServiceLocator)](https://github.com/PaulNonatomic/ServiceLocator/releases)
[![Unity](https://img.shields.io/badge/Unity-2022.3+-black.svg)](https://unity3d.com/pt/get-unity/download/archive)

</div>

---

## Installation
To install ServiceLocator in your Unity project, add the package from the git URL: https://github.com/PaulNonatomic/ServiceLocator.git using the Unity package manager.

## Key Features
- `ScriptableObject`-based implementation for seamless Unity integration
- Synchronous, asynchronous, and promise-based service retrieval
- Coroutine support for service access
- Multi-service retrieval in a single call
- Cancellation support with `CancellationToken`, including `MonoBehaviour.destroyCancellationToken`
- Robust error handling and service rejection mechanisms
- Automatic scene tracking for services with a fallback cleanup method when scenes are unloaded

## Usage

### Creating a ServiceLocator Asset
A `DefaultServiceLocator` asset is included in the package for immediate use. The `ServiceLocator` property drawer automatically populates with the first `ServiceLocator` asset found in the project. To create a custom instance:

1. Right-click in the Unity Editor's Project panel.
2. Navigate to `Create -> ServiceLocator` to create a new `ServiceLocator` asset.

### Registering Services
Register services using the `Register<T>` method:

```csharp
public interface IMyService
{
    void DoSomething();
}

public class MyService : IMyService
{
    public void DoSomething()
    {
        Debug.Log("Doing something!");
    }
}

// Obtain your ServiceLocator instance
ServiceLocator locator; // ... reference to your ServiceLocator asset
locator.Register<IMyService>(new MyService());

```

### Accessing Services
ServiceLocator offers multiple retrieval methods:

1. Async/Await - Single Service:
```csharp
IMyService myService = await _serviceLocator.GetServiceAsync<IMyService>();
myService.DoSomething();
```

2. Promise-based - Single Service:
```csharp
_serviceLocator.GetService<IMyService>()
    .Then(service => service.DoSomething())
    .Catch(ex => Debug.LogError($"Failed to get service: {ex.Message}"));
```

3. Coroutine-based - Single Service:
```csharp
StartCoroutine(_serviceLocator.GetServiceCoroutine<IMyService>(service => 
{
    service?.DoSomething();
}));

```

4. Immediate (Try-Get) - Single Service
```csharp
if (_serviceLocator.TryGetService<IMyService>(out var myService))
{
    myService.DoSomething();
}
```

### Retrieving Multiple Services

Retrieve multiple services in one call (up to 6 supported):

1. Async/Await - Two Services
```csharp
var (myService, anotherService) = await _serviceLocator.GetServicesAsync<IMyService, IAnotherService>();
myService.DoSomething();
anotherService.DoAnotherThing();
```
#### Promise-based - Three Services
```csharp
_serviceLocator.GetService<IMyService, IAnotherService, IThirdService>()
    .Then(services =>
    {
        services.Item1.DoSomething();
        services.Item2.DoAnotherThing();
        services.Item3.DoThirdThing();
    })
    .Catch(ex => Debug.LogError($"Failed to get services: {ex.Message}"));
```

### Unregistering Services
Unregister a service when no longer needed:

```csharp
_serviceLocator.Unregister<IMyService>();
```

### Simplified Service Registration with MonoService
Use MonoService<T> for automatic registration/unregistration::

```csharp
public interface IMyService
{
    void DoSomething();
}

public class MyMonoService : MonoService<IMyService>, IMyService
{
    public void DoSomething()
    {
        Debug.Log("Doing something from MonoService!");
    }

    protected override void Awake()
    {
        base.Awake();
        // Additional initialization if needed
        ServiceReady(); // Registers the service
    }
}
```

### MonoService Inheritance
You can create abstract base classes that inherit from MonoService to establish service patterns:
```csharp
// Base abstract class for all mini-game score services
public abstract class MiniGameScoreService<T> : MonoService<T>, IMiniGameScoreService 
    where T: class, IMiniGameScoreService
{
    // Common score tracking functionality
    protected int _score;
    
    public virtual int GetScore() => _score;
    public virtual void AddScore(int points) 
    {
        _score += points;
        OnScoreChanged?.Invoke(_score);
    }
    
    public event Action<int> OnScoreChanged;
    
    // Other shared functionality...
}

// Concrete implementation for a specific game
public class MyGameScoreService : MiniGameScoreService<IMyGameScoreService>, IMyGameScoreService 
{
    // Game-specific scoring logic
    public void AddComboBonus(int comboCount)
    {
        AddScore(comboCount * 10);
    }
    
    protected override void Awake()
    {
        base.Awake();
        _score = 0; // Initialize score
        ServiceReady(); // Register with ServiceLocator
    }
}
```
**Usage:**
```csharp
public class GameController : MonoBehaviour
{
    [SerializeField] private ServiceLocator _serviceLocator;
    
    protected virtual async void Awake()
    {
        // Get the specific mini-game score service
        // This works because MyGameScoreService registers as IMyGameScoreService
        var scoreService = await _serviceLocator.GetServiceAsync<IMyGameScoreService>();
        scoreService.AddComboBonus(5);
        
        // This would get the specific implementation, not a generic IMiniGameScoreService
        // var genericScoreService = await _serviceLocator.GetServiceAsync<IMiniGameScoreService>(); // Won't work!
        
        // You can still access the common interface methods
        scoreService.AddScore(100);
    }
}
```
## Advanced Use Cases
Using Cancellation Token

ServiceLocator supports `CancellationToken` for canceling service retrieval, particularly useful with `MonoBehaviour.destroyCancellationToken` to handle cleanup when objects are destroyed.

**Example: Canceling Async Service Retrieval**
```csharp
public class ServiceUser : MonoBehaviour
{
    [SerializeField] private ServiceLocator _serviceLocator;

    private async void Start()
    {
        try
        {
            var service = await _serviceLocator.GetServiceAsync<IMyService>(destroyCancellationToken);
            service.DoSomething();
        }
        catch (TaskCanceledException)
        {
            Debug.Log("Service retrieval canceled due to object destruction.");
        }
    }
}
```
**Behavior:** If the MonoBehaviour is destroyed before the service is retrieved, the task cancels automatically, preventing memory leaks or invalid operations.

**Example: Canceling Promise-based Retrieval**
```csharp
public class PromiseUser : MonoBehaviour
{
    [SerializeField] private ServiceLocator locator;

    private void Start()
    {
        locator.GetService<IMyService>(destroyCancellationToken)
            .Then(service => service.DoSomething())
            .Catch(ex => Debug.Log($"Retrieval failed or canceled: {ex.Message}"));
    }
}
```
**Behavior:** The promise rejects with a TaskCanceledException if the object is destroyed, ensuring safe cleanup.

### Linked Cancellation Tokens
The ServiceLocator uses [CancellationTokenSource.CreateLinkedTokenSource](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.createlinkedtokensource?view=net-9.0) internally to provide robust cancellation behavior for multi-service operations. This offers several benefits:

- **All-or-nothing cancellation**: If any service in a multi-service request fails or cancels, all other pending requests are automatically cancelled
- **Single control point**: One cancellation token affects all dependent operations
- **Reduced resource leakage**: Proper cleanup of all related operations when cancellation occurs
- **Consistent application state**: Prevents partial results where some parts of a tuple are filled while others aren't
 
**Example: Cancelling Multi-Service Operations**

Utilise [MonoBehaviour.destroyCancellationToken](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour-destroyCancellationToken.html) to cancel multi-service operations when the object is destroyed with ease.
```csharp
public class MultiServiceUser : MonoBehaviour
{
    [SerializeField] private ServiceLocator _serviceLocator;

    private async void Start()
    {
        try 
        {
            // Request multiple services with the same token
            var (service1, service2, service3) = await _serviceLocator.GetServicesAsync<
                IAuthService, 
                IUserService, 
                IDataService
            >(destroyCancellationToken);
            
            // All services successfully retrieved
            ProcessServices(service1, service2, service3);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Multi-service operation was cancelled");
        }
    }
}
```

## Error Handling and Service Rejection
ServiceLocator provides mechanisms to handle errors and explicitly reject service promises.

**Example: Handling Errors with Async/Await**
```csharp
public class ErrorHandler : MonoBehaviour
{
    [SerializeField] private ServiceLocator locator;

    private async void Start()
    {
        try
        {
            var service = await locator.GetServiceAsync<IMyService>();
            service.DoSomething();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to retrieve service: {ex.Message}");
        }
    }
}
```
**Behavior:** Catches any exceptions, such as cancellation or rejection, thrown during retrieval.

**Example: Rejecting a Service with a Custom Exception**
```csharp
public class ServiceRejector : MonoBehaviour
{
    [SerializeField] private ServiceLocator locator;

    private void Start()
    {
        locator.GetService<IMyService>()
            .Then(service => service.DoSomething())
            .Catch(ex => Debug.LogError($"Service rejected: {ex.Message}"));

        // Simulate a failure to initialize the service
        locator.RejectService<IMyService>(new InvalidOperationException("Service initialization failed"));
    }
}
```
**Behavior:** RejectService triggers the Catch handler with the custom exception, allowing you to handle initialization failures gracefully.

**Example: Combining Cancellation and Rejection**
```csharp
public class CombinedHandler : MonoBehaviour
{
    [SerializeField] private ServiceLocator _serviceLocator;

    private void Start()
    {
        _serviceLocator.GetService<IMyService>(destroyCancellationToken)
            .Then(service => service.DoSomething())
            .Catch(ex =>
            {
                if (ex is TaskCanceledException)
                {
                    Debug.Log("Retrieval canceled due to destruction.");
                }
                else
                {
                    Debug.LogError($"Service retrieval failed: {ex.Message}");
                }
            });

        // Simulate rejection if initialization fails
        if (someCondition)
        {
            _serviceLocator.RejectService<IMyService>(new InvalidOperationException("Initialization failed"));
        }
    }
}
```
**Behavior:** Handles both cancellation (e.g., object destruction) and explicit rejection (e.g., initialization failure) in a single Catch block.

### Cleaning Up
Manually clean up services and promises:

```csharp
_serviceLocator.Cleanup(); // Clears services and cancels pending promises
```

When a service is unregistered (either manually or through scene unloading), any pending promises for that service will be automatically rejected with an ObjectDisposedException, allowing consumers to handle the unavailability gracefully.

### Scene Management

ServiceLocator automatically tracks which scene each MonoBehaviour-based service belongs to. When a scene is unloaded, any services from that scene are automatically unregistered to prevent memory leaks.
If service clean is handled correctly this fallback should not be needed, but it is a good safety net and a warning will be logged for each service that is not cleaned up via the fallback.

**Example: Automatic Scene Cleanup**
```csharp
// When TestScene is unloaded, all services registered from that scene
// will be automatically unregistered, and any pending promises will be rejected
Behavior: This prevents "zombie" services from persisting after their scene has been unloaded, which could lead to memory leaks or unexpected behavior.
    


### Initialization and De-initialization
ServiceLocator initializes/de-initializes automatically, but you can control it manually:

```csharp
_serviceLocator.Initialize(); // Manual initialization
_serviceLocator.DeInitialize(); // Manual cleanup
```

## Contributing
Contributions are welcome! Please refer to CONTRIBUTING.md for guidelines on how to contribute.

## License
ServiceLocator is licensed under the MIT license. See LICENSE for more details.

## Alternative Solution
For more complex dependency management, consider Dependency Injection frameworks like Zenject. ServiceLocator is ideal for simpler service management needs in Unity.