using System;
using System.Collections.Generic;
using UnityEditor;

namespace Nonatomic.ServiceLocator.Settings
{
	/// <summary>
	///     Static utility class for managing Service Locator configuration.
	///     This approach avoids creating any asset files.
	/// </summary>
	public static class ServiceLocatorSettings
	{
		// Constants for the preprocessor directives
		public const string DEFINE_ENABLE_ASYNC = "ENABLE_SL_ASYNC";
		public const string DEFINE_ENABLE_UNITASK = "ENABLE_SL_UNITASK";
		public const string DEFINE_ENABLE_PROMISES = "ENABLE_SL_PROMISES";
		public const string DEFINE_ENABLE_COROUTINES = "ENABLE_SL_COROUTINES";
		public const string DEFINE_ENABLE_SCENE_TRACKING = "ENABLE_SL_SCENE_TRACKING";
		public const string DEFINE_ENABLE_LOGGING = "ENABLE_SL_LOGGING";

		// Old directives for backward compatibility
		public const string DEFINE_DISABLE_ASYNC = "DISABLE_SL_ASYNC";
		public const string DEFINE_DISABLE_PROMISES = "DISABLE_SL_PROMISES";
		public const string DEFINE_DISABLE_COROUTINES = "DISABLE_SL_COROUTINES";
		public const string DEFINE_DISABLE_SCENE_TRACKING = "DISABLE_SL_SCENE_TRACKING";
		public const string DEFINE_DISABLE_LOGGING = "DISABLE_SL_LOGGING";

		// EditorPrefs keys
		private const string PrefKeyPrefix = "Nonatomic.ServiceLocator.";
		private const string EnableAsyncKey = PrefKeyPrefix + "EnableAsync";
		private const string EnableUniTaskKey = PrefKeyPrefix + "EnableUniTask";
		private const string EnablePromisesKey = PrefKeyPrefix + "EnablePromises";
		private const string EnableCoroutinesKey = PrefKeyPrefix + "EnableCoroutines";
		private const string EnableSceneTrackingKey = PrefKeyPrefix + "EnableSceneTracking";
		private const string EnableLoggingKey = PrefKeyPrefix + "EnableLogging";

		// Default values - all features enabled by default
		private const bool DefaultEnabled = true;

		// Properties for each feature
		public static bool EnableAsyncServices
		{
			get => EditorPrefs.GetBool(EnableAsyncKey, DefaultEnabled);
			set
			{
				if (EditorPrefs.GetBool(EnableAsyncKey, DefaultEnabled) == value)
				{
					return;
				}

				// Can't have both Async and UniTask enabled
				if (value && EnableUniTaskServices)
				{
					EnableUniTaskServices = false;
				}

				EditorPrefs.SetBool(EnableAsyncKey, value);
				UpdateScriptingDefineSymbols();
			}
		}

		public static bool EnableUniTaskServices
		{
			get => EditorPrefs.GetBool(EnableUniTaskKey, false);
			set
			{
				if (EditorPrefs.GetBool(EnableUniTaskKey, false) == value)
				{
					return;
				}

				// Can't have both Async and UniTask enabled
				if (value && EnableAsyncServices)
				{
					EnableAsyncServices = false;
				}

				EditorPrefs.SetBool(EnableUniTaskKey, value);
				UpdateScriptingDefineSymbols();
			}
		}

		public static bool EnablePromiseServices
		{
			get => EditorPrefs.GetBool(EnablePromisesKey, DefaultEnabled);
			set
			{
				if (EditorPrefs.GetBool(EnablePromisesKey, DefaultEnabled) == value)
				{
					return;
				}

				EditorPrefs.SetBool(EnablePromisesKey, value);
				UpdateScriptingDefineSymbols();
			}
		}

		public static bool EnableCoroutineServices
		{
			get => EditorPrefs.GetBool(EnableCoroutinesKey, DefaultEnabled);
			set
			{
				if (EditorPrefs.GetBool(EnableCoroutinesKey, DefaultEnabled) == value)
				{
					return;
				}

				EditorPrefs.SetBool(EnableCoroutinesKey, value);
				UpdateScriptingDefineSymbols();
			}
		}

		public static bool EnableSceneTracking
		{
			get => EditorPrefs.GetBool(EnableSceneTrackingKey, DefaultEnabled);
			set
			{
				if (EditorPrefs.GetBool(EnableSceneTrackingKey, DefaultEnabled) == value)
				{
					return;
				}

				EditorPrefs.SetBool(EnableSceneTrackingKey, value);
				UpdateScriptingDefineSymbols();
			}
		}

		public static bool EnableLogging
		{
			get => EditorPrefs.GetBool(EnableLoggingKey, DefaultEnabled);
			set
			{
				if (EditorPrefs.GetBool(EnableLoggingKey, DefaultEnabled) == value)
				{
					return;
				}

				EditorPrefs.SetBool(EnableLoggingKey, value);
				UpdateScriptingDefineSymbols();
			}
		}

		/// <summary>
		///     Updates the scripting define symbols based on the current settings.
		/// </summary>
		public static void UpdateScriptingDefineSymbols()
		{
			// Get current symbols
			var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
			var definesList = new List<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

			// Remove old DISABLE_* directives (for backwards compatibility)
			definesList.Remove(DEFINE_DISABLE_ASYNC);
			definesList.Remove(DEFINE_DISABLE_PROMISES);
			definesList.Remove(DEFINE_DISABLE_COROUTINES);
			definesList.Remove(DEFINE_DISABLE_SCENE_TRACKING);
			definesList.Remove(DEFINE_DISABLE_LOGGING);

			// Update defines based on settings using the new ENABLE_* pattern
			UpdateDefine(definesList, DEFINE_ENABLE_ASYNC, EnableAsyncServices);
			UpdateDefine(definesList, DEFINE_ENABLE_UNITASK, EnableUniTaskServices);
			UpdateDefine(definesList, DEFINE_ENABLE_PROMISES, EnablePromiseServices);
			UpdateDefine(definesList, DEFINE_ENABLE_COROUTINES, EnableCoroutineServices);
			UpdateDefine(definesList, DEFINE_ENABLE_SCENE_TRACKING, EnableSceneTracking);
			UpdateDefine(definesList, DEFINE_ENABLE_LOGGING, EnableLogging);

			// Save the updated defines
			var newDefines = string.Join(";", definesList.ToArray());
			PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, newDefines);
		}

		/// <summary>
		///     Updates a specific define symbol in the list.
		/// </summary>
		private static void UpdateDefine(List<string> defines, string define, bool shouldExist)
		{
			var exists = defines.Contains(define);

			if (shouldExist && !exists)
			{
				defines.Add(define);
			}
			else if (!shouldExist && exists)
			{
				defines.Remove(define);
			}
		}

		/// <summary>
		///     Syncs the settings with the current project's scripting define symbols.
		/// </summary>
		public static void SyncFromProjectSettings()
		{
			// Get current symbols
			var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
			var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
			var definesList = new List<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

			// Update EditorPrefs based on the defines (without triggering UpdateScriptingDefineSymbols)
			// Handle both new ENABLE_* symbols and legacy DISABLE_* symbols for backward compatibility
			EditorPrefs.SetBool(EnableAsyncKey,
				definesList.Contains(DEFINE_ENABLE_ASYNC) || !definesList.Contains(DEFINE_DISABLE_ASYNC));

			EditorPrefs.SetBool(EnableUniTaskKey,
				definesList.Contains(DEFINE_ENABLE_UNITASK));

			EditorPrefs.SetBool(EnablePromisesKey,
				definesList.Contains(DEFINE_ENABLE_PROMISES) || !definesList.Contains(DEFINE_DISABLE_PROMISES));

			EditorPrefs.SetBool(EnableCoroutinesKey,
				definesList.Contains(DEFINE_ENABLE_COROUTINES) || !definesList.Contains(DEFINE_DISABLE_COROUTINES));

			EditorPrefs.SetBool(EnableSceneTrackingKey,
				definesList.Contains(DEFINE_ENABLE_SCENE_TRACKING) ||
				!definesList.Contains(DEFINE_DISABLE_SCENE_TRACKING));

			EditorPrefs.SetBool(EnableLoggingKey,
				definesList.Contains(DEFINE_ENABLE_LOGGING) || !definesList.Contains(DEFINE_DISABLE_LOGGING));

			// Update the script defines to the new format
			UpdateScriptingDefineSymbols();
		}

		/// <summary>
		///     Resets all settings to their default values.
		/// </summary>
		public static void ResetToDefaults()
		{
			EditorPrefs.SetBool(EnableAsyncKey, DefaultEnabled);
			EditorPrefs.SetBool(EnableUniTaskKey, false);
			EditorPrefs.SetBool(EnablePromisesKey, DefaultEnabled);
			EditorPrefs.SetBool(EnableCoroutinesKey, DefaultEnabled);
			EditorPrefs.SetBool(EnableSceneTrackingKey, DefaultEnabled);
			EditorPrefs.SetBool(EnableLoggingKey, DefaultEnabled);

			UpdateScriptingDefineSymbols();
		}
	}
}