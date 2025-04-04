using System;
using Nonatomic.ServiceLocator.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
    public class ServiceItem : VisualElement
    {
       // Static counter to track item position for alternating colors
       private static int _itemCounter = 0;
       
       // References to the icons
       private Image _icon;
       private Texture2D _normalIcon;
       private Texture2D _hoverIcon;
       private Color _iconColor;

       public ServiceItem(Type serviceType, object serviceInstance, SceneType sceneType = SceneType.Regular)
       {
          // Add the base service-item class
          AddToClassList("service-item");
          
          // Add alternating background class (even/odd)
          if (_itemCounter % 2 == 0)
          {
             AddToClassList("service-item-even");
          }
          else
          {
             AddToClassList("service-item-odd");
          }
          
          // Increment the counter for the next item
          _itemCounter++;
          
          var container = new VisualElement();
          container.AddToClassList("service-item-container");
          Add(container);

          // Load both icon textures upfront
          _normalIcon = Resources.Load<Texture2D>("Icons/circle");
          _hoverIcon = Resources.Load<Texture2D>("Icons/circle-fill");
          
          // Create the icon image element
          _icon = new Image();
          _icon.AddToClassList("service-icon");
          _icon.image = _normalIcon; // Start with normal icon
          
          // Store the color to use for both states
          _iconColor = GetColorForSceneType(sceneType);
          
          // Set initial tint color
          _icon.tintColor = _iconColor;
          
          container.Add(_icon);
          
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
          
          var openIcon = new Image();
          openIcon.AddToClassList("open-script-icon");
          openIcon.image = Resources.Load<Texture2D>("Icons/pencil");
          openButton.Add(openIcon);
          
          // Register mouse hover events at the container level for better UX
          RegisterCallbacks(container, serviceInstance);
       }
       
       private void RegisterCallbacks(VisualElement container, object serviceInstance)
       {
          // Handle mouse enter (hover start)
          container.RegisterCallback<MouseEnterEvent>((evt) => {
             // Swap to the hover icon and maintain color
             _icon.image = _hoverIcon;
             _icon.tintColor = _iconColor;
          });

          // Handle mouse leave (hover end)
          container.RegisterCallback<MouseLeaveEvent>((evt) => {
             // Switch back to normal icon and maintain color
             _icon.image = _normalIcon;
             _icon.tintColor = _iconColor;
          });
          
          container.RegisterCallback<ClickEvent>((evt) => {
             if (serviceInstance is MonoBehaviour monoBehaviour)
             {
                PingGameObject(monoBehaviour);
             }
          });
       }
       
       /// <summary>
       /// Gets a color matching the scene type.
       /// </summary>
       private Color GetColorForSceneType(SceneType sceneType)
       {
          return sceneType switch
          {
              SceneType.NoScene => new Color(0.129f, 0.588f, 0.953f), // #2196F3 Blue
              SceneType.DontDestroyOnLoad => new Color(0.298f, 0.686f, 0.314f), // #4CAF50 Green
              SceneType.Unloaded => new Color(1.0f, 0.42f, 0.42f), // #FF6B6B Red
              SceneType.Regular => new Color(1.0f, 0.596f, 0.0f), // #FF9800 Orange
              _ => Color.white
          };
       }

       // Method to reset the counter when refreshing the UI
       public static void ResetItemCounter()
       {
          _itemCounter = 0;
       }

       private static void PingGameObject(MonoBehaviour monoBehaviour)
       {
          Selection.activeGameObject = monoBehaviour.gameObject;
          EditorGUIUtility.PingObject(monoBehaviour.gameObject);
       }
       
       private void OpenScriptInIDE(Type type)
       {
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