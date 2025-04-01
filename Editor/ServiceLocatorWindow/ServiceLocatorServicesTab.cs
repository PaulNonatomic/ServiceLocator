#nullable enable
using System;
using System.Collections.Generic;
using Nonatomic.ServiceLocator.Utils;
using UnityEditor;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
	/// <summary>
	///     Represents the Services tab in the Service Locator Window.
	/// </summary>
	public class ServiceLocatorServicesTab : VisualElement
	{
		private readonly Action _refreshCallback;
		private List<ServiceLocator> _serviceLocators;
		private readonly ScrollView _servicesScrollView;

		public ServiceLocatorServicesTab(Action refreshCallback)
		{
			_refreshCallback = refreshCallback;

			// Setup the root container
			AddToClassList("services-tab");

			// Add header
			var headerLabel = new Label("Service Locator Services");
			headerLabel.AddToClassList("header");
			Add(headerLabel);

			// Create and add the scroll view
			_servicesScrollView = new();
			Add(_servicesScrollView);
			
			// Add refresh button
			var refreshButton = new Button(RefreshServices) { text = "Refresh Services" };
			refreshButton.AddToClassList("refresh-button");
			Add(refreshButton);

			// Initial refresh
			RefreshServices();
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

			_servicesScrollView.Clear();
			AssetDatabase.Refresh();

			_serviceLocators = FindServiceLocatorAssets();

			foreach (var locator in _serviceLocators)
			{
				var locatorItem = new LocatorItem(locator);
				_servicesScrollView.contentContainer.Add(locatorItem);
			}

			// Call the provided refresh callback if it exists
			_refreshCallback?.Invoke();
		}

		/// <summary>
		///     Finds all ServiceLocator assets in the project.
		/// </summary>
		private static List<ServiceLocator> FindServiceLocatorAssets()
		{
			return AssetUtils.FindAssetsByType<ServiceLocator>();
		}
	}
}