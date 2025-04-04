using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.ServiceLocator.Utils;
using UnityEditor;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
	public class LocatorItem : VisualElement
	{
		private readonly ServiceLocator _locator;
		private readonly VisualElement _servicesContainer;

		public LocatorItem(ServiceLocator locator)
		{
			_locator = locator;
			AddToClassList("locator-item");

			var headerLabel = new Label(locator.name);
			headerLabel.AddToClassList("locator-header");
			Add(headerLabel);

			_servicesContainer = new();
			_servicesContainer.AddToClassList("services-container");
			Add(_servicesContainer);

			RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
			_locator.OnChange += HandleChange;

			RefreshServices();
		}

		private void HandleChange()
		{
			EditorApplication.delayCall += () =>
			{
				if (panel == null)
				{
					return;
				}

				RefreshServices();
			};
		}

		private void HandleDetachFromPanel(DetachFromPanelEvent evt)
		{
			_locator.OnChange -= HandleChange;
			UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
		}

		private void RefreshServices()
		{
			_servicesContainer.Clear();
			var services = _locator.GetAllServices();

			if (services.Count == 0)
			{
				var emptyLabel = new Label("No services registered");
				emptyLabel.AddToClassList("no-services-message");
				_servicesContainer.Add(emptyLabel);
				return;
			}

			#if !DISABLE_SL_SCENE_TRACKING
			// Reset the item counter to ensure proper alternating pattern within each scene
			ServiceItem.ResetItemCounter();

			var servicesByScene = GroupServicesByScene(services);
			foreach (var sceneGroup in servicesByScene)
			{
				var sceneItem = new SceneItem(
					sceneGroup.SceneName,
					sceneGroup.Scene,
					sceneGroup.IsUnloaded,
					sceneGroup.IsDontDestroyOnLoad);

				_servicesContainer.Add(sceneItem);

				// Reset counter before each scene to ensure the pattern is consistent within each scene
				ServiceItem.ResetItemCounter();

				// Add services for this scene - pass the scene type for color consistency
				foreach (var (serviceType, serviceInstance) in sceneGroup.Services)
				{
					var serviceItem = new ServiceItem(serviceType, serviceInstance, sceneItem.GetSceneType());
					sceneItem.AddService(serviceItem);
				}
			}
			#else
          var sceneItem = new SceneItem("All scenes", null, false, false);
          _servicesContainer.Add(sceneItem);
          
          // Reset counter before adding services
          ServiceItem.ResetItemCounter();
             
          // Add services for this scene
          foreach (var (serviceType, serviceInstance) in services)
          {
             var serviceItem = new ServiceItem(serviceType, serviceInstance, sceneItem.GetSceneType());
             sceneItem.AddService(serviceItem);
          }
			#endif
		}


		#if !DISABLE_SL_SCENE_TRACKING
		private List<SceneGroupData> GroupServicesByScene(IReadOnlyDictionary<Type, object> services)
		{
			var result = new Dictionary<string, SceneGroupData>();

			foreach (var (serviceType, serviceInstance) in services)
			{
				// Get scene info for this service
				var sceneInfo = ServiceUtils.GetSceneInfoForService(serviceInstance, serviceType, _locator);

				// Generate a key for the dictionary that differentiates between scene types
				string sceneKey;
				if (sceneInfo.IsDontDestroyOnLoad)
				{
					// Special category for DontDestroyOnLoad
					sceneKey = "DONTDESTROY";
				}
				else if (sceneInfo.IsUnloaded)
				{
					// Use the unloaded scene prefix to differentiate 
					sceneKey = $"{ServiceUtils.UnloadedScenePrefix}{sceneInfo.SceneName}";
				}
				else
				{
					sceneKey = sceneInfo.SceneName;
				}

				// Add to appropriate scene group
				if (!result.TryGetValue(sceneKey, out var sceneGroup))
				{
					sceneGroup = new()
					{
						SceneName = sceneInfo.SceneName,
						Scene = sceneInfo.Scene,
						IsUnloaded = sceneInfo.IsUnloaded,
						IsDontDestroyOnLoad = sceneInfo.IsDontDestroyOnLoad
					};
					result[sceneKey] = sceneGroup;
				}

				sceneGroup.Services.Add((serviceType, serviceInstance));
			}

			// NEW ORDERING:
			// 1. "No Scene" services first
			// 2. DontDestroyOnLoad services
			// 3. Regular loaded scenes (alphabetically)
			// 4. Unloaded scenes (alphabetically) at the end
			return result.Values
				.OrderBy(group =>
				{
					if (group.SceneName == "No Scene")
					{
						return 1;
					}

					if (group.IsDontDestroyOnLoad)
					{
						return 2;
					}

					if (group.IsUnloaded)
					{
						return 4;
					}

					return 3; // Regular scenes
				})
				.ThenBy(group => group.SceneName) // Alphabetical ordering within each category
				.ToList();
		}
		#endif
	}
}