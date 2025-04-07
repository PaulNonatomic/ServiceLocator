using Nonatomic.ServiceLocator.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Nonatomic.ServiceLocator.PropertyDrawers
{
	[CustomPropertyDrawer(typeof(ServiceLocator))]
	public class ServiceLocatorPropertyDrawer : PropertyDrawer
	{
		private Button _newModelButton;

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			if (GuardAgainstDestroyedSerializedObject(property))
			{
				return default;
			}

			if (property.objectReferenceValue == null)
			{
				property.objectReferenceValue = AssetUtils.FindAssetByType<ServiceLocator>();
				property.serializedObject.ApplyModifiedProperties();
			}

			var root = CreateRoot();
			CreateServiceLocatorField(property, root);

			return root;
		}

		private static VisualElement CreateServiceLocatorField(SerializedProperty property, VisualElement container)
		{
			if (GuardAgainstDestroyedSerializedObject(property))
			{
				return default;
			}

			var serviceLocatorField = new ObjectField("Service Locator")
			{
				objectType = typeof(ServiceLocator),
				allowSceneObjects = false,
				bindingPath = nameof(property),
				style =
				{
					flexGrow = 1,
					flexShrink = 1
				}
			};

			serviceLocatorField.BindProperty(property);
			container.Add(serviceLocatorField);

			return serviceLocatorField;
		}

		private static VisualElement CreateRoot()
		{
			var root = new VisualElement
			{
				name = "root",
				style =
				{
					flexDirection = FlexDirection.Row
				}
			};

			return root;
		}

		private static bool GuardAgainstDestroyedSerializedObject(SerializedProperty property)
		{
			return property?.serializedObject == null ||
				   property.serializedObject.targetObject == null;
		}
	}
}