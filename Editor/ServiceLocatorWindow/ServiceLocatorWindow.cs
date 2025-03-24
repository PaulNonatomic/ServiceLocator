using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.ServiceLocator.Editor.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor
{
	public class ServiceLocatorWindow : EditorWindow
	{
		private VisualElement _root;
		private ScrollView _scrollView;
		private List<ServiceLocator> _serviceLocators;
		private bool _refreshPending = false;

		[MenuItem("Window/Nonatomic/Service Locator Window")]
		public static void ShowWindow()
		{
			var wnd = GetWindow<ServiceLocatorWindow>();
			wnd.titleContent = new GUIContent("Service Locator");
			wnd.minSize = new Vector2(300, 300);
		}

		public void CreateGUI()
		{
			// Get the root element
			_root = rootVisualElement;
			
			// Load and apply styles
			var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
				"Packages/com.nonatomic.servicelocator/Editor/ServiceLocatorWindow/ServiceLocatorWindowStyles.uss");
			_root.styleSheets.Add(styleSheet);
			_root.AddToClassList("service-locator-window");

			var headerLabel = new Label("Service Locator Services");
			headerLabel.AddToClassList("header");
			_root.Add(headerLabel);

			_scrollView = new ScrollView();
			_root.Add(_scrollView);

			var refreshButton = new Button(RefreshServices) { text = "Refresh Services" };
			refreshButton.AddToClassList("refresh-button");
			_root.Add(refreshButton);

			RefreshServices();
		}

		private void ScheduleRefresh()
		{
			if (_refreshPending) return;
			
			_refreshPending = true;
			EditorApplication.delayCall += () => 
			{
				if (this == null) return;
				RefreshServices();
				_refreshPending = false;
			};
		}

		private void RefreshServices()
		{
			if (_scrollView == null) return;
			
			Debug.Log("Refreshing services...");
	
			// Clear existing content
			_scrollView.Clear();
	
			// Force Unity to refresh asset references
			AssetDatabase.Refresh();
	
			// Find all ServiceLocator instances
			_serviceLocators = FindServiceLocatorAssets();
	
			// Create and add LocatorItems
			foreach (var locator in _serviceLocators)
			{
				var locatorItem = new LocatorItem(locator);
				_scrollView.contentContainer.Add(locatorItem);
			}
		}

		private static List<ServiceLocator> FindServiceLocatorAssets()
		{
			Debug.Log("FindServiceLocatorAssets");
			return AssetUtils.FindAssetsByType<ServiceLocator>();
		}

		private void OnFocus()
		{
			ScheduleRefresh();
		}
		
		private void OnEnable()
		{
			EditorApplication.playModeStateChanged += PlayModeStateChanged;
			
			EditorSceneManager.sceneOpened += HandleSceneOpened;
			EditorSceneManager.sceneClosed += HandleSceneClosed;
			EditorSceneManager.sceneLoaded += HandleSceneLoaded;
			EditorSceneManager.sceneUnloaded += HandleSceneUnloaded;
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= PlayModeStateChanged;
			
			EditorSceneManager.sceneOpened -= HandleSceneOpened;
			EditorSceneManager.sceneClosed -= HandleSceneClosed;
			EditorSceneManager.sceneLoaded -= HandleSceneLoaded;
			EditorSceneManager.sceneUnloaded -= HandleSceneUnloaded;
		}
		
		// Scene event handlers
		private void HandleSceneOpened(Scene scene, OpenSceneMode mode)
		{
			Debug.Log($"Scene opened: {scene.name}, mode: {mode}");
			ScheduleRefresh();
		}
		
		private void HandleSceneClosed(Scene scene)
		{
			Debug.Log($"Scene closed: {scene.name}");
			ScheduleRefresh();
		}
		
		private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Debug.Log($"Scene loaded: {scene.name}, mode: {mode}");
			ScheduleRefresh();
		}
		
		private void HandleSceneUnloaded(Scene scene)
		{
			Debug.Log($"Scene unloaded: {scene.name}");
			ScheduleRefresh();
		}

		private void PlayModeStateChanged(PlayModeStateChange state)
		{
			Debug.Log($"Play mode state changed: {state}");

			if (state is not (PlayModeStateChange.EnteredEditMode or PlayModeStateChange.EnteredPlayMode)) return;
			ScheduleRefresh();
		}
	}
}