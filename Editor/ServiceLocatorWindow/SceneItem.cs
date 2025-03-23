using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor
{
	public class SceneItem : VisualElement
	{
		private readonly Foldout _sceneFoldout;
		private readonly string _sceneName;
		private readonly Scene _scene;
		private readonly bool _isUnloadedScene;

		public SceneItem(string sceneName, Scene scene, bool isUnloaded = false)
		{
			_sceneName = sceneName;
			_scene = scene;
			_isUnloadedScene = isUnloaded;
	
			_sceneFoldout = new Foldout
			{
				text = _isUnloadedScene ? $"{sceneName} (Unloaded)" : sceneName,
				value = true
			};
			_sceneFoldout.AddToClassList("scene-header");
	
			if (_isUnloadedScene)
			{
				Debug.Log($"Found unloaded scene: {sceneName}");
				_sceneFoldout.AddToClassList("unloaded-scene");
			}
	
			Add(_sceneFoldout);
		}
		
		private bool IsUnloadedScene(string sceneName, Scene scene)
		{
			// Skip the "No Scene" category
			if (sceneName == "No Scene")
				return false;
				
			// A scene is considered unloaded if any of these are true:
			// 1. The scene struct is not valid (default value or invalid handle)
			// 2. The scene is not loaded
			// 3. The scene handle is 0 (invalid/unloaded)
			// 4. The scene buildIndex is -1 (not part of build)
			bool sceneInvalid = !scene.IsValid();
			bool sceneNotLoaded = scene.IsValid() && !scene.isLoaded;
			bool sceneHandleZero = scene.handle == 0;
			bool sceneBuildIndexInvalid = scene.buildIndex < 0;
			
			bool result = sceneInvalid || sceneNotLoaded || sceneHandleZero || sceneBuildIndexInvalid;
			
			// Log detailed information for debugging
			Debug.Log($"Scene '{sceneName}' analysis: Invalid={sceneInvalid}, NotLoaded={sceneNotLoaded}, " +
				$"Handle={scene.handle}, BuildIndex={scene.buildIndex}, Result={result}");
				
			return result;
		}
		
		public void AddService(ServiceItem service)
		{
			_sceneFoldout.Add(service);
		}

		public void RemoveService(ServiceItem service)
		{
			if(!_sceneFoldout.Contains(service)) return;
			_sceneFoldout.Remove(service);
		}
	}
}