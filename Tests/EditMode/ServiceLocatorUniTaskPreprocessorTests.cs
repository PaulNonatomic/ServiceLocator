#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.EditMode
{
	[TestFixture]
	public class ServiceLocatorUniTaskPreprocessorTests
	{
		[SetUp]
		public void Setup()
		{
			_serviceLocator = ScriptableObject.CreateInstance<TestServiceLocator>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_serviceLocator);
		}

		private TestServiceLocator _serviceLocator;

		/// <summary>
		///     Tests that the UniTask methods are conditionally compiled based on preprocessor directives
		/// </summary
	}
}
#endif