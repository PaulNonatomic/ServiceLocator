using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nonatomic.ServiceLocator.Utils
{
    public static class ServiceLocatorReferenceFixer
    {
       [MenuItem("Tools/Service Locator/Fix ServiceLocator References", false)]
       public static void FixServiceLocatorReferences()
       {
          // Delay the execution to ensure everything is loaded
          EditorApplication.delayCall -= FixServiceLocatorReferences;

          var serviceLocator = AssetDatabase.FindAssets("t:ServiceLocator")
             .Select(guid => AssetDatabase.LoadAssetAtPath<ServiceLocator>(AssetDatabase.GUIDToAssetPath(guid)))
             .FirstOrDefault();

          if (!serviceLocator)
          {
             Debug.LogWarning("No ServiceLocator asset found in the project.");
             return;
          }

          FixReferencesInOpenScenes(serviceLocator);
          FixReferencesInAllAssets(serviceLocator);
       }
       
       private static void FixReferencesInOpenScenes(ServiceLocator serviceLocator)
       {
          for (var i = 0; i < SceneManager.sceneCount; i++)
          {
             var scene = SceneManager.GetSceneAt(i);
             if (!scene.isLoaded) continue;

             var sceneChanged = false;
             var rootGameObjects = scene.GetRootGameObjects();

             foreach (var go in rootGameObjects)
             {
                var components = go.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (var component in components)
                {
                   if (!component) continue;
                   var so = new SerializedObject(component);
                   var sp = so.GetIterator();
                   
                   while (sp.NextVisible(true))
                   {
                      if (sp.propertyType != SerializedPropertyType.ObjectReference ||
                         sp.objectReferenceValue ||
                         sp.type != "PPtr<ServiceLocator>") continue;
                      
                      sp.objectReferenceValue = serviceLocator;
                      so.ApplyModifiedProperties();
                      sceneChanged = true;
                   }
                }
             }

             if (!sceneChanged) continue;
             EditorSceneManager.MarkSceneDirty(scene);
             EditorSceneManager.SaveScene(scene);
          }
       }

       // Optional: Fix references in all assets
       private static void FixReferencesInAllAssets(ServiceLocator serviceLocator)
       {
          // Get all asset paths
          var allAssetPaths = AssetDatabase.GetAllAssetPaths();

          foreach (var path in allAssetPaths)
          {
             if (!path.StartsWith("Assets")) continue;
             var asset = AssetDatabase.LoadMainAssetAtPath(path);

             if (asset is not GameObject && asset is not ScriptableObject) continue;
             
             var assetChanged = false;
             var so = new SerializedObject(asset);
             var sp = so.GetIterator();

             while (sp.NextVisible(true))
             {
                if (sp.propertyType != SerializedPropertyType.ObjectReference ||
                   sp.objectReferenceValue ||
                   sp.type != "PPtr<ServiceLocator>") continue;
                
                sp.objectReferenceValue = serviceLocator;
                so.ApplyModifiedProperties();
                assetChanged = true;
                Debug.Log("ServiceLocatorReferenceFixer: Fixed missing reference in " + path);
             }

             if (!assetChanged) continue;
             EditorUtility.SetDirty(asset);
          }

          AssetDatabase.SaveAssets();
       }
    }
}