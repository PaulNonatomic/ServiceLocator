using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nonatomic.ServiceLocator.Editor.Utils
{
	public static class ServiceUtils
	{
		// Special scene name for unloaded scenes
		public const string UNLOADED_SCENE_PREFIX = "__UNLOADED__";
		
		// Utility class to hold scene information
		public class SceneInfo
		{
			public Scene Scene { get; }
			public string SceneName { get; }
			public bool IsUnloaded { get; }
			
			public SceneInfo(Scene scene, string sceneName, bool isUnloaded)
			{
				Scene = scene;
				SceneName = sceneName;
				IsUnloaded = isUnloaded;
			}
		}
		
		public static SceneInfo GetSceneInfoForService(object service, Type serviceType, ServiceLocator locator)
		{
			// Get the recorded scene name from ServiceLocator
			string recordedSceneName = locator.GetSceneNameForService(serviceType);
			
			try
			{
				// Check if this is a MonoBehaviour
				if (service is MonoBehaviour)
				{
					try
					{
						// Try to access the MonoBehaviour - this will throw if it's been destroyed
						MonoBehaviour monoBehaviour = (MonoBehaviour)service;
						
						// This will throw if the GameObject has been destroyed
						GameObject go = monoBehaviour.gameObject;
						
						// If we got here, the MonoBehaviour is still valid
						Scene scene = go.scene;
						
						// Check if the scene from recordedSceneName is currently loaded
						bool isSceneCurrentlyLoaded = false;
						for (int i = 0; i < SceneManager.sceneCount; i++)
						{
							Scene loadedScene = SceneManager.GetSceneAt(i);
							if (loadedScene.name == recordedSceneName)
							{
								isSceneCurrentlyLoaded = true;
								break;
							}
						}
						
						if (!isSceneCurrentlyLoaded && recordedSceneName != "No Scene")
						{
							Debug.Log($"Detected unloaded scene for {serviceType.Name}: {recordedSceneName}");
							return new SceneInfo(default, recordedSceneName, true);
						}
						
						// Regular loaded scene
						return new SceneInfo(scene, scene.name, false);
					}
					catch (MissingReferenceException)
					{
						// MonoBehaviour has been destroyed - this is what we expect for unloaded scenes
						Debug.Log($"Service {serviceType.Name} is a destroyed MonoBehaviour from scene {recordedSceneName}");
						return new SceneInfo(default, recordedSceneName, true);
					}
				}
				
				// Non-MonoBehaviour service
				return new SceneInfo(default, "No Scene", false);
			}
			catch (Exception ex)
			{
				// Fallback for any unexpected errors
				Debug.LogError($"Error processing service {serviceType.Name}: {ex.Message}");
				return new SceneInfo(default, recordedSceneName != "No Scene" ? recordedSceneName : "Unknown Scene", 
					recordedSceneName != "No Scene");
			}
		}
	}
}