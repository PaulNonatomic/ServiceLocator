#nullable enable
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
	public class ServiceLocatorWindow : EditorWindow
	{
		private bool _refreshPending;
		private VisualElement _root;
		private ServiceLocatorServicesTab _servicesTab;
		private ServiceLocatorSettingsTab _settingsTab;
		private TabView _tabView;

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

		public void CreateGUI()
		{
			_root = rootVisualElement;

			// Load the base stylesheet
			var baseStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
				"Packages/com.nonatomic.servicelocator/Editor/ServiceLocatorWindow/ServiceLocatorWindowStyles.uss");
			_root.styleSheets.Add(baseStyleSheet);

			// Load theme-specific stylesheet
			var themeSuffix = EditorGUIUtility.isProSkin ? "Dark" : "Light";
			var themeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
				$"Packages/com.nonatomic.servicelocator/Editor/ServiceLocatorWindow/ServiceLocatorWindowStyles{themeSuffix}.uss");
    
			// Add theme stylesheet if found
			if (themeStyleSheet != null)
			{
				_root.styleSheets.Add(themeStyleSheet);
			}
			
			
			_root.AddToClassList("service-locator-window");

			// Create tab view
			_tabView = new();
			_root.Add(_tabView);

			// Create services tab
			_servicesTab = new(ScheduleRefresh);
			_tabView.AddTab("Services", _servicesTab);

			// Create settings tab
			_settingsTab = new();
			_tabView.AddTab("Settings", _settingsTab);
		}

		private void OnFocus()
		{
			ScheduleRefresh();
		}

		[MenuItem("Tools/Service Locator/Service Locator Window")]
		public static void ShowWindow()
		{
			var wnd = GetWindow<ServiceLocatorWindow>();
			wnd.titleContent = new("Service Locator");
			wnd.minSize = new(400, 500);
		}

		private void ScheduleRefresh()
		{
			if (_refreshPending)
			{
				return;
			}

			_refreshPending = true;
			EditorApplication.delayCall += () =>
			{
				if (this == null)
				{
					return;
				}

				_servicesTab?.RefreshServices();
				_refreshPending = false;
			};
		}

		// Scene event handlers
		private void HandleSceneOpened(Scene scene, OpenSceneMode mode)
		{
			ScheduleRefresh();
		}

		private void HandleSceneClosed(Scene scene)
		{
			ScheduleRefresh();
		}

		private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			ScheduleRefresh();
		}

		private void HandleSceneUnloaded(Scene scene)
		{
			ScheduleRefresh();
		}

		private void PlayModeStateChanged(PlayModeStateChange state)
		{
			if (state is not (PlayModeStateChange.EnteredEditMode or PlayModeStateChange.EnteredPlayMode))
			{
				return;
			}

			ScheduleRefresh();
		}
	}
}