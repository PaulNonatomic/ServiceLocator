# Change Log

## [0.5.0] - Nov 25, 2024
- BREAKING change to how the ServicePromise resolves. Task.ContinueWith it turns out operates off the main thread which is dangerous for Unity Objects.
As a solution to this the promise now resolves via the SynchronizationContext, however this means that the promise will not resolve until the next frame.
As a workaround for this, promises now resolve immediately for registered services. The breaking change here is that ServicePromises retrieved in the Awake method will not resolve
in time to supply the Start method. However, simply moving the retrieval to the Start method will resolve this issue.
- Added addition unit tests to ensure the ServicePromise resolves on the main thread

## [0.4.2] - Nov 18, 2024
- Removed the initialize on load attribute from the Reference Fixer as it was slowwwwwww.

## [0.4.1] - Nov 08, 2024
- Added a tool to fix missing references to the ServiceLocator. This comes off the back of realising that
the property drawer should probably not be assigning properties and can cause serialization issues. 

## [0.4.0] - Nov 07, 2024
- Package has been battle tested and is now considered stable so i've dropped the beta tag
- Tweaked support for nullable types in the GetService method
- Added support for retrieving multiple services via GetService and GetServiceAsync

## [0.3.2-beta] - Aug 28, 2024
- Exposed the ServiceLocator of MonoService through a protected member
	- As the ServiceLocator field is serialized i've included the FormerlySerializedAs attribute to resolve any broken serialization for now. 
	- This will be removed in a future update.
  
## [0.3.1-beta] - Jul 06, 2024
- Fix for GetServiceCoroutine running a perpetual while loop if a service is never registered.

## [0.3.0-beta] - Jul 06, 2024
- Completely rewrote this to simplify the promise interface, add support for additional service retrieval methods and enhanced cleanup workflow
- Wrote all new tests

## [0.2.0-beta] - Mar 23, 2024
- Added dedicated property drawer for the ServiceLocator

## [0.1.1-beta] - Mar 10, 2024
- Removed unrequired interfaces from the ServiceLocator

## [0.1.0-beta] - Mar 10, 2024
- Changed the GetService method to return a promise
- Added unit tests for the ServiceLocator

## [0.0.0-beta] - Feb 11, 2024
- First commit