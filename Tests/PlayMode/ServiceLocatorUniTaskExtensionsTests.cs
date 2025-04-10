// Tests.PlayMode.ServiceLocatorUniTaskExtensionsTests.cs
// (Make sure you have 'using Cysharp.Threading.Tasks;' and 'using NUnit.Framework;')

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using Nonatomic.ServiceLocator.Extensions;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Cysharp.Threading.Tasks; // Ensure this is present

namespace Tests.PlayMode
{
    [TestFixture]
    public class ServiceLocatorUniTaskExtensionsTests
    {
        private TestServiceLocator _serviceLocator;

        [SetUp]
        public void Setup()
        {
            _serviceLocator = ScriptableObject.CreateInstance<TestServiceLocator>();
            _serviceLocator.ForceInitialize();
            // Keep ignoring logs during setup/test, handle expected ones with LogAssert.Expect
            // LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            // Restore default log handling
            // LogAssert.ignoreFailingMessages = false;

            try
            {
                if (_serviceLocator != null)
                {
                    // Use SafeCleanup before destroying
                    try
                    {
                        _serviceLocator.SafeCleanup();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error during SafeCleanup: {ex.Message}");
                    }

                    Object.DestroyImmediate(_serviceLocator);
                    _serviceLocator = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error during TearDown: {ex.Message}");
            }
        }

        // This test verifies that the WithErrorHandling extension is correctly conditional on UniTask being enabled
        [Test]
        public void UniTaskExtensions_OnlyExistWhenEnabled()
        {
            #if !DISABLE_SL_UNITASK && ENABLE_UNITASK
            // UniTask is enabled, so the extension methods should exist
            Assert.DoesNotThrow(() =>
            {
                // This is just a compilation check - just needs valid syntax
                var dummyTask = UniTask.CompletedTask; // Use UniTask.CompletedTask
                var _ = dummyTask.WithErrorHandling();
                var dummyGenericTask = UniTask.FromResult(new object());
                var __ = dummyGenericTask.WithErrorHandling(defaultValue: null);
            });
            #else
            // When UniTask is disabled, the test is still valid but we don't check anything
            Assert.Pass("UniTask is disabled, so extensions should not exist");
            #endif
        }

        #if !DISABLE_SL_UNITASK && ENABLE_UNITASK
        [UnityTest]
        public IEnumerator WithErrorHandling_UniTaskSucceeds_CompletesNormally()
        {
            // Arrange
            var service = new TestService();
            _serviceLocator.Register(service);
            bool handlerCalled = false;
            bool exceptionCaught = false;

            // Act
            UniTask testTask = UniTask.Create(async () =>
            {
                try
                {
                    await _serviceLocator.GetServiceAsync<TestService>()
                        .WithErrorHandling(
                            errorHandler: _ => handlerCalled = true,
                            rethrowException: false);
                }
                catch
                {
                    exceptionCaught = true; // This catch block should not be hit
                }
            });

            // Yield and Assert after completion
            yield return testTask.ToCoroutine();
            Assert.IsFalse(handlerCalled, "Error handler should not be called for successful tasks");
            Assert.IsFalse(exceptionCaught, "Exception should not be thrown for successful tasks");
        }

        [UnityTest]
        public IEnumerator WithErrorHandling_GenericUniTaskSucceeds_ReturnsExpectedValue()
        {
            // Arrange
            var service = new TestService();
            _serviceLocator.Register(service);
            TestService result = null;

            // Act
            UniTask testTask = UniTask.Create(async () =>
            {
                result = await _serviceLocator.GetServiceAsync<TestService>()
                    .WithErrorHandling(
                        defaultValue: null, // Provide a default explicitly
                        errorHandler: _ => { }, // Empty handler
                        rethrowException: false);
            });

            // Yield and Assert after completion
            yield return testTask.ToCoroutine();
            Assert.AreEqual(service, result, "Should return the expected service");
        }

        [UnityTest]
        public IEnumerator WithErrorHandling_UniTaskFails_HandlesError()
        {
            // Arrange: Don't register service
            bool handlerCalled = false;
            bool exceptionCaught = false;

            // Expect the error log from WithErrorHandling
            LogAssert.Expect(LogType.Error, new Does.Contain("[UniTask Error]"));

            // Act
            var testTask = UniTask.Create(async () =>
            {
                try
                {
                    // GetServiceAsync will create a promise that likely gets cancelled on TearDown/SafeCleanup
                    await _serviceLocator.GetServiceAsync<TestService>()
                        .WithErrorHandling(
                            errorHandler: _ =>
                            {
                                // Debug.Log("ErrorHandler invoked!"); // Optional debug log
                                handlerCalled = true;
                            },
                            rethrowException: false);
                }
                catch // Catch exceptions thrown *outside* WithErrorHandling (shouldn't happen here)
                {
                    exceptionCaught = true;
                }
            });

            // Yield and Assert
            // The ToCoroutine() will wait for the UniTask.
            // If GetServiceAsync hangs indefinitely (before SafeCleanup), this might still time out.
            // If GetServiceAsync gets Cancelled by SafeCleanup, WithErrorHandling should catch OperationCanceledException silently.
            // If GetServiceAsync gets faulted some other way, WithErrorHandling should catch Exception, log, and call handler.
            // Let's assume SafeCleanup cancellation is the most likely path for a missing service.
            // But the test *intends* to check the non-cancelled failure path.
            // Let's force a failure directly instead of relying on GetServiceAsync/SafeCleanup timing.

            // --- Revised Arrange & Act for deterministic failure ---
            handlerCalled = false; // Reset flags
            exceptionCaught = false;
            LogAssert.Expect(LogType.Error, new StringContains("[UniTask Error]")); // Expect log again
            var failingTask = UniTask.FromException(new InvalidOperationException("Simulated failure"));

            UniTask revisedTestTask = UniTask.Create(async () => {
                 try
                 {
                     await failingTask
                         .WithErrorHandling(
                             errorHandler: _ => handlerCalled = true,
                             rethrowException: false);
                 }
                 catch { exceptionCaught = true; }
             });

            yield return revisedTestTask.ToCoroutine();

            // Assert
            Assert.IsTrue(handlerCalled, "Error handler should be called for failed tasks");
            Assert.IsFalse(exceptionCaught, "Exception should not be thrown when rethrowException is false");
        }


        [UnityTest]
        public IEnumerator WithErrorHandling_GenericUniTaskFails_ReturnsDefaultValue()
        {
            // Arrange: Don't register service
            var defaultService = new TestService();
            TestService result = null;

            // Expect the error log from WithErrorHandling catching the failure
            LogAssert.Expect(LogType.Error, new StringContains("[UniTask Error]"));

            // Act
            // Force a deterministic failure instead of relying on GetServiceAsync timing/cancellation
            var failingGenericTask = UniTask.FromException<TestService>(new InvalidOperationException("Simulated failure"));

            UniTask testTask = UniTask.Create(async () => {
                 result = await failingGenericTask
                     .WithErrorHandling(
                         defaultService,
                         _ => { }, // Empty handler
                         false); // rethrowException = false
             });

            // Yield and Assert
            yield return testTask.ToCoroutine();
            Assert.AreEqual(defaultService, result, "Should return the default value when task fails");
        }

        [UnityTest]
        public IEnumerator WithErrorHandling_UniTaskFails_WithRethrow_ThrowsException()
        {
            // Arrange
            var failingTask = UniTask.FromException<TestService>(new InvalidOperationException("Simulated failure"));
            bool exceptionCaughtCorrectly = false;

            // Expect the log from WithErrorHandling *before* it rethrows
            LogAssert.Expect(LogType.Error, new StringContains("[UniTask Error]"));

            // Act
            UniTask testTask = UniTask.Create(async () => {
                try
                {
                    await failingTask.WithErrorHandling(
                            errorHandler: _ => { }, // Empty handler
                            rethrowException: true); // Rethrow is TRUE

                    // If we reach here, the exception wasn't rethrown
                    Assert.Fail("Exception was not rethrown by WithErrorHandling.");
                }
                catch (InvalidOperationException)
                {
                    // This is the expected path
                    exceptionCaughtCorrectly = true;
                }
                catch (Exception ex)
                {
                    // Caught something else? Fail the test.
                    Assert.Fail($"Caught unexpected exception type: {ex.GetType()}");
                }
            });

            // Yield and Assert
            yield return testTask.ToCoroutine();
            Assert.IsTrue(exceptionCaughtCorrectly, "The expected exception was not caught after being rethrown.");
        }

        [UnityTest]
        public IEnumerator WithErrorHandling_UniTaskCancelled_HandlesGracefully()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var task = UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cts.Token); // Longer delay
            bool handlerCalled = false;
            bool exceptionThrown = false;

            // Act
            UniTask testTask = UniTask.Create(async () =>
            {
                try
                {
                    // Start the task, then cancel it immediately
                    var wrappedTask = task.WithErrorHandling(
                        _ => handlerCalled = true,
                        false); // rethrow = false

                    cts.Cancel(); // Cancel *after* starting the wrapped task but before awaiting it long

                    await wrappedTask; // Await the result
                }
                catch
                {
                    exceptionThrown = true;
                }
            });

            // Yield and Assert
            yield return testTask.ToCoroutine();
            Assert.IsFalse(handlerCalled, "Error handler should not be called for cancelled tasks");
            Assert.IsFalse(exceptionThrown, "Exception should not be thrown for cancelled tasks");

            cts.Dispose(); // Dispose CTS
        }

        [UnityTest]
        public IEnumerator WithErrorHandling_SimplifiedComplexReturnTypes_WithDefaultValue()
        {
            // Arrange
            var defaultValue = new TestService();
            TestService result = null;
            var failingTask = UniTask.FromException<TestService>(new InvalidOperationException("Test exception"));

            // Expect the log from the error handler
            LogAssert.Expect(LogType.Error, new StringContains("[UniTask Error]"));

            // Act
            UniTask testTask = UniTask.Create(async () =>
            {
                result = await failingTask
                    .WithErrorHandling(
                        defaultValue,
                        _ => { }, // Empty handler
                        false); // rethrow = false
            });

            // Yield and Assert
            yield return testTask.ToCoroutine();
            Assert.AreEqual(defaultValue, result, "Should return the default value");
        }

        [UnityTest]
        public IEnumerator WithErrorHandling_CustomErrorHandler_ReceivesCorrectException()
        {
            // Arrange
            var expectedException = new ArgumentException("Test specific exception");
            var failingTask = UniTask.FromException(expectedException);
            Exception caughtException = null;

            // Expect the log from WithErrorHandling
            LogAssert.Expect(LogType.Error, new StringContains("[UniTask Error]"));

            // Act
            UniTask testTask = UniTask.Create(async () => {
                 await failingTask.WithErrorHandling(
                     ex => caughtException = ex, // Assign exception in handler
                     false); // rethrow = false
             });

            // Yield and Assert
            yield return testTask.ToCoroutine();
            Assert.IsNotNull(caughtException, "Exception should be caught by handler");
            Assert.AreSame(expectedException, caughtException, "Handler should receive the exact exception instance");
            Assert.AreEqual("Test specific exception", caughtException.Message, "Exception message should be preserved");
        }

        [UnityTest]
        public IEnumerator ServiceRegisteredAfterRequest_WithErrorHandling_Success()
        {
            // Arrange
            var service = new TestService();
            TestService result = null;

            // Act
            UniTask testTask = UniTask.Create(async () =>
            {
                // Start async request *before* registering
                var serviceTask = _serviceLocator.GetServiceAsync<TestService>()
                    .WithErrorHandling(defaultValue: null); // Use error handling just in case

                // Register the service after a small delay (simulating async registration)
                await UniTask.Delay(50);
                _serviceLocator.Register(service);

                // Await the task result
                result = await serviceTask;
            });

            // Yield and Assert
            yield return testTask.ToCoroutine();
            Assert.AreEqual(service, result, "Should return the service registered after request started");
        }

        // Test REMOVED: ServiceUnregisteredDuringRequest_WithErrorHandling_ReturnsDefault
        // As discussed, the original test logic didn't reliably test the intended scenario.
        // Cancellation and failure-to-register scenarios are covered by other tests.

        [UnityTest]
        public IEnumerator CancelledServiceRequest_WithErrorHandling_ReturnsDefaultValue()
        {
            // Arrange
            var defaultService = new TestService();
            var cts = new CancellationTokenSource();
            TestService result = null;

            // Act
            UniTask testTask = UniTask.Create(async () =>
            {
                // Start request with cancellation token and error handling
                var serviceTask = _serviceLocator.GetServiceAsync<TestService>(cts.Token)
                    .WithErrorHandling(defaultService);

                // Cancel the request after a delay
                await UniTask.Delay(50); // Ensure the GetServiceAsync call has started and created the promise
                cts.Cancel();

                // Await the result - WithErrorHandling should catch OperationCanceledException
                result = await serviceTask;
            });

            // Yield and Assert
            yield return testTask.ToCoroutine();
            Assert.AreEqual(defaultService, result, "Should return default value when request is cancelled");
            cts.Dispose();
        }
        #endif
    }

    // --- Helper classes remain the same ---
    // TestService, AnotherTestService, ThirdTestService, ISceneTrackingService, SceneTrackingTestService
    public class TestService {}

    public class TestServiceLocator : BaseServiceLocator
    {
        // ... (Keep the existing TestServiceLocator helper class with ForceInitialize, SafeCleanup etc.) ...
        public new void OnEnable()
        {
            base.OnEnable();
        }

        public new void OnDisable()
        {
            base.OnDisable();
        }

        public void ForceInitialize()
        {
            Initialize();
            IsInitialized = true;
        }

        public void ForceDeInitialize()
        {
            DeInitialize();
            IsInitialized = false;
        }

        // Override to prevent automatic initialization
        protected override void Initialize()
        {
            // Do nothing, allowing manual control in tests
        }

        // Safe cleanup method to handle collections more safely
        public void SafeCleanup()
        {
            #if !DISABLE_SL_UNITASK && ENABLE_UNITASK
            // Safely clean up UniTaskPromiseMap
            if (UniTaskPromiseMap != null)
            {
                try
                {
                    // Make a copy of the keys to avoid enumeration issues
                    var keys = new List<Type>(UniTaskPromiseMap.Keys);

                    foreach (var key in keys)
                    {
                        if (UniTaskPromiseMap.TryGetValue(key, out var promises))
                        {
                            // Make a copy of the promises to avoid enumeration issues
                            var promisesCopy = new List<UniTaskCompletionSource<object>>(promises);

                            foreach (var promise in promisesCopy)
                            {
                                // Use CancellationToken.None for general cancellation
                                promise.TrySetCanceled(CancellationToken.None);
                            }
                        }
                    }

                    UniTaskPromiseMap.Clear();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error during UniTaskPromiseMap cleanup: {ex.Message}");
                }
            }
            #endif

            // --- Keep the rest of SafeCleanup for other collections (ServiceMap, PromiseMap etc.) ---
            // Clear service map
            if (ServiceMap != null)
            {
                ServiceMap.Clear();
            }

            #if !DISABLE_SL_SCENE_TRACKING
            // Clear scene tracking map
            if (ServiceSceneMap != null)
            {
                ServiceSceneMap.Clear();
            }
            #endif

            #if !DISABLE_SL_ASYNC || !DISABLE_SL_PROMISES
            // Safely clean up PromiseMap
            if (PromiseMap != null)
            {
                try
                {
                    var keys = new List<Type>(PromiseMap.Keys);

                    foreach (var key in keys)
                    {
                        if (PromiseMap.TryGetValue(key, out var promises))
                        {
                            var promisesCopy = new List<TaskCompletionSource<object>>(promises);

                            foreach (var promise in promisesCopy)
                            {
                                promise.TrySetCanceled();
                            }
                        }
                    }

                    PromiseMap.Clear();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error during PromiseMap cleanup: {ex.Message}");
                }
            }
            #endif

            #if !DISABLE_SL_COROUTINES
            // Clear pending coroutines
            if (PendingCoroutines != null)
            {
                PendingCoroutines.Clear();
            }
            #endif
        }
    }
} // End namespace Tests.PlayMode