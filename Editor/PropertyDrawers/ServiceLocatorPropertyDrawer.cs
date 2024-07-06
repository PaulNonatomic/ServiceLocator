using Nonatomic.ServiceLocator.Editor.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.Editor.PropertyDrawers
{
	[CustomPropertyDrawer(typeof(ServiceLocator))]
	public class ServiceLocatorPropertyDrawer : PropertyDrawer
	{
		private Button _newModelButton;

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			if (property.objectReferenceValue == null)
			{
				property.objectReferenceValue = AssetUtils.FindAssetByType<ServiceLocator>();
				property.serializedObject.ApplyModifiedProperties();
			}
			
			var root = CreateRoot();
			CreateServiceLocatorField(property, root);
			
			return root;
		}
		
		private VisualElement CreateServiceLocatorField(SerializedProperty property, VisualElement container)
		{
			var serviceLocatorField = new ObjectField("Service Locator")
			{
				objectType = typeof(ServiceLocator),
				allowSceneObjects = false,
				bindingPath = nameof(property)
			};
			
			serviceLocatorField.style.flexGrow = 1;
			serviceLocatorField.style.flexShrink = 1;
			serviceLocatorField.BindProperty(property);
			container.Add(serviceLocatorField);

			return serviceLocatorField;
		}
		
		private static VisualElement CreateRoot()
		{
			var root = new VisualElement();
			root.name = "root";
			root.style.flexDirection = FlexDirection.Row;
			
			return root;
		}
	}
}