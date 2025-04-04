# Change Log

## [0.10.0] - Apr 04, 2025
- Added service search filtering
- 
## [0.9.3] - Apr 04, 2025
- Added support for DontDestroyOnLoad
- Added scene type colour coding
- Added some additional styling

## [0.9.2] - Apr 03, 2025
- Expanded the IsServiceValid method to take a service reference which covers the case where a Service has been overwritten in the ServiceLocator leaving dangling references.

## [0.9.1] - Apr 03, 2025
- Fix for uss styling issue in toggle inputs
- Added Theme based styling

## [0.9.0] - Apr 03, 2025
- Added a settings tab to the Service Locator Window allowing users to disable features of the ServiceLocator. Disabled features are disabled by PreProcessor so the code is not included in builds.

## [0.8.13] - Apr 02, 2025
- added the IsServiceValid method to help validate the state of services
  - More importantly this will help with detecting invalid MonoBehaviour based services

## [0.8.12] - Apr 01, 2025
- For code clarity the BaseServiceLocator has been split into three partial classes to separate the 
concerns of the service locator by retrieval method i.e async, coroutine and promise

## [0.8.11] - Mar 24, 2025
- Code cleanup

## [0.8.10] - Mar 24, 2025
- Added NonSerialized attributes to all collections in the BaseServiceLocator to prevent Unity serializing any references

## [0.8.9] - Mar 24, 2025
- Removed unrequired logs
- 
## [0.8.8] - Mar 24, 2025
## [0.8.7] - Mar 24, 2025
## [0.8.6] - Mar 24, 2025
- regenerated the meta files for the icons

## [0.8.5] - Mar 24, 2025
## [0.8.4] - Mar 24, 2025
## [0.8.3] - Mar 24, 2025
## [0.8.2] - Mar 24, 2025
- Tweaked icon paths to correct for installation via package manager

## [0.8.1] - Mar 24, 2025
- Changed menu path to the Service Locator Window
- Updated the README

## [0.8.0] - Mar 23, 2025
- Added a Service Locator Window to track registered services
- Added a more robust cleanup workflow to ensure that all services are cleaned up when exiting play mode
- Added additional tests

## [0.7.1] - Mar 05, 2025
- Added support for combined cancellation tokens that ensures all tasks and promises are cancelled when the token is cancelled
- Added additional unit tests to ensure cancellation tokens are working as expected
- Added the WithCancellation method to the ServicePromise to allow for the cancellation of a promise
- Updated the Readme with additional information on cancellation tokens

## [0.7.0] - Mar 02, 2025
- Added support for Cancellation Tokens to the GetServiceAsync method
- Added support for RejectService method to allow for the rejection of a service promises and Tasks with propogated exceptions

## [0.6.1] - Feb 18, 2025
- Added syntactic sugar to allow the simple addition of additional services requests through GetServiceAsync without adding an s, sounds silly, but it gets annoying quick

## [0.6.0] - Nov 26, 2024
- BREAKING change. Previously all services were registered in the Awake method but must now be manually registered by calling the ServiceReady method. This is to indicate to
developers that service setup logic should be ran before the service is registered.

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