using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.EditMode
{
	[TestFixture]
	public class ServiceLocatorAsyncExtensionsTests
	{
		[SetUp]
		public void Setup()
		{
			_serviceLocator = ScriptableObject.CreateInstance<TestServiceLocator>();
			_serviceLocator.ForceInitialize();
			LogAssert.ignoreFailingMessages = true; // Ignore Debug.LogError messages during tests
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_serviceLocator);
			LogAssert.ignoreFailingMessages = false;
		}

		private TestServiceLocator _serviceLocator;

		#if !DISABLE_SL_ASYNC
        [Test]
        public async Task WithErrorHandling_TaskSucceeds_CompletesNormally()
        {
            // Register a service so the task succeeds
            var service = new TestService();
            _serviceLocator.Register(service);

            // Act
            var handlerCalled = false;
            var exceptionThrown = false;

            try
            {
                await _serviceLocator.GetServiceAsync<TestService>()
                    .WithErrorHandling(
                        errorHandler: _ => handlerCalled = true,
                        rethrowException: false);
            }
            catch
            {
                exceptionThrown = true;
            }

            // Assert
            Assert.IsFalse(handlerCalled, "Error handler should not be called for successful tasks");
            Assert.IsFalse(exceptionThrown, "Exception should not be thrown for successful tasks");
        }

        [Test]
        public async Task WithErrorHandling_GenericTaskSucceeds_ReturnsExpectedValue()
        {
            // Register a service so the task succeeds
            var service = new TestService();
            _serviceLocator.Register(service);

            // Act
            var result = await _serviceLocator.GetServiceAsync<TestService>()
                .WithErrorHandling(
                    defaultValue: null,
                    errorHandler: _ => { },
                    rethrowException: false);

            // Assert
            Assert.AreEqual(service, result, "Should return the expected service");
        }

        [Test]
        public async Task WithErrorHandling_TaskFails_HandlesError()
        {
            // Don't register a service so the task fails

            // Act
            var handlerCalled = false;
            var exceptionThrown = false;

            try
            {
                await _serviceLocator.GetServiceAsync<TestService>()
                    .WithErrorHandling(
                        errorHandler: _ => handlerCalled = true,
                        rethrowException: false);
            }
            catch
            {
                exceptionThrown = true;
            }

            // Assert
            Assert.IsTrue(handlerCalled, "Error handler should be called for failed tasks");
            Assert.IsFalse(exceptionThrown, "Exception should not be thrown when rethrowException is false");
        }

        [Test]
        public async Task WithErrorHandling_GenericTaskFails_ReturnsDefaultValue()
        {
            // Don't register a service so the task fails
            var defaultService = new TestService();

            // Act
            var result = await _serviceLocator.GetServiceAsync<TestService>()
                .WithErrorHandling(
                    defaultValue: defaultService,
                    errorHandler: _ => { },
                    rethrowException: false);

            // Assert
            Assert.AreEqual(defaultService, result, "Should return the default value when task fails");
        }

        [Test]
        public void WithErrorHandling_TaskFails_WithRethrow_ThrowsException()
        {
            // Don't register a service so the task fails

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _serviceLocator.GetServiceAsync<TestService>()
                    .WithErrorHandling(
                        errorHandler: _ => { },
                        rethrowException: true);
            });
        }

        [Test]
        public void WithErrorHandling_GenericTaskFails_WithRethrow_ThrowsException()
        {
            // Don't register a service so the task fails

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _serviceLocator.GetServiceAsync<TestService>()
                    .WithErrorHandling(
                        defaultValue: null,
                        errorHandler: _ => { },
                        rethrowException: true);
            });
        }

        [Test]
        public async Task WithErrorHandling_TaskCancelled_HandlesGracefully()
        {
            // Setup a cancellation source
            var cts = new CancellationTokenSource();
            
            // Start a task that will be cancelled
            var task = Task.Delay(1000, cts.Token);
            cts.Cancel();
            
            // Act
            var handlerCalled = false;
            var exceptionThrown = false;
            
            try
            {
                await task.WithErrorHandling(
                    errorHandler: _ => handlerCalled = true,
                    rethrowException: false);
            }
            catch
            {
                exceptionThrown = true;
            }
            
            // Assert
            Assert.IsFalse(handlerCalled, "Error handler should not be called for cancelled tasks");
            Assert.IsFalse(exceptionThrown, "Exception should not be thrown for cancelled tasks");
        }
		#endif

		#if !DISABLE_SL_UNITASK && ENABLE_UNITASK
		// UniTask equivalent tests would go here
		// The structure would be similar to the Task tests above
		// but using UniTask instead of Task

		// Note: These tests would require the UniTask package to be installed
		// and would only be compiled when ENABLE_UNITASK is defined
		#endif
	}
}