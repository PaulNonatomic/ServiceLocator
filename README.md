# ServiceLocator

## Overview
ServiceLocator is a Unity package that provides a flexible and efficient way to manage and access services throughout your Unity application. It's based on a ScriptableObject implementation, offering various methods for registering, unregistering, and retrieving services.

## Installation
To install ServiceLocator in your Unity project, add the package from the git URL: https://github.com/PaulNonatomic/ServiceLocator.git using the Unity package manager.

## Key Features
- `ScriptableObject`-based implementation for seamless Unity integration
- Synchronous, asynchronous, and promise-based service retrieval
- Coroutine support for service access
- Multi-service retrieval in a single call
- Cancellation support with `CancellationToken`, including `MonoBehaviour.destroyCancellationToken`
- Robust error handling and service rejection mechanisms

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
ServiceLocator locator = // ... reference to your ServiceLocator asset
locator.Register<IMyService>(new MyService());

```

### Accessing Services
ServiceLocator offers multiple retrieval methods:

1. Async/Await - Single Service:
```csharp
IMyService myService = await locator.GetServiceAsync<IMyService>();
myService.DoSomething();
```

2. Promise-based - Single Service:
```csharp
locator.GetService<IMyService>()
    .Then(service => service.DoSomething())
    .Catch(ex => Debug.LogError($"Failed to get service: {ex.Message}"));
```

3. Coroutine-based - Single Service:
```csharp
StartCoroutine(locator.GetServiceCoroutine<IMyService>(service => 
{
    service?.DoSomething();
}));

```

4. Immediate (Try-Get) - Single Service
```csharp
if (locator.TryGetService<IMyService>(out var myService))
{
    myService.DoSomething();
}
```

### Retrieving Multiple Services

Retrieve multiple services in one call (up to 6 supported):

1. Async/Await - Two Services
```csharp
var (myService, anotherService) = await locator.GetServicesAsync<IMyService, IAnotherService>();
myService.DoSomething();
anotherService.DoAnotherThing();
```
#### Promise-based - Three Services
```csharp
locator.GetService<IMyService, IAnotherService, IThirdService>()
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
locator.Unregister<IMyService>();
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

## Advanced Use Cases
Using Cancellation Token

ServiceLocator supports `CancellationToken` for canceling service retrieval, particularly useful with `MonoBehaviour.destroyCancellationToken` to handle cleanup when objects are destroyed.

**Example: Canceling Async Service Retrieval**
```csharp
public class ServiceUser : MonoBehaviour
{
    [SerializeField] private ServiceLocator locator;

    private async void Start()
    {
        try
        {
            var service = await locator.GetServiceAsync<IMyService>(destroyCancellationToken);
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
    [SerializeField] private ServiceLocator locator;

    private void Start()
    {
        locator.GetService<IMyService>(destroyCancellationToken)
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
            locator.RejectService<IMyService>(new InvalidOperationException("Initialization failed"));
        }
    }
}
```
**Behavior:** Handles both cancellation (e.g., object destruction) and explicit rejection (e.g., initialization failure) in a single Catch block.

### Cleaning Up
Manually clean up services and promises:

```csharp
locator.Cleanup(); // Clears services and cancels pending promises
```

### Initialization and De-initialization
ServiceLocator initializes/de-initializes automatically, but you can control it manually:

```csharp
locator.Initialize(); // Manual initialization
locator.DeInitialize(); // Manual cleanup
```

## Contributing
Contributions are welcome! Please refer to CONTRIBUTING.md for guidelines on how to contribute.

## License
ServiceLocator is licensed under the MIT license. See LICENSE for more details.

## Alternative Solution
For more complex dependency management, consider Dependency Injection frameworks like Zenject. ServiceLocator is ideal for simpler service management needs in Unity.