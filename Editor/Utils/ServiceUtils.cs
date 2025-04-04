using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nonatomic.ServiceLocator.Utils
{
	public static class ServiceUtils
	{
		// Special scene name for unloaded scenes
		public const string UnloadedScenePrefix = "__UNLOADED__";
		public const string DontDestroyOnLoadSceneName = "DontDestroyOnLoad";

		#if !DISABLE_SL_SCENE_TRACKING
		public static SceneInfo GetSceneInfoForService(object service, Type serviceType, ServiceLocator locator)
		{
			var recordedSceneName = locator.GetSceneNameForService(serviceType);

			try
			{
				// Non-MonoBehaviour service
				if (service is not MonoBehaviour monoBehaviour)
				{
					return new(default, "No Scene", false);
				}

				try
				{
					// Try to access the MonoBehaviour - this will throw if it's been destroyed
					var go = monoBehaviour.gameObject;

					// If we got here, the MonoBehaviour is still valid
					var scene = go.scene;

					// Check if this is a DontDestroyOnLoad scene
					if (scene.name == DontDestroyOnLoadSceneName || scene.buildIndex == -1)
					{
						return new(scene, scene.name, false, true);
					}

					// Check if the scene from recordedSceneName is currently loaded
					var isSceneCurrentlyLoaded = false;
					for (var i = 0; i < SceneManager.sceneCount; i++)
					{
						var loadedScene = SceneManager.GetSceneAt(i);
						if (loadedScene.name != recordedSceneName)
						{
							continue;
						}

						isSceneCurrentlyLoaded = true;
						break;
					}

					if (isSceneCurrentlyLoaded || recordedSceneName == "No Scene")
					{
						return new(scene, scene.name, false);
					}

					Debug.Log($"Detected unloaded scene for {serviceType.Name}: {recordedSceneName}");
					return new(default, recordedSceneName, true);
				}
				catch (MissingReferenceException exception)
				{
					// MonoBehaviour has been destroyed - this is what we expect for unloaded scenes
					Debug.Log(
						$"Service {serviceType.Name} is a destroyed MonoBehaviour from scene {recordedSceneName}");
					return new(default, recordedSceneName, true);
				}
			}
			catch (Exception ex)
			{
				// Fallback for any unexpected errors
				Debug.LogError($"Error processing service {serviceType.Name}: {ex.Message}");
				return new(default, recordedSceneName != "No Scene" ? recordedSceneName : "Unknown Scene",
					recordedSceneName != "No Scene");
			}
		}
		#endif

		public class SceneInfo
		{
			public SceneInfo(Scene scene, string sceneName, bool isUnloaded, bool isDontDestroyOnLoad = false)
			{
				Scene = scene;
				SceneName = sceneName;
				IsUnloaded = isUnloaded;
				IsDontDestroyOnLoad = isDontDestroyOnLoad;
			}

			public Scene Scene { get; }
			public string SceneName { get; }
			public bool IsUnloaded { get; }
			public bool IsDontDestroyOnLoad { get; }
		}
	}
}