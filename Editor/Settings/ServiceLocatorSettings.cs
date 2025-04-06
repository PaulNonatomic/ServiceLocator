using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nonatomic.ServiceLocator.Settings
{
	/// <summary>
	///     Static utility class for managing Service Locator configuration.
	///     This approach avoids creating any asset files.
	/// </summary>
	public static class ServiceLocatorSettings
	{
		// Constants for the preprocessor directives
		public const string DisableAsync = "DISABLE_SL_ASYNC";
		public const string DisablePromises = "DISABLE_SL_PROMISES";
		public const string DisableCoroutines = "DISABLE_SL_COROUTINES";
		public const string DisableSceneTracking = "DISABLE_SL_SCENE_TRACKING";
		public const string DisableLogging = "DISABLE_SL_LOGGING";
		public const string DisableUniTask = "DISABLE_SL_UNITASK";
		public const string EnableUniTask = "ENABLE_UNITASK";

		// EditorPrefs keys
		private const string PrefKeyPrefix = "Nonatomic.ServiceLocator.";
		private const string EnableAsyncKey = PrefKeyPrefix + "EnableAsync";
		private const string EnablePromisesKey = PrefKeyPrefix + "EnablePromises";
		private const string EnableCoroutinesKey = PrefKeyPrefix + "EnableCoroutines";
		private const string EnableSceneTrackingKey = PrefKeyPrefix + "EnableSceneTracking";
		private const string EnableLoggingKey = PrefKeyPrefix + "EnableLogging";
		private const string EnableUniTaskKey = PrefKeyPrefix + "EnableUniTask";

		// Default values - all features enabled by default except UniTask
		private const bool DefaultEnabled = true;
		private const bool DefaultUniTaskEnabled = false;

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

				EditorPrefs.SetBool(EnableAsyncKey, value);
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

		public static bool EnableUniTaskServices
		{
			get => EditorPrefs.GetBool(EnableUniTaskKey, DefaultUniTaskEnabled);
			set
			{
				if (EditorPrefs.GetBool(EnableUniTaskKey, DefaultUniTaskEnabled) == value)
				{
					return;
				}

				EditorPrefs.SetBool(EnableUniTaskKey, value);
				UpdateScriptingDefineSymbols();
			}
		}

		/// <summary>
		///     Checks if UniTask package exists in the project
		/// </summary>
		public static bool CheckUniTaskExists()
		{
			return Directory.Exists("Packages/com.cysharp.unitask") ||
				   Directory.Exists("Assets/Plugins/UniTask");
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

			// Update defines based on settings
			UpdateDefine(definesList, DisableAsync, !EnableAsyncServices);
			UpdateDefine(definesList, DisablePromises, !EnablePromiseServices);
			UpdateDefine(definesList, DisableCoroutines, !EnableCoroutineServices);
			UpdateDefine(definesList, DisableSceneTracking, !EnableSceneTracking);
			UpdateDefine(definesList, DisableLogging, !EnableLogging);
			UpdateDefine(definesList, DisableUniTask, !EnableUniTaskServices);

			// Ensure ENABLE_UNITASK is present if UniTask is enabled
			if (EnableUniTaskServices)
			{
				var unitaskExists = CheckUniTaskExists();

				if (unitaskExists)
				{
					// Make sure ENABLE_UNITASK is defined
					if (!definesList.Contains(EnableUniTask))
					{
						definesList.Add(EnableUniTask);
					}
				}
				else
				{
					// UniTask package not found, show warning and disable the setting
					Debug.LogWarning(
						"UniTask package not found in project. Please install UniTask to use this feature.");
					EditorPrefs.SetBool(EnableUniTaskKey, false);

					// Make sure ENABLE_UNITASK is not defined
					if (definesList.Contains(EnableUniTask))
					{
						definesList.Remove(EnableUniTask);
					}
				}
			}

			// UniTask not enabled, so don't add ENABLE_UNITASK
			// We don't remove it though, as it might be used elsewhere
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
			EditorPrefs.SetBool(EnableAsyncKey, !definesList.Contains(DisableAsync));
			EditorPrefs.SetBool(EnablePromisesKey, !definesList.Contains(DisablePromises));
			EditorPrefs.SetBool(EnableCoroutinesKey, !definesList.Contains(DisableCoroutines));
			EditorPrefs.SetBool(EnableSceneTrackingKey, !definesList.Contains(DisableSceneTracking));
			EditorPrefs.SetBool(EnableLoggingKey, !definesList.Contains(DisableLogging));
			EditorPrefs.SetBool(EnableUniTaskKey,
				!definesList.Contains(DisableUniTask) && definesList.Contains(EnableUniTask));
		}

		/// <summary>
		///     Resets all settings to their default values.
		/// </summary>
		public static void ResetToDefaults()
		{
			EditorPrefs.SetBool(EnableAsyncKey, DefaultEnabled);
			EditorPrefs.SetBool(EnablePromisesKey, DefaultEnabled);
			EditorPrefs.SetBool(EnableCoroutinesKey, DefaultEnabled);
			EditorPrefs.SetBool(EnableSceneTrackingKey, DefaultEnabled);
			EditorPrefs.SetBool(EnableLoggingKey, DefaultEnabled);
			EditorPrefs.SetBool(EnableUniTaskKey, DefaultUniTaskEnabled);

			UpdateScriptingDefineSymbols();
		}
	}
}