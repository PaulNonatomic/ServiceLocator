#nullable enable
using System;
using System.Collections.Generic;
using Nonatomic.ServiceLocator.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
	/// <summary>
	///     Represents the Services tab in the Service Locator Window.
	/// </summary>
	public class ServiceLocatorServicesTab : VisualElement
	{
		private readonly Button _clearSearchButton;
		private readonly List<LocatorItem> _locatorItems = new();
		private readonly Label _noResultsLabel;
		private readonly Action _refreshCallback;
		private readonly TextField _searchField;
		private readonly ScrollView _servicesScrollView;
		private bool _refreshPending;
		private List<ServiceLocator> _serviceLocators = new();

		public ServiceLocatorServicesTab(Action refreshCallback)
		{
			_refreshCallback = refreshCallback;
			AddToClassList("services-tab");

			// Title bar
			var titleBar = new VisualElement();
			titleBar.AddToClassList("services-title-bar");
			Add(titleBar);

			// Add header
			var headerLabel = new Label("Services");
			headerLabel.AddToClassList("services-header");
			titleBar.Add(headerLabel);

			// Add refresh button
			var refreshButton = new Button(RefreshServicesManually);
			refreshButton.tooltip = "Refresh service list";
			refreshButton.AddToClassList("refresh-button");
			titleBar.Add(refreshButton);

			var icon = new Image();
			icon.AddToClassList("button-icon");
			icon.image = Resources.Load<Texture2D>("Icons/refresh");
			refreshButton.Add(icon);

			// Create the search container
			var searchContainer = new VisualElement();
			searchContainer.AddToClassList("search-container");
			Add(searchContainer);

			// Add search field
			_searchField = new();
			_searchField.AddToClassList("search-field");
			
			// Set placeholder text based on Unity version
			#if UNITY_2022_3_OR_OLDER
			_searchField.placeholder = "Search services...";
			#elif UNITY_2023_1_OR_NEWER || UNITY_6_0_OR_NEWER
			_searchField.textEdition.placeholder = "Search services...";
			_searchField.textEdition.hidePlaceholderOnFocus = true;
			#endif
			
			_searchField.RegisterValueChangedCallback(OnSearchChanged);
			searchContainer.Add(_searchField);

			// Add clear button
			_clearSearchButton = new(ClearSearch);
			_clearSearchButton.AddToClassList("clear-search-button");
			_clearSearchButton.text = "×";
			_clearSearchButton.tooltip = "Clear search";
			_clearSearchButton.style.display = DisplayStyle.None; // Hidden initially
			searchContainer.Add(_clearSearchButton);

			// "No results" label (hidden initially)
			_noResultsLabel = new("No services match your search");
			_noResultsLabel.AddToClassList("no-results-message");
			_noResultsLabel.style.display = DisplayStyle.None;
			Add(_noResultsLabel);

			// Create and add the scroll view
			_servicesScrollView = new();
			Add(_servicesScrollView);

			// Register panel callbacks to handle attachment/detachment
			RegisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
			RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);

			// Initial refresh
			RefreshServices();
		}

		private void HandleAttachToPanel(AttachToPanelEvent evt)
		{
			// Subscribe to change events for each service locator
			foreach (var locator in _serviceLocators)
			{
				locator.OnChange += HandleServiceLocatorChange;
			}

			// Listen for editor scene changes
			EditorApplication.hierarchyChanged += HandleHierarchyChanged;
		}

		private void HandleDetachFromPanel(DetachFromPanelEvent evt)
		{
			// Unsubscribe from all service locator events
			foreach (var locator in _serviceLocators)
			{
				locator.OnChange -= HandleServiceLocatorChange;
			}

			// Stop listening for editor scene changes
			EditorApplication.hierarchyChanged -= HandleHierarchyChanged;

			// Clean up other callbacks
			UnregisterCallback<AttachToPanelEvent>(HandleAttachToPanel);
			UnregisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
		}

		private void HandleHierarchyChanged()
		{
			// This catches scene changes (loading/unloading)
			ScheduleRefresh();
		}

		private void HandleServiceLocatorChange()
		{
			// Schedule a refresh on the main thread
			ScheduleRefresh();
		}

		/// <summary>
		///     Schedules a refresh to happen on the next editor update.
		/// </summary>
		private void ScheduleRefresh()
		{
			if (_refreshPending)
			{
				return;
			}

			_refreshPending = true;
			EditorApplication.delayCall += () =>
			{
				// Check if the tab is still attached to a panel
				if (panel == null)
				{
					_refreshPending = false;
					return;
				}

				RefreshServices();
				_refreshPending = false;
			};
		}

		/// <summary>
		///     Manually triggered refresh from the UI button.
		/// </summary>
		private void RefreshServicesManually()
		{
			RefreshServices();

			// Also call the window's refresh callback
			_refreshCallback?.Invoke();
		}

		/// <summary>
		///     Refreshes the displayed services.
		/// </summary>
		public void RefreshServices()
		{
			if (_servicesScrollView == null)
			{
				return;
			}

			// Unsubscribe from old service locators first
			foreach (var locator in _serviceLocators)
			{
				locator.OnChange -= HandleServiceLocatorChange;
			}

			_servicesScrollView.Clear();
			_locatorItems.Clear();

			// Force asset database refresh to find any new ServiceLocator assets
			AssetDatabase.Refresh();

			// Find all ServiceLocator assets
			_serviceLocators = FindServiceLocatorAssets();

			// Subscribe to all service locator change events
			foreach (var locator in _serviceLocators)
			{
				locator.OnChange += HandleServiceLocatorChange;
			}

			// Create UI for each service locator
			foreach (var locator in _serviceLocators)
			{
				var locatorItem = new LocatorItem(locator);
				_locatorItems.Add(locatorItem);
				_servicesScrollView.contentContainer.Add(locatorItem);
			}

			// Apply any existing search filter
			if (!string.IsNullOrWhiteSpace(_searchField.value))
			{
				ApplySearchFilter(_searchField.value);
			}
		}

		/// <summary>
		///     Finds all ServiceLocator assets in the project.
		/// </summary>
		private static List<ServiceLocator> FindServiceLocatorAssets()
		{
			return AssetUtils.FindAssetsByType<ServiceLocator>();
		}

		/// <summary>
		///     Handles search field value changes.
		/// </summary>
		private void OnSearchChanged(ChangeEvent<string> evt)
		{
			var searchText = evt.newValue;

			// Show/hide clear button based on whether there's search text
			_clearSearchButton.style.display = string.IsNullOrWhiteSpace(searchText)
				? DisplayStyle.None
				: DisplayStyle.Flex;

			ApplySearchFilter(searchText);
		}

		/// <summary>
		///     Clears the search field.
		/// </summary>
		private void ClearSearch()
		{
			_searchField.value = string.Empty;
			// ApplySearchFilter will be called automatically by the value changed callback
		}

		/// <summary>
		///     Applies the search filter to all service items.
		/// </summary>
		private void ApplySearchFilter(string searchText)
		{
			if (string.IsNullOrWhiteSpace(searchText))
			{
				// Show all items when search is empty
				foreach (var locatorItem in _locatorItems)
				{
					locatorItem.ShowAllItems();
				}

				_noResultsLabel.style.display = DisplayStyle.None;
				return;
			}

			var totalMatchCount = 0;

			// Apply filter to each locator item
			foreach (var locatorItem in _locatorItems)
			{
				var matchCount = locatorItem.ApplySearchFilter(searchText);
				totalMatchCount += matchCount;
			}

			// Show "no results" message if needed
			_noResultsLabel.style.display = totalMatchCount > 0
				? DisplayStyle.None
				: DisplayStyle.Flex;
		}
	}
}