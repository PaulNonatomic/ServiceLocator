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
			_sceneFoldout.contentContainer.AddToClassList("scene-content");
	
			if (_isUnloadedScene)
			{
				Debug.Log($"Found unloaded scene: {sceneName}");
				_sceneFoldout.AddToClassList("unloaded-scene");
			}
	
			Add(_sceneFoldout);
		}
		
		public void AddService(ServiceItem service)
		{
			_sceneFoldout.Add(service);
		}
	}
}