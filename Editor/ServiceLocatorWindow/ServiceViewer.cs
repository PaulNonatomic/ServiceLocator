using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Nonatomic.ServiceLocator.Utils;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
	/// <summary>
	/// A component that displays services with filtering capabilities
	/// </summary>
	public class ServiceViewer : VisualElement
	{
		public event Action OnRefreshRequested;

		private readonly ScrollView _scrollView;
		private readonly DependencyFilterControl _filterControl;
		private readonly List<ServiceLocator> _serviceLocators = new();
		private Type _currentFilterType;
		private DependencyFilterControl.FilterMode _currentFilterMode;

		public ServiceViewer()
		{
			_filterControl = new DependencyFilterControl();
			_filterControl.OnFilterChanged += HandleFilterChanged;
			_filterControl.OnFilterCleared += HandleFilterCleared;
			Add(_filterControl);

			var legend = CreateDependencyLegend();
			Add(legend);

			_scrollView = new ScrollView();
			_scrollView.AddToClassList("services-scroll-view");
			Add(_scrollView);

			RegisterCallback<DetachFromPanelEvent>(e => CleanupEventHandlers());
		}

		/// <summary>
		/// Updates the view with services from the provided service locators
		/// </summary>
		public void UpdateServices(List<ServiceLocator> serviceLocators)
		{
			_serviceLocators.Clear();
			_serviceLocators.AddRange(serviceLocators);

			// Check if we have an active filter to reapply
			if (_filterControl.IsFilterActive && _filterControl.SelectedServiceType != null)
			{
				RefreshServicesViewWithFilter(_filterControl.SelectedServiceType, _filterControl.CurrentMode);
			}
			else
			{
				RefreshServicesView();
			}
		}

		/// <summary>
		/// Clear resources and event handlers when detached
		/// </summary>
		private void CleanupEventHandlers()
		{
			_filterControl.OnFilterChanged -= HandleFilterChanged;
			_filterControl.OnFilterCleared -= HandleFilterCleared;
		}

		/// <summary>
		/// Handles when a filter is applied
		/// </summary>
		private void HandleFilterChanged(Type serviceType, DependencyFilterControl.FilterMode filterMode)
		{
			_currentFilterType = serviceType;
			_currentFilterMode = filterMode;
			RefreshServicesViewWithFilter(serviceType, filterMode);
		}

		/// <summary>
		/// Handles when a filter is cleared
		/// </summary>
		private void HandleFilterCleared()
		{
			_currentFilterType = null;
			RefreshServicesView();

			OnRefreshRequested?.Invoke();
		}

		/// <summary>
		/// Refreshes the services view with no filtering
		/// </summary>
		private void RefreshServicesView()
		{
			_scrollView.Clear();
			ServiceItem.ClearAllServiceItems();

			foreach (var locator in _serviceLocators)
			{
				var locatorItem = CreateLocatorItem(locator);
				_scrollView.Add(locatorItem);
			}
		}

		/// <summary>
		/// Creates a standard locator item that shows all services
		/// </summary>
		private VisualElement CreateLocatorItem(ServiceLocator locator)
		{
			var container = new VisualElement();
			container.AddToClassList("locator-item");

			var headerLabel = new Label(locator.name);
			headerLabel.AddToClassList("locator-header");
			container.Add(headerLabel);

			var servicesContainer = new VisualElement();
			servicesContainer.AddToClassList("services-container");
			container.Add(servicesContainer);

			// Get all services for this locator
			var services = locator.GetAllServices();

			if (services.Count == 0)
			{
				var emptyLabel = new Label("No services registered");
				emptyLabel.AddToClassList("no-services-message");
				servicesContainer.Add(emptyLabel);
				return container;
			}

			// Group services by scene
			var servicesByScene = GroupServicesByScene(services, locator);

			// Add all scenes and their services
			foreach (var sceneGroup in servicesByScene)
			{
				var sceneItem = CreateSceneItem(sceneGroup);
				servicesContainer.Add(sceneItem);
			}

			return container;
		}

		/// <summary>
		/// Refreshes the services view with filtering applied
		/// </summary>
		private void RefreshServicesViewWithFilter(Type serviceType, DependencyFilterControl.FilterMode filterMode)
		{
			_scrollView.Clear();
			ServiceItem.ClearAllServiceItems();

			var allServiceTypes = CollectAllServiceTypes();
			var dependencies = serviceType != null ? ServiceDependencyAnalyzer.GetServiceDependencies(serviceType) : new HashSet<Type>();

			var dependents = serviceType != null ? ServiceDependencyAnalyzer.GetServiceDependents(serviceType, allServiceTypes) : new HashSet<Type>();

			// Add filtered locator items
			foreach (var locator in _serviceLocators)
			{
				var locatorItem = CreateFilteredLocatorItem(locator, serviceType, dependencies, dependents, filterMode);
				if (locatorItem != null)
				{
					_scrollView.Add(locatorItem);
				}
			}
		}

		/// <summary>
		/// Creates a filtered locator item that only shows relevant services
		/// </summary>
		private VisualElement CreateFilteredLocatorItem(ServiceLocator locator, Type filterServiceType,
			HashSet<Type> dependencies, HashSet<Type> dependents,
			DependencyFilterControl.FilterMode filterMode)
		{
			var container = new VisualElement();
			container.AddToClassList("locator-item");

			var headerLabel = new Label(locator.name);
			headerLabel.AddToClassList("locator-header");
			container.Add(headerLabel);

			var servicesContainer = new VisualElement();
			servicesContainer.AddToClassList("services-container");
			container.Add(servicesContainer);

			// Get all services for this locator
			var services = locator.GetAllServices();
			if (services.Count == 0)
			{
				return null;
			}

			// Filter services based on the selected filter mode
			var filteredServices = FilterServices(services, filterServiceType, dependencies, dependents, filterMode);
			if (filteredServices.Count == 0)
			{
				return null;
			}

			// Group filtered services by scene
			var servicesByScene = GroupServicesByScene(filteredServices, locator);
			foreach (var sceneGroup in servicesByScene)
			{
				var sceneItem = CreateSceneItem(sceneGroup);
				servicesContainer.Add(sceneItem);

				// Mark the selected service with special styling
				if (filterServiceType == null) continue;

				foreach (var (serviceType, _) in sceneGroup.Services)
				{
					if (serviceType != filterServiceType) continue;

					// Find the service item and mark it
					var serviceItems = sceneItem
						.Query<ServiceItem>()
						.Where(item => item.ServiceType == serviceType)
						.ToList();

					foreach (var serviceItem in serviceItems)
					{
						serviceItem.AddToClassList("service-selected");

						// Add special handling for the selected service
						serviceItem.RegisterCallback<ClickEvent>(evt =>
						{
							if ((evt.modifiers & EventModifiers.Alt) != 0) return;
							_filterControl.ApplyFilter(null);
						});
					}
				}
			}

			return container;
		}

		/// <summary>
		/// Filters services based on the selected filter mode
		/// </summary>
		private static Dictionary<Type, object> FilterServices(
			IReadOnlyDictionary<Type, object> services,
			Type filterServiceType,
			HashSet<Type> dependencies,
			HashSet<Type> dependents,
			DependencyFilterControl.FilterMode filterMode)
		{
			var result = new Dictionary<Type, object>();

			// Always include the filter service itself
			if (filterServiceType != null && services.TryGetValue(filterServiceType, out var filterService))
			{
				result[filterServiceType] = filterService;
			}

			foreach (var kvp in services)
			{
				if (kvp.Key == filterServiceType) continue;

				var shouldInclude = filterMode switch
				{
					DependencyFilterControl.FilterMode.Dependencies =>
						dependencies.Contains(kvp.Key),
					DependencyFilterControl.FilterMode.Dependents =>
						dependents.Contains(kvp.Key),
					DependencyFilterControl.FilterMode.Both =>
						dependencies.Contains(kvp.Key) || dependents.Contains(kvp.Key),
					DependencyFilterControl.FilterMode.All =>
						true,
					_ => false
				};

				if (shouldInclude)
				{
					result[kvp.Key] = kvp.Value;
				}
			}

			return result;
		}

		/// <summary>
		/// Creates a scene group item containing services
		/// </summary>
		private VisualElement CreateSceneItem(SceneGroupData sceneGroup)
		{
			var sceneItem = new VisualElement();
			sceneItem.AddToClassList("scene-item");

			var sceneFoldout = new Foldout
			{
				text = sceneGroup.IsUnloaded ? $"{sceneGroup.SceneName} (Unloaded)" : sceneGroup.SceneName,
				value = true
			};
			sceneFoldout.AddToClassList("scene-header");
			sceneFoldout.contentContainer.AddToClassList("scene-content");

			if (sceneGroup.IsUnloaded)
			{
				sceneFoldout.AddToClassList("unloaded-scene");
			}

			sceneItem.Add(sceneFoldout);

			// Add all services for this scene
			foreach (var (serviceType, serviceInstance) in sceneGroup.Services)
			{
				var serviceItem = new ServiceItem(serviceType, serviceInstance);
				serviceItem.RegisterCallback<ClickEvent>(evt =>
				{
					if ((evt.modifiers & EventModifiers.Alt) == 0) return;

					// If service is clicked with Alt key, apply filter
					_filterControl.ApplyFilter(serviceType);
					evt.StopPropagation();
				});

				sceneFoldout.Add(serviceItem);
			}

			return sceneItem;
		}

		/// <summary>
		/// Groups services by the scene they belong to
		/// </summary>
		private List<SceneGroupData> GroupServicesByScene(IReadOnlyDictionary<Type, object> services, ServiceLocator locator)
		{
			var result = new Dictionary<string, SceneGroupData>();

			foreach (var (serviceType, serviceInstance) in services)
			{
				var sceneInfo = ServiceUtils.GetSceneInfoForService(serviceInstance, serviceType, locator);

				var sceneKey = sceneInfo.IsUnloaded
					? $"{ServiceUtils.UnloadedScenePrefix}{sceneInfo.SceneName}"
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

		/// <summary>
		/// Collects all service types from all locators
		/// </summary>
		private List<Type> CollectAllServiceTypes()
		{
			var result = new List<Type>();

			foreach (var locator in _serviceLocators)
			{
				var services = locator.GetAllServices();
				result.AddRange(services.Keys);
			}

			return result.Distinct().ToList();
		}

		/// <summary>
		/// Creates a legend explaining the dependency highlighting
		/// </summary>
		private VisualElement CreateDependencyLegend()
		{
			var legend = new VisualElement();
			legend.AddToClassList("dependency-legend");

			var instructionsRow = new VisualElement();
			instructionsRow.AddToClassList("legend-row");

			var instructionsLabel = new Label("Alt+Click a service to filter by its dependencies");
			instructionsLabel.AddToClassList("legend-instructions");
			instructionsRow.Add(instructionsLabel);

			legend.Add(instructionsRow);

			var dependenciesRow = new VisualElement();
			dependenciesRow.AddToClassList("legend-row");

			var dependencyItem = new VisualElement();
			dependencyItem.AddToClassList("legend-item");

			var dependencyColor = new VisualElement();
			dependencyColor.AddToClassList("legend-color");
			dependencyColor.AddToClassList("dependency-color");
			dependencyItem.Add(dependencyColor);

			var dependencyLabel = new Label("Dependencies (services used by the selected service)");
			dependencyLabel.AddToClassList("legend-label");
			dependencyItem.Add(dependencyLabel);

			dependenciesRow.Add(dependencyItem);
			legend.Add(dependenciesRow);

			var dependentsRow = new VisualElement();
			dependentsRow.AddToClassList("legend-row");

			var dependentItem = new VisualElement();
			dependentItem.AddToClassList("legend-item");

			var dependentColor = new VisualElement();
			dependentColor.AddToClassList("legend-color");
			dependentColor.AddToClassList("dependent-color");
			dependentItem.Add(dependentColor);

			var dependentLabel = new Label("Dependents (services that use the selected service)");
			dependentLabel.AddToClassList("legend-label");
			dependentItem.Add(dependentLabel);

			dependentsRow.Add(dependentItem);
			legend.Add(dependentsRow);

			var bidirectionalRow = new VisualElement();
			bidirectionalRow.AddToClassList("legend-row");

			var bidirectionalItem = new VisualElement();
			bidirectionalItem.AddToClassList("legend-item");

			var bidirectionalColor = new VisualElement();
			bidirectionalColor.AddToClassList("legend-color");
			bidirectionalColor.AddToClassList("bidirectional-color");
			bidirectionalItem.Add(bidirectionalColor);

			var bidirectionalLabel = new Label("Bidirectional (services with mutual dependencies)");
			bidirectionalLabel.AddToClassList("legend-label");
			bidirectionalItem.Add(bidirectionalLabel);

			bidirectionalRow.Add(bidirectionalItem);
			legend.Add(bidirectionalRow);

			return legend;
		}

		/// <summary>
		/// Information about a scene
		/// </summary>
		private class SceneGroupData
		{
			public string SceneName { get; set; } = "No Scene";
			public Scene Scene { get; set; } = default;
			public bool IsUnloaded { get; set; } = false;
			public List<(Type Type, object Instance)> Services { get; } = new List<(Type, object)>();
		}
	}
}