using System;
using Nonatomic.ServiceLocator.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
	public class ServiceItem : VisualElement
	{
		// Static counter to track item position for alternating colors
		private static int _itemCounter;
		private readonly Texture2D _hoverIcon;

		// References to the icons
		private readonly Image _icon;
		private readonly Color _iconColor;
		private readonly Texture2D _normalIcon;
		private readonly Label _serviceLabel;

		public ServiceItem(Type serviceType, object serviceInstance, SceneType sceneType = SceneType.Regular)
		{
			// Store the service type name for searching
			ServiceTypeName = serviceType.Name;

			// Add the base service-item class
			AddToClassList("service-item");

			// Add alternating background class (even/odd)
			if (_itemCounter % 2 == 0)
			{
				AddToClassList("service-item-even");
			}
			else
			{
				AddToClassList("service-item-odd");
			}

			// Increment the counter for the next item
			_itemCounter++;

			var container = new VisualElement();
			container.AddToClassList("service-item-container");
			Add(container);

			// Load both icon textures upfront
			_normalIcon = Resources.Load<Texture2D>("Icons/circle");
			_hoverIcon = Resources.Load<Texture2D>("Icons/circle-fill");

			// Create the icon image element
			_icon = new();
			_icon.AddToClassList("service-icon");
			_icon.image = _normalIcon; // Start with normal icon

			// Store the color to use for both states
			_iconColor = GetColorForSceneType(sceneType);

			// Set initial tint color
			_icon.tintColor = _iconColor;

			container.Add(_icon);

			_serviceLabel = new(ServiceTypeName);
			_serviceLabel.AddToClassList("service-label");
			container.Add(_serviceLabel);

			var buttonsContainer = new VisualElement();
			buttonsContainer.AddToClassList("service-edit-btn-container");
			container.Add(buttonsContainer);

			// Add Open Script button
			var openButton = new Button(() => OpenScriptInIDE(serviceType));
			openButton.AddToClassList("open-script-button");
			buttonsContainer.Add(openButton);

			var openIcon = new Image();
			openIcon.AddToClassList("open-script-icon");
			openIcon.image = Resources.Load<Texture2D>("Icons/pencil");
			openButton.Add(openIcon);

			// Register mouse hover events at the container level for better UX
			RegisterCallbacks(container, serviceInstance);
		}

		// The type name for search purposes
		public string ServiceTypeName { get; }

		private void RegisterCallbacks(VisualElement container, object serviceInstance)
		{
			// Handle mouse enter (hover start)
			container.RegisterCallback<MouseEnterEvent>(evt =>
			{
				// Swap to the hover icon and maintain color
				_icon.image = _hoverIcon;
				_icon.tintColor = _iconColor;
			});

			// Handle mouse leave (hover end)
			container.RegisterCallback<MouseLeaveEvent>(evt =>
			{
				// Switch back to normal icon and maintain color
				_icon.image = _normalIcon;
				_icon.tintColor = _iconColor;
			});

			container.RegisterCallback<ClickEvent>(evt =>
			{
				if (serviceInstance is MonoBehaviour monoBehaviour)
				{
					PingGameObject(monoBehaviour);
				}
			});
		}

		/// <summary>
		///     Gets a color matching the scene type.
		/// </summary>
		private Color GetColorForSceneType(SceneType sceneType)
		{
			return sceneType switch
			{
				SceneType.NoScene => new(0.129f, 0.588f, 0.953f), // #2196F3 Blue
				SceneType.DontDestroyOnLoad => new(0.298f, 0.686f, 0.314f), // #4CAF50 Green
				SceneType.Unloaded => new(1.0f, 0.42f, 0.42f), // #FF6B6B Red
				SceneType.Regular => new(1.0f, 0.596f, 0.0f), // #FF9800 Orange
				_ => Color.white
			};
		}

		// Method to reset the counter when refreshing the UI
		public static void ResetItemCounter()
		{
			_itemCounter = 0;
		}

		private static void PingGameObject(MonoBehaviour monoBehaviour)
		{
			Selection.activeGameObject = monoBehaviour.gameObject;
			EditorGUIUtility.PingObject(monoBehaviour.gameObject);
		}

		private void OpenScriptInIDE(Type type)
		{
			var script = ScriptFindingUtils.FindScriptForType(type);

			if (script != null)
			{
				AssetDatabase.OpenAsset(script);
				return;
			}

			// If not found, try to show a selection dialog
			var potentialScripts = ScriptFindingUtils.FindPotentialScriptsForType(type);

			if (potentialScripts.Count > 0)
			{
				// If there's only one potential script, just open it
				if (potentialScripts.Count == 1)
				{
					AssetDatabase.OpenAsset(potentialScripts[0]);
					return;
				}

				// Otherwise show a selection menu
				var menu = new GenericMenu();
				foreach (var potentialScript in potentialScripts)
				{
					var path = AssetDatabase.GetAssetPath(potentialScript);
					var displayPath = path.Replace("Assets/", "");

					menu.AddItem(new(displayPath), false, () => { AssetDatabase.OpenAsset(potentialScript); });
				}

				menu.ShowAsContext();
				return;
			}

			// No scripts found
			EditorUtility.DisplayDialog("Script Not Found",
				$"Could not find the script file for type {type.Name}.", "OK");
		}

		/// <summary>
		///     Checks if this service matches the search text using fuzzy matching.
		/// </summary>
		public bool MatchesSearch(string searchText)
		{
			if (string.IsNullOrWhiteSpace(searchText))
			{
				return true;
			}

			return FuzzyMatch(ServiceTypeName, searchText);
		}

		/// <summary>
		///     Performs a fuzzy match between the service name and search text.
		/// </summary>
		private bool FuzzyMatch(string serviceName, string searchText)
		{
			// Convert both strings to lowercase for case-insensitive matching
			var lowerServiceName = serviceName.ToLowerInvariant();
			var lowerSearchText = searchText.ToLowerInvariant();

			// Simple contains check first (most common case)
			if (lowerServiceName.Contains(lowerSearchText))
			{
				return true;
			}

			// More sophisticated fuzzy matching - check if the characters appear in order
			var serviceIndex = 0;
			var searchIndex = 0;

			while (serviceIndex < lowerServiceName.Length && searchIndex < lowerSearchText.Length)
			{
				if (lowerServiceName[serviceIndex] == lowerSearchText[searchIndex])
				{
					searchIndex++;
				}

				serviceIndex++;
			}

			// If we matched all characters in the search text
			return searchIndex == lowerSearchText.Length;
		}
	}
}