using UnityEngine.SceneManagement;

namespace Nonatomic.ServiceLocator.Editor.Utils
{
	using UnityEngine;
	using UnityEditor;
	using UnityEditor.SceneManagement;
	using System.Linq;

	[InitializeOnLoad]
	public static class ServiceLocatorReferenceFixer
	{
		static ServiceLocatorReferenceFixer()
		{
			EditorApplication.delayCall += FixServiceLocatorReferences;
		}

		private static void FixServiceLocatorReferences()
		{
			// Delay the execution to ensure everything is loaded
			EditorApplication.delayCall -= FixServiceLocatorReferences;

			// Find the ServiceLocator asset
			var serviceLocator = AssetDatabase.FindAssets("t:ServiceLocator")
				.Select(guid => AssetDatabase.LoadAssetAtPath<ServiceLocator>(AssetDatabase.GUIDToAssetPath(guid)))
				.FirstOrDefault();

			if (!serviceLocator)
			{
				Debug.LogWarning("No ServiceLocator asset found in the project.");
				return;
			}

			// Fix references in open scenes
			FixReferencesInOpenScenes(serviceLocator);

			// Optionally, fix references in all assets
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