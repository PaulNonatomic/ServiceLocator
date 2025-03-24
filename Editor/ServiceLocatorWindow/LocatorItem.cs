using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Nonatomic.ServiceLocator.Editor.Utils;

namespace Nonatomic.ServiceLocator.Editor
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
			
			_servicesContainer = new VisualElement();
			_servicesContainer.AddToClassList("services-container");
			Add(_servicesContainer);
			
			// Register for DetachFromPanel event
			RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
			
			// Register for service changes
			_locator.OnChange += HandleChange;
			
			RefreshServices();
		}

		private void HandleChange()
		{
			UnityEditor.EditorApplication.delayCall += () =>
			{
				if (panel == null) return;
				RefreshServices();
			};
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			_locator.OnChange -= HandleChange;
			UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
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
			
			var servicesByScene = GroupServicesByScene(services);
			foreach (var sceneGroup in servicesByScene)
			{
				var sceneItem = new SceneItem(sceneGroup.SceneName, sceneGroup.Scene, sceneGroup.IsUnloaded);
				_servicesContainer.Add(sceneItem);
				
				// Add services for this scene
				foreach (var (serviceType, serviceInstance) in sceneGroup.Services)
				{
					var serviceItem = new ServiceItem(serviceType, serviceInstance);
					sceneItem.AddService(serviceItem);
				}
			}
		}
		
		private List<SceneGroupData> GroupServicesByScene(IReadOnlyDictionary<Type, object> services)
		{
			var result = new Dictionary<string, SceneGroupData>();
	
			foreach (var (serviceType, serviceInstance) in services)
			{
				// Get scene info for this service including type info
				var sceneInfo = ServiceUtils.GetSceneInfoForService(serviceInstance, serviceType, _locator);
		
				// Generate a key for the dictionary
				var sceneKey = sceneInfo.IsUnloaded 
					? $"{ServiceUtils.UNLOADED_SCENE_PREFIX}{sceneInfo.SceneName}" 
					: sceneInfo.SceneName;
		
				// Add to appropriate scene group
				if (!result.TryGetValue(sceneKey, out var sceneGroup))
				{
					sceneGroup = new SceneGroupData
					{
						SceneName = sceneInfo.SceneName,
						Scene = sceneInfo.Scene,
						IsUnloaded = sceneInfo.IsUnloaded
					};
					result[sceneKey] = sceneGroup;
				}
		
				sceneGroup.Services.Add((serviceType, serviceInstance));
			}
	
			// Sort scenes: Loaded scenes alphabetically, then Unloaded scenes, then "No Scene"
			return result.Values
				.OrderBy(group => group.IsUnloaded ? 1 : 0)
				.ThenBy(group => group.SceneName == "No Scene" ? 1 : 0)
				.ThenBy(group => group.SceneName)
				.ToList();
		}
	}
}