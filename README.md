# ServiceLocator #

## Overview ##
ServiceLocator is a Unity package designed to facilitate service registration and locating via a Scriptable Object-based Service Locator. This approach simplifies the management and access of services throughout your Unity application.

## Installation ##
To install ServiceLocator in your Unity project, add the package from the git URL: https://github.com/PaulNonatomic/ServiceLocator.git using the Unity package manager.

## Usage ##
### Creating a ServiceLocator Asset ###
In the Unity Editor, right-click in the Project panel.
Navigate to Create -> Service Locator to create a new ServiceLocator asset or use the provided DefaultServiceLocator asset.

### Registering Services ###
To register a service with the ServiceLocator, use the Register<T> method. Here's an example:

```cs
public class MyService : IMyService
{
    // Service implementation
}

ServiceLocator locator = // Get your ServiceLocator instance
locator.Register<IMyService>(new MyService());
```

### Accessing Services ###
To retrieve a registered service, use the GetService<T> method:

```cs
IMyService myService;

_serviceLocator.GetService<IMyService>().Task.ContinueWith(task =>
			{
				myService = task.Result;
			});
```

or

```cs
IMyService myService;
_serviceLocator.TryGetService<IMyService>(out myService);
```

### Unregistering Services ###
To unregister a service, use the Unregister<T> method:

```cs
locator.Unregister<IMyService>(myService);
```

## Contributing ##
Contributions are welcome! Please refer to CONTRIBUTING.md for guidelines on how to contribute.

## License ##
ServiceLocator is licensed under the MIT license. See LICENSE for more details.

## Alternative Solution ##
While the ServiceLocator pattern is useful, consider using Dependency Injection (DI) for a more robust solution. DI frameworks like Zenject can offer more control and flexibility in managing dependencies in Unity projects. 

