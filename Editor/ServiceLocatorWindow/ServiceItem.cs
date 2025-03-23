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
			container.style.flexDirection = FlexDirection.Row;
			container.style.justifyContent = Justify.SpaceBetween;
			container.style.alignItems = Align.Center;
			Add(container);
			
			var serviceLabel = new Label(serviceType.Name);
			serviceLabel.AddToClassList("service-label");
			container.Add(serviceLabel);
			
			var buttonsContainer = new VisualElement();
			buttonsContainer.style.flexDirection = FlexDirection.Row;
			container.Add(buttonsContainer);
			
			// Add Open Script button
			var openButton = new Button(() => OpenScriptInIDE(serviceType))
			{
				text = "Open Script"
			};
			openButton.AddToClassList("open-script-button");
			buttonsContainer.Add(openButton);
			
			// Add Ping button only for MonoBehaviours
			if (serviceInstance is MonoBehaviour monoBehaviour)
			{
				var pingButton = new Button(() => PingGameObject(monoBehaviour))
				{
					text = "Ping"
				};
				pingButton.AddToClassList("ping-button");
				buttonsContainer.Add(pingButton);
			}
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