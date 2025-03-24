using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Nonatomic.ServiceLocator.Editor.Utils;

namespace Nonatomic.ServiceLocator.Editor
{
	public class ServiceItem : VisualElement
	{
		public ServiceItem(Type serviceType, object serviceInstance)
		{
			AddToClassList("service-item");
			
			var container = new VisualElement();
			container.AddToClassList("service-item-container");
			Add(container);

			var icon = new Image();
			icon.AddToClassList("service-icon");
			container.Add(icon);
			
			var serviceLabel = new Label(serviceType.Name);
			serviceLabel.AddToClassList("service-label");
			container.Add(serviceLabel);
			
			var buttonsContainer = new VisualElement();
			buttonsContainer.AddToClassList("service-edit-btn-container");
			container.Add(buttonsContainer);
			
			// Add Open Script button
			var openButton = new Button(() => OpenScriptInIDE(serviceType));
			openButton.AddToClassList("open-script-button");
			buttonsContainer.Add(openButton);
			
			container.RegisterCallback<ClickEvent>((evt)=>
			{
				if (serviceInstance is MonoBehaviour monoBehaviour)
				{
					PingGameObject(monoBehaviour);
				}
			});
		}

		private void PingGameObject(MonoBehaviour monoBehaviour)
		{
			// Select the GameObject in the hierarchy
			Selection.activeGameObject = monoBehaviour.gameObject;
			
			// Ping it in the hierarchy to highlight it
			EditorGUIUtility.PingObject(monoBehaviour.gameObject);
		}
		
		private void OpenScriptInIDE(Type type)
		{
			// Try to find and open the script
			var script = ScriptFindingUtils.FindScriptForType(type);
			
			if (script != null)
			{
				AssetDatabase.OpenAsset(script);
				return;
			}
			
			// If not found, try to show a selection dialog
			var potentialScripts = ScriptFindingUtils.FindPotentialScriptsForType(type);
			
			if (potentialScripts.Count > 0)
			{
				// If there's only one potential script, just open it
				if (potentialScripts.Count == 1)
				{
					AssetDatabase.OpenAsset(potentialScripts[0]);
					return;
				}
				
				// Otherwise show a selection menu
				var menu = new GenericMenu();
				foreach (var potentialScript in potentialScripts)
				{
					var path = AssetDatabase.GetAssetPath(potentialScript);
					var displayPath = path.Replace("Assets/", "");
					
					menu.AddItem(new GUIContent(displayPath), false, () => {
						AssetDatabase.OpenAsset(potentialScript);
					});
				}
				
				menu.ShowAsContext();
				return;
			}
			
			// No scripts found
			EditorUtility.DisplayDialog("Script Not Found", 
				$"Could not find the script file for type {type.Name}.", "OK");
		}
	}
}