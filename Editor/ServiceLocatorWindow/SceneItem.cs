using System.Collections.Generic;
using System.Linq;
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
       private readonly List<ServiceItem> _serviceItems = new();

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
          _serviceItems.Add(service);
          _sceneFoldout.Add(service);
       }
       
       public SceneType GetSceneType()
       {
          return _sceneType;
       }
       
       /// <summary>
       /// Gets the number of service items in this scene.
       /// </summary>
       public int GetServiceCount()
       {
          return _serviceItems.Count;
       }
       
       /// <summary>
       /// Shows all service items in this scene.
       /// </summary>
       public void ShowAllServices()
       {
          foreach (var service in _serviceItems)
          {
             service.style.display = DisplayStyle.Flex;
          }
       }
       
       /// <summary>
       /// Applies a search filter to the services in this scene.
       /// </summary>
       /// <param name="searchText">The text to search for</param>
       /// <returns>Number of services that match the filter</returns>
       public int ApplySearchFilter(string searchText)
       {
          if (string.IsNullOrWhiteSpace(searchText))
          {
             ShowAllServices();
             return _serviceItems.Count;
          }
          
          int matchCount = 0;
          
          foreach (var serviceItem in _serviceItems)
          {
             bool isMatch = IsServiceMatch(serviceItem, searchText);
             serviceItem.style.display = isMatch ? DisplayStyle.Flex : DisplayStyle.None;
             
             if (isMatch)
             {
                matchCount++;
             }
          }
          
          // Expand the foldout if we have matches
          if (matchCount > 0)
          {
             _sceneFoldout.value = true;
          }
          
          return matchCount;
       }
       
       /// <summary>
       /// Performs fuzzy matching on a service item.
       /// </summary>
       private bool IsServiceMatch(ServiceItem serviceItem, string searchText)
       {
          // Get the service name text from the Label inside the ServiceItem
          string serviceName = GetServiceName(serviceItem);
          
          if (string.IsNullOrEmpty(serviceName))
          {
             return false;
          }

          // Simple case-insensitive contains check (basic fuzzy matching)
          return serviceName.IndexOf(searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
       }
       
       /// <summary>
       /// Extracts the service name from a ServiceItem element.
       /// </summary>
       private string GetServiceName(ServiceItem serviceItem)
       {
          // Find the service label inside the service item
          Label? labelElement = serviceItem.Q<Label>(className: "service-label");
          return labelElement?.text ?? string.Empty;
       }
    }
}