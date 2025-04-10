using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.CoreTests
{
	[TestFixture]
	public class ServiceLocatorErrorHandlingTests
	{
		[SetUp]
		public void Setup()
		{
			_serviceLocator = ScriptableObject.CreateInstance<ServiceLocator>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_serviceLocator);
		}

		private ServiceLocator _serviceLocator;

		#if !DISABLE_SL_ASYNC
		[UnityTest]
		public IEnumerator WithErrorHandling_BasicTask_HandlesExceptionGracefully()
		{
			// Setup
			var errorHandlerCalled = false;
			Exception caughtException = null;

			// Expect error log message
			LogAssert.Expect(LogType.Error, new Regex("\\[Async Error\\].*"));

			// Act
			var task = Task.Run((Action)(() => { throw new InvalidOperationException("Test exception"); }));

			errorHandlerCalled = false;
			task.WithErrorHandling(ex =>
			{
				errorHandlerCalled = true;
				caughtException = ex;
			});

			// Wait for task to complete
			yield return new WaitForSeconds(0.1f);

			// Assert
			Assert.IsTrue(errorHandlerCalled, "Error handler should be called");
			Assert.IsInstanceOf<InvalidOperationException>(caughtException, "Exception should be of correct type");
			Assert.AreEqual("Test exception", caughtException.Message, "Exception message should be preserved");
		}

		[UnityTest]
		public IEnumerator WithErrorHandling_GenericTask_ReturnsDefaultValue()
		{
			// Setup
			var defaultValue = "Default Value";
			string result = null;

			// Expect error log message
			LogAssert.Expect(LogType.Error, new Regex("\\[Async Error\\].*"));

			// Act
			Func<string> stringFunc = () => { throw new InvalidOperationException("Test exception"); };
			var task = Task.Run(stringFunc);

			var handledTask = task.WithErrorHandling(defaultValue);

			// Wait for task to complete
			yield return new WaitUntil(() => handledTask.IsCompleted);

			// Get result
			result = handledTask.Result;

			// Assert
			Assert.AreEqual(defaultValue, result, "Task should return the default value on error");
		}

		[UnityTest]
		public IEnumerator WithErrorHandling_CancelledTask_HandlesGracefully()
		{
			// Setup
			var cts = new CancellationTokenSource();
			var defaultValue = "Default Value";
			string result = null;
			var errorHandlerCalled = false;

			// Act
			var task = Task.Run(() =>
			{
				cts.Token.ThrowIfCancellationRequested();
				return "Success Value";
			}, cts.Token);

			// Cancel the task
			cts.Cancel();

			var handledTask = task.WithErrorHandling(
				defaultValue,
				ex => errorHandlerCalled = true
			);

			// Wait for task to complete
			yield return new WaitUntil(() => handledTask.IsCompleted);

			// Get result
			result = handledTask.Result;

			// Assert
			Assert.AreEqual(defaultValue, result, "Task should return the default value on cancellation");
			Assert.IsFalse(errorHandlerCalled, "Error handler should not be called for cancellations");
		}

		[UnityTest]
		public IEnumerator WithErrorHandling_RethrowException_ThrowsOriginalException()
		{
			// Setup
			var expectedException = new InvalidOperationException("Test exception");
			Exception caughtException = null;

			// Expect error log message
			LogAssert.Expect(LogType.Error, new Regex("\\[Async Error\\].*"));

			// Act
			var task = Task.Run((Action)(() => { throw expectedException; }));

			var handledTask = task.WithErrorHandling(
				rethrowException: true,
				errorHandler: ex =>
				{
					/* Still called even with rethrow */
				}
			);

			// Wait for task to complete
			yield return new WaitUntil(() => handledTask.IsCompleted);

			// Try to await the task to see if it throws
			var exceptionThrown = false;
			try
			{
				// Since we can't directly use await in IEnumerator methods,
				// we'll check if the task has an exception
				if (handledTask.IsFaulted && handledTask.Exception != null)
				{
					caughtException = handledTask.Exception.InnerException;
					exceptionThrown = true;
				}
			}
			catch (Exception ex)
			{
				caughtException = ex;
				exceptionThrown = true;
			}

			// Assert
			Assert.IsTrue(exceptionThrown, "Exception should be detected");
			Assert.IsNotNull(caughtException, "Exception should be captured");
			Assert.IsInstanceOf<InvalidOperationException>(caughtException,
				"The original exception should be preserved");
			Assert.AreEqual(expectedException.Message, caughtException.Message, "The exception message should match");
		}

		[UnityTest]
		public IEnumerator WithErrorHandling_WorksWithServiceLocatorAsync()
		{
			// Setup
			var errorHandlerCalled = false;
			Exception caughtException = null;
			ServiceLocatorTestUtils.TestService result = null;

			// Expect error log message
			LogAssert.Expect(LogType.Error, new Regex("\\[Async Error\\].*"));

			// Act - Request a service that doesn't exist
			var task = _serviceLocator
				.GetServiceAsync<ServiceLocatorTestUtils.TestService>()
				.WithErrorHandling(
					null,
					ex =>
					{
						errorHandlerCalled = true;
						caughtException = ex;
					}
				);

			// Reject the service to trigger an error
			_serviceLocator.RejectService<ServiceLocatorTestUtils.TestService>(
				new InvalidOperationException("Service initialization failed"));

			// Wait for task to complete
			yield return new WaitUntil(() => task.IsCompleted);

			// Get result
			result = task.Result;

			// Assert
			Assert.IsNull(result, "Result should be null on error");
			Assert.IsTrue(errorHandlerCalled, "Error handler should be called");
			Assert.IsInstanceOf<InvalidOperationException>(caughtException, "Exception should be of correct type");
		}

		[UnityTest]
		public IEnumerator WithErrorHandling_WorksWithFluentAPI()
		{
			// Setup
			var errorHandlerCalled = false;
			var defaultTuple = (null as ServiceLocatorTestUtils.TestService,
				null as ServiceLocatorTestUtils.AnotherTestService);
			var result = defaultTuple;

			// Expect error log message
			LogAssert.Expect(LogType.Error, new Regex("\\[Async Error\\].*"));

			// Create the services first (to avoid timing issues)
			_serviceLocator.Register(new ServiceLocatorTestUtils.AnotherTestService());

			// Act - Request multiple services using fluent API
			var task = _serviceLocator
				.GetAsync<ServiceLocatorTestUtils.TestService>()
				.AndAsync<ServiceLocatorTestUtils.AnotherTestService>()
				.WithCancellation()
				.WithErrorHandling(
					defaultTuple,
					ex => { errorHandlerCalled = true; }
				);

			// Reject the service to trigger an error with a specific exception
			_serviceLocator.RejectService<ServiceLocatorTestUtils.TestService>(
				new InvalidOperationException("Service initialization failed"));

			// Wait for task to complete with a timeout
			var startTime = Time.time;
			var timeout = 5.0f; // 5 second timeout
			yield return new WaitUntil(() => task.IsCompleted || Time.time - startTime > timeout);

			// If we timed out, fail the test
			Assert.IsTrue(task.IsCompleted, "Task did not complete within timeout period");

			if (task.IsCompleted)
			{
				// Get result
				result = task.Result;

				// Assert
				Assert.AreEqual(defaultTuple, result, "Result should be the default tuple on error");
				Assert.IsTrue(errorHandlerCalled, "Error handler should be called");
				Assert.IsNull(result.Item1, "First item should be null");
				Assert.IsNull(result.Item2, "Second item should be null");
			}
		}

		[UnityTest]
		public IEnumerator WithErrorHandling_SuccessfulTask_ReturnsOriginalResult()
		{
			// Setup
			var expectedValue = "Success Value";
			var defaultValue = "Default Value";
			string result = null;
			var errorHandlerCalled = false;

			// Act
			var task = Task.Run(() => expectedValue);

			var handledTask = task.WithErrorHandling(
				defaultValue,
				ex => errorHandlerCalled = true
			);

			// Wait for task to complete
			yield return new WaitUntil(() => handledTask.IsCompleted);

			// Get result
			result = handledTask.Result;

			// Assert
			Assert.AreEqual(expectedValue, result, "Task should return the original value on success");
			Assert.IsFalse(errorHandlerCalled, "Error handler should not be called for successful tasks");
		}

		[UnityTest]
		public IEnumerator WithErrorHandling_ServiceLocatorSuccessfulTask_ReturnsOriginalResult()
		{
			// Setup
			var expectedService = new ServiceLocatorTestUtils.TestService();
			_serviceLocator.Register(expectedService);

			ServiceLocatorTestUtils.TestService result = null;
			var errorHandlerCalled = false;

			// Act
			var task = _serviceLocator
				.GetServiceAsync<ServiceLocatorTestUtils.TestService>()
				.WithErrorHandling(
					null,
					ex => errorHandlerCalled = true
				);

			// Wait for task to complete
			yield return new WaitUntil(() => task.IsCompleted);

			// Get result
			result = task.Result;

			// Assert
			Assert.AreEqual(expectedService, result, "Task should return the original service on success");
			Assert.IsFalse(errorHandlerCalled, "Error handler should not be called for successful tasks");
		}
		#endif
	}
}