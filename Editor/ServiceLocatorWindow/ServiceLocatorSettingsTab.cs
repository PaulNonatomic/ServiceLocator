using System;
using Nonatomic.ServiceLocator.Settings;
using UnityEditor;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
	/// <summary>
	///     Represents the Settings tab in the Service Locator Window.
	///     Uses the simplified static ServiceLocatorSettings.
	/// </summary>
	public class ServiceLocatorSettingsTab : VisualElement
	{
		private readonly Toggle _asyncToggle;
		private readonly Toggle _coroutinesToggle;
		private readonly Toggle _loggingToggle;
		private readonly Toggle _promisesToggle;
		private readonly Toggle _sceneTrackingToggle;

		public ServiceLocatorSettingsTab()
		{
			// Create the root container
			AddToClassList("settings-tab");

			// Add header
			var headerLabel = new Label("Service Locator Configuration");
			headerLabel.AddToClassList("settings-header");
			Add(headerLabel);

			// Add description
			var descriptionLabel = new Label(
				"Configure which features of the Service Locator are enabled. " +
				"Disabling features will add the corresponding preprocessor directive to exclude the code from compilation.");
			descriptionLabel.AddToClassList("settings-description");
			Add(descriptionLabel);

			// Add a container for the toggles
			var togglesContainer = new VisualElement();
			togglesContainer.AddToClassList("toggles-container");
			Add(togglesContainer);

			// Add toggles for each feature
			_asyncToggle = AddFeatureToggle(togglesContainer, "Enable Async Services",
				"Enables the GetServiceAsync<T>() methods for Task-based service retrieval.",
				ServiceLocatorSettings.EnableAsyncServices,
				value => ServiceLocatorSettings.EnableAsyncServices = value);

			_promisesToggle = AddFeatureToggle(togglesContainer, "Enable Promise Services",
				"Enables the GetService<T>() methods that return IServicePromise<T>.",
				ServiceLocatorSettings.EnablePromiseServices,
				value => ServiceLocatorSettings.EnablePromiseServices = value);

			_coroutinesToggle = AddFeatureToggle(togglesContainer, "Enable Coroutine Services",
				"Enables the GetServiceCoroutine<T>() methods for Unity coroutine-based service retrieval.",
				ServiceLocatorSettings.EnableCoroutineServices,
				value => ServiceLocatorSettings.EnableCoroutineServices = value);

			_sceneTrackingToggle = AddFeatureToggle(togglesContainer, "Enable Scene Tracking",
				"Enables automatic tracking and cleanup of services when scenes are unloaded.",
				ServiceLocatorSettings.EnableSceneTracking,
				value => ServiceLocatorSettings.EnableSceneTracking = value);

			_loggingToggle = AddFeatureToggle(togglesContainer, "Enable Logging",
				"Enables debug logging for service registration, retrieval, and other operations.",
				ServiceLocatorSettings.EnableLogging,
				value => ServiceLocatorSettings.EnableLogging = value);

			// Add buttons container
			var buttonsContainer = new VisualElement();
			buttonsContainer.AddToClassList("buttons-container");
			Add(buttonsContainer);

			// Add reset button
			var resetButton = new Button(ResetToDefaults) { text = "Reset to Defaults" };
			resetButton.AddToClassList("reset-button");
			buttonsContainer.Add(resetButton);

			// Add sync button
			var syncButton = new Button(SyncFromProjectSettings) { text = "Sync from Project Settings" };
			syncButton.AddToClassList("sync-button");
			buttonsContainer.Add(syncButton);
		}

		/// <summary>
		///     Adds a feature toggle with a label and tooltip.
		/// </summary>
		private Toggle AddFeatureToggle(VisualElement container, string label, string tooltip,
			bool initialValue, Action<bool> setter)
		{
			var toggleContainer = new VisualElement();
			toggleContainer.AddToClassList("toggle-row");

			var toggle = new Toggle { value = initialValue, label = label };
			toggle.tooltip = tooltip;
			toggle.RegisterValueChangedCallback(evt => setter(evt.newValue));

			toggleContainer.Add(toggle);
			container.Add(toggleContainer);

			return toggle;
		}

		/// <summary>
		///     Resets all settings to their default values (all features enabled).
		/// </summary>
		private void ResetToDefaults()
		{
			if (EditorUtility.DisplayDialog("Reset Settings",
					"Are you sure you want to reset all Service Locator settings to their defaults? " +
					"This will enable all features.", "Reset", "Cancel"))
			{
				ServiceLocatorSettings.ResetToDefaults();

				// Update the UI
				_asyncToggle.value = ServiceLocatorSettings.EnableAsyncServices;
				_promisesToggle.value = ServiceLocatorSettings.EnablePromiseServices;
				_coroutinesToggle.value = ServiceLocatorSettings.EnableCoroutineServices;
				_sceneTrackingToggle.value = ServiceLocatorSettings.EnableSceneTracking;
				_loggingToggle.value = ServiceLocatorSettings.EnableLogging;

				EditorUtility.DisplayDialog("Settings Reset", "All settings have been reset to their defaults.", "OK");
			}
		}

		/// <summary>
		///     Syncs the settings with the current project's scripting define symbols.
		/// </summary>
		private void SyncFromProjectSettings()
		{
			ServiceLocatorSettings.SyncFromProjectSettings();

			// Update the UI
			_asyncToggle.value = ServiceLocatorSettings.EnableAsyncServices;
			_promisesToggle.value = ServiceLocatorSettings.EnablePromiseServices;
			_coroutinesToggle.value = ServiceLocatorSettings.EnableCoroutineServices;
			_sceneTrackingToggle.value = ServiceLocatorSettings.EnableSceneTracking;
			_loggingToggle.value = ServiceLocatorSettings.EnableLogging;

			EditorUtility.DisplayDialog("Settings Synced", "Settings have been synced from project settings.", "OK");
		}
	}
}