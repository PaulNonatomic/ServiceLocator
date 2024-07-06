# ServiceLocator

## Overview
ServiceLocator is a Unity package that provides a flexible and efficient way to manage and access services throughout your Unity application. It's based on a ScriptableObject implementation, offering various methods for registering, unregistering, and retrieving services.

## Installation
To install ServiceLocator in your Unity project, add the package from the git URL: https://github.com/PaulNonatomic/ServiceLocator.git using the Unity package manager.

## Key Features
- ScriptableObject-based implementation for easy integration with Unity
- Support for synchronous and asynchronous service retrieval
- Promise-based service access
- Coroutine-based service retrieval
- Automatic service registration and unregistration for MonoBehaviours

## Usage

### Creating a ServiceLocator Asset

Note there is a DefaultServiceLocator asset included in the package should you wish to use this.<br>
The ServiceLocator property drawer will self populate with the first ServiceLocator asset it finds in the project.<br>
However should you wish to create a new instance of the ServiceLocator then...

1. In the Unity Editor, right-click in the Project panel.
2. Navigate to Create -> ServiceLocator to create a new ServiceLocator asset.

### Registering Services
To register a service with the ServiceLocator, use the `Register<T>` method:

```csharp
public class MyService : IMyService
{
    // Service implementation
}

ServiceLocator locator = // Get your ServiceLocator instance
locator.Register<IMyService>(new MyService());
```

### Accessing Services
ServiceLocator provides multiple ways to retrieve services:

1. Async/Await:
```csharp
IMyService myService = await locator.GetServiceAsync<IMyService>();
```

2. Promise-based:
```csharp
locator.GetService<IMyService>()
    .Then(service => {
        // Use the service
    })
    .Catch(ex => {
        Debug.LogError($"Failed to get service: {ex.Message}");
    });
```

3. Coroutine-based:
```csharp
StartCoroutine(locator.GetServiceCoroutine<IMyService>(service => {
    // Use the service
}));
```

4. Immediate (try-get):
```csharp
if (locator.TryGetService<IMyService>(out var myService))
{
    // Use the service
}
```

### Unregistering Services
To unregister a service:

```csharp
locator.Unregister<IMyService>();
```

### Automatic Service Registration with MonoService
For MonoBehaviour-based services, you can use the `MonoService<T>` base class:

```csharp
public class MyMonoService : MonoService<IMyService>, IMyService
{
    // Service implementation
}
```

This will automatically register the service when the MonoBehaviour is created and unregister it when destroyed.

## Advanced Features

### Cleanup
To clean up all registered services and pending promises:

```csharp
locator.CleanupServiceLocator();
```

### Initialization and De-initialization
ServiceLocator automatically initializes when enabled and de-initializes when disabled. You can also manually control this:

```csharp
// To manually initialize
locator.InitializeServiceLocator();

// To manually de-initialize
locator.DeInitializeServiceLocator();
```

## Contributing
Contributions are welcome! Please refer to CONTRIBUTING.md for guidelines on how to contribute.

## License
ServiceLocator is licensed under the MIT license. See LICENSE for more details.

## Alternative Solution
While the ServiceLocator pattern is useful, consider using Dependency Injection (DI) for a more robust solution. DI frameworks like Zenject can offer more control and flexibility in managing dependencies in Unity projects.
