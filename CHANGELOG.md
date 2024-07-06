# Change Log

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