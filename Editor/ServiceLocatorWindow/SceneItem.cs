using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
    public class SceneItem : VisualElement
    {
       private readonly Foldout _sceneFoldout;
       private readonly string _sceneName;
       private readonly Scene? _scene;
       private readonly bool _isUnloadedScene;
       private readonly bool _isDontDestroyOnLoad;
       private readonly SceneType _sceneType;

       public SceneItem(string sceneName, Scene? scene = null, bool isUnloaded = false, bool isDontDestroyOnLoad = false)
       {
          AddToClassList("scene-item");
          
          _sceneName = sceneName;
          _scene = scene;
          _isUnloadedScene = isUnloaded;
          _isDontDestroyOnLoad = isDontDestroyOnLoad;
          
          // Determine the scene type for coloring purposes
          if (_sceneName == "No Scene")
          {
             _sceneType = SceneType.NoScene;
          }
          else if (_isDontDestroyOnLoad)
          {
             _sceneType = SceneType.DontDestroyOnLoad;
          }
          else if (_isUnloadedScene)
          {
             _sceneType = SceneType.Unloaded;
          }
          else
          {
             _sceneType = SceneType.Regular;
          }
    
          string displayText = sceneName;
          
          // Handle display text for special cases
          if (_isDontDestroyOnLoad)
          {
             displayText = $"{sceneName} (DontDestroyOnLoad)";
          }
          else if (_isUnloadedScene)
          {
             displayText = $"{sceneName} (Unloaded)";
          }
          
          _sceneFoldout = new Foldout
          {
             text = displayText,
             value = true
          };
          _sceneFoldout.AddToClassList("scene-header");
          _sceneFoldout.contentContainer.AddToClassList("scene-content");
    
          // Apply appropriate styling class based on scene type
          if (_isDontDestroyOnLoad)
          {
             _sceneFoldout.AddToClassList("dont-destroy-scene");
          }
          else if (_isUnloadedScene)
          {
             _sceneFoldout.AddToClassList("unloaded-scene");
          }
          else if (_sceneName == "No Scene")
          {
             _sceneFoldout.AddToClassList("no-scene");
          }
          else
          {
             // Regular scene - add orange styling
             _sceneFoldout.AddToClassList("regular-scene");
          }
    
          Add(_sceneFoldout);
       }
       
       public void AddService(ServiceItem service)
       {
          _sceneFoldout.Add(service);
       }
       
       public SceneType GetSceneType()
       {
          return _sceneType;
       }
    }
}