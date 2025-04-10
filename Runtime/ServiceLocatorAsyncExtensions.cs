using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace Nonatomic.ServiceLocator
{
	/// <summary>
	///     Extension methods for error handling with async operations in the ServiceLocator.
	///     These extensions provide a consistent way to handle errors in async calls without
	///     repetitive try/catch blocks.
	/// </summary>
	public static class ServiceLocatorAsyncExtensions
	{
		#if !DISABLE_SL_ASYNC
		/// <summary>
		///     Provides standardized error handling for Task operations.
		/// </summary>
		/// <param name="task">The task to handle errors for.</param>
		/// <param name="errorHandler">Optional custom error handler.</param>
		/// <param name="rethrowException">Whether to rethrow the exception after handling.</param>
		/// <param name="callerMemberName">Automatically populated with the calling method name.</param>
		/// <param name="callerFilePath">Automatically populated with the source file path.</param>
		/// <param name="callerLineNumber">Automatically populated with the line number.</param>
		/// <returns>A task that completes when the original task completes or fails.</returns>
		public static async Task WithErrorHandling(
			this Task task,
			Action<Exception> errorHandler = null,
			bool rethrowException = false,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
		{
			try
			{
				await task;
			}
			catch (OperationCanceledException)
			{
				// Handle cancellation silently
			}
			catch (Exception ex)
			{
				var errorLocation = $"{callerMemberName} ({callerFilePath}:{callerLineNumber})";
				Debug.LogError($"[Async Error] in {errorLocation}\n{ex}");

				errorHandler?.Invoke(ex);

				if (rethrowException)
				{
					throw;
				}
			}
		}

		/// <summary>
		///     Provides standardized error handling for Task<T> operations.
		/// </summary>
		/// <typeparam name="T">The type of result the task returns.</typeparam>
		/// <param name="task">The task to handle errors for.</param>
		/// <param name="defaultValue">The default value to return if the task fails.</param>
		/// <param name="errorHandler">Optional custom error handler.</param>
		/// <param name="rethrowException">Whether to rethrow the exception after handling.</param>
		/// <param name="callerMemberName">Automatically populated with the calling method name.</param>
		/// <param name="callerFilePath">Automatically populated with the source file path.</param>
		/// <param name="callerLineNumber">Automatically populated with the line number.</param>
		/// <returns>The task result or the default value if the task fails.</returns>
		public static async Task<T> WithErrorHandling<T>(
			this Task<T> task,
			T defaultValue = default,
			Action<Exception> errorHandler = null,
			bool rethrowException = false,
			[CallerMemberName] string callerMemberName = "",
			[CallerFilePath] string callerFilePath = "",
			[CallerLineNumber] int callerLineNumber = 0)
		{
			try
			{
				return await task;
			}
			catch (OperationCanceledException)
			{
				// Handle cancellation silently
				return defaultValue;
			}
			catch (Exception ex)
			{
				var errorLocation = $"{callerMemberName} ({callerFilePath}:{callerLineNumber})";
				Debug.LogError($"[Async Error] in {errorLocation}\n{ex}");

				errorHandler?.Invoke(ex);

				if (rethrowException)
				{
					throw;
				}

				return defaultValue;
			}
		}
		#endif
	}
}