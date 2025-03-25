using System;
using System.Collections.Generic;
using System.Linq;
using Nonatomic.ServiceLocator.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.ServiceLocatorWindow
{
    public class ServiceItem : VisualElement
    {
        public Type ServiceType => _serviceType;
        
        private readonly Type _serviceType;
        private readonly object _serviceInstance;
        private readonly VisualElement _container;
        private readonly Label _dependencyIndicator;
        private static readonly Dictionary<Type, List<ServiceItem>> AllServiceItems = new();
        private const string DependencyHighlightClass = "dependency-highlight";
        private const string DependentHighlightClass = "dependent-highlight";
        private const string BidirectionalHighlightClass = "bidirectional-dependency";
        private const string ServiceSelectedClass = "service-selected";
        
        public ServiceItem(Type serviceType, object serviceInstance)
        {
            _serviceType = serviceType;
            _serviceInstance = serviceInstance;
            
            AddToClassList("service-item");

            _dependencyIndicator = new Label();
            _dependencyIndicator.AddToClassList("dependency-indicator");
            Add(_dependencyIndicator);

            _container = CreateContainer(this);
            CreateServiceIcon(_container);
            CreateServiceLabel(_serviceType, _container);
            CreateEditServiceButton(_serviceType, _serviceInstance, _container);
            
            RegisterCallback<MouseEnterEvent>(HandleMouseEnter);
            RegisterCallback<MouseLeaveEvent>(HandleMouseLeave);
            RegisterServiceItem();
            
            tooltip = $"{serviceType.FullName}";
        }
        
        public static void ClearAllServiceItems()
        {
            AllServiceItems.Clear();
        }
        
        private void RegisterServiceItem()
        {
            if (!AllServiceItems.TryGetValue(_serviceType, out var items))
            {
                items = new List<ServiceItem>();
                AllServiceItems[_serviceType] = items;
            }
          
            items.Add(this);
          
            RegisterCallback<DetachFromPanelEvent>(HandleDetachFromPanel);
        }
        
        private void HandleMouseEnter(MouseEnterEvent evt)
        {
            HighlightDependencies();
        }

        private void HandleMouseLeave(MouseLeaveEvent evt)
        {
            ClearAllHighlights();
        }
        
        private void UnregisterServiceItem()
        {
            if (!AllServiceItems.TryGetValue(_serviceType, out var items)) return;
            
            items.Remove(this);
            if (items.Count == 0)
            {
                AllServiceItems.Remove(_serviceType);
            }
        }
        
        private void HandleDetachFromPanel(DetachFromPanelEvent evt)
        {
            UnregisterServiceItem();
        }

        private void CreateEditServiceButton(Type serviceType, object serviceInstance, VisualElement container)
        {
            var buttonsContainer = new VisualElement();
            buttonsContainer.AddToClassList("service-edit-btn-container");
            container.Add(buttonsContainer);
            
            var openButton = new Button(() => EditScript(serviceType));
            openButton.AddToClassList("open-script-button");
            buttonsContainer.Add(openButton);
            
            var openIcon = new Image();
            openIcon.AddToClassList("open-script-icon");
            openIcon.image = Resources.Load<Texture2D>("Icons/pencil");
            openButton.Add(openIcon);
            
            container.RegisterCallback<ClickEvent>((evt) =>
            {
                if ((evt.modifiers & EventModifiers.Alt) != 0)
                {
                    // Don't ping object if Alt is held (reserved for filtering)
                    return;
                }
                
                if (serviceInstance is MonoBehaviour monoBehaviour)
                {
                    PingGameObject(monoBehaviour);
                }
            });
        }

        private static void CreateServiceLabel(Type serviceType, VisualElement container)
        {
            var serviceLabel = new Label(serviceType.Name);
            serviceLabel.AddToClassList("service-label");
            container.Add(serviceLabel);
        }

        private static void CreateServiceIcon(VisualElement container)
        {
            var icon = new Image();
            icon.AddToClassList("service-icon");
            icon.image = Resources.Load<Texture2D>("Icons/circle");
            container.Add(icon);
        }

        private static VisualElement CreateContainer(VisualElement container)
        {
            var serviceItemContainer = new VisualElement();
            serviceItemContainer.AddToClassList("service-item-container");
            container.Add(serviceItemContainer);

            return serviceItemContainer;
        }

        private static void PingGameObject(MonoBehaviour monoBehaviour)
        {
            Selection.activeGameObject = monoBehaviour.gameObject;
            EditorGUIUtility.PingObject(monoBehaviour.gameObject);
        }

        private static void EditScript(Type type)
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
        
        private void HighlightDependencies()
        {
            AddToClassList(ServiceSelectedClass);
            
            var allServiceTypes = AllServiceItems.Keys.ToList();
            var dependencies = ServiceDependencyAnalyzer.GetServiceDependencies(_serviceType);
            var dependents = ServiceDependencyAnalyzer.GetServiceDependents(_serviceType, allServiceTypes);

            Debug.Log($"Service {_serviceType.Name} has {dependencies.Count} dependencies and {dependents.Count} dependents");
            
            // Apply highlights to dependencies
            foreach (var dependency in dependencies)
            {
                if (!AllServiceItems.TryGetValue(dependency, out var items)) continue;
                
                foreach (var item in items)
                {
                    // Check if this is a bidirectional dependency
                    if (dependents.Contains(dependency))
                    {
                        item.AddToClassList(BidirectionalHighlightClass);
                        item._dependencyIndicator.text = "↔";
                    }
                    else
                    {
                        item.AddToClassList(DependencyHighlightClass);
                        item._dependencyIndicator.text = "→";
                    }
                }
            }
            
            // Apply highlights to dependents
            foreach (var dependent in dependents)
            {
                if (!AllServiceItems.TryGetValue(dependent, out var items)) continue;
                if (dependencies.Contains(dependent)) continue;
                
                foreach (var item in items)
                {
                    item.AddToClassList(DependentHighlightClass);
                    item._dependencyIndicator.text = "←";
                }
            }
        }
        
        private static void ClearAllHighlights()
        {
            foreach (var itemsList in AllServiceItems.Values)
            {
                foreach (var item in itemsList)
                {
                    item.RemoveFromClassList(ServiceSelectedClass);
                    item.RemoveFromClassList(DependencyHighlightClass);
                    item.RemoveFromClassList(DependentHighlightClass);
                    item.RemoveFromClassList(BidirectionalHighlightClass);
                    item._dependencyIndicator.text = "";
                }
            }
        }
    }
}