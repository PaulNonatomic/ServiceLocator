#nullable enable
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
	/// <summary>
	///     A simple tab view control for the ServiceLocatorWindow.
	/// </summary>
	public class TabView : VisualElement
	{
		private readonly VisualElement _tabButtonsContainer;
		private readonly VisualElement _tabContentContainer;
		private readonly List<(string name, VisualElement content)> _tabs = new();
		private int _selectedTabIndex;

		public TabView()
		{
			AddToClassList("tab-view");

			_tabButtonsContainer = new();
			_tabButtonsContainer.AddToClassList("tab-buttons-container");
			Add(_tabButtonsContainer);

			_tabContentContainer = new();
			_tabContentContainer.AddToClassList("tab-content-container");
			Add(_tabContentContainer);
		}

		/// <summary>
		///     Adds a new tab with the specified name and content.
		/// </summary>
		public void AddTab(string name, VisualElement content)
		{
			var tabIndex = _tabs.Count;

			// Create button for the tab
			var button = new Button(() => SelectTab(tabIndex)) { text = name };
			button.AddToClassList("tab-button");
			_tabButtonsContainer.Add(button);

			// Add the tab to our list
			_tabs.Add((name, content));

			// If this is the first tab, select it
			if (_tabs.Count == 1)
			{
				SelectTab(0);
			}
		}

		/// <summary>
		///     Selects the tab at the specified index.
		/// </summary>
		private void SelectTab(int index)
		{
			if (index < 0 || index >= _tabs.Count)
			{
				return;
			}

			_selectedTabIndex = index;

			// Update button states
			for (var i = 0; i < _tabButtonsContainer.childCount; i++)
			{
				var button = _tabButtonsContainer[i] as Button;
				if (button == null)
				{
					continue;
				}

				if (i == _selectedTabIndex)
				{
					button.AddToClassList("tab-button-selected");
				}
				else
				{
					button.RemoveFromClassList("tab-button-selected");
				}
			}

			// Clear and add the selected content
			_tabContentContainer.Clear();
			_tabContentContainer.Add(_tabs[_selectedTabIndex].content);
		}
	}
}