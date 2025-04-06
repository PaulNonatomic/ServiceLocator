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
		/// </summary>
		[Test]
		public void ConfiguredFeatures_UniTaskMethods_DependOnPreprocessorDirectives()
		{
			var locatorType = typeof(BaseServiceLocator);

			#if !DISABLE_SL_UNITASK && ENABLE_UNITASK
			// When DISABLE_SL_UNITASK is not defined and ENABLE_UNITASK is defined,
			// UniTask methods should exist
			var hasUniTaskMethods = locatorType.GetMethods()
				.Any(m => m.Name.StartsWith("GetServiceUniTask") || m.Name.StartsWith("GetServicesUniTask"));

			Assert.IsTrue(hasUniTaskMethods,
				"GetServiceUniTask methods should exist when DISABLE_SL_UNITASK is not defined and ENABLE_UNITASK is defined");

			var hasUniTaskPromiseMap = locatorType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Any(f => f.Name.Contains("UniTaskPromiseMap"));

			Assert.IsTrue(hasUniTaskPromiseMap,
				"UniTaskPromiseMap field should exist when DISABLE_SL_UNITASK is not defined and ENABLE_UNITASK is defined");

			var hasCleanupUniTaskPromises = locatorType.GetMethod("CleanupUniTaskPromises");
			Assert.IsNotNull(hasCleanupUniTaskPromises,
				"CleanupUniTaskPromises method should exist when DISABLE_SL_UNITASK is not defined and ENABLE_UNITASK is defined");
			#elif DISABLE_SL_UNITASK || !ENABLE_UNITASK
            // When DISABLE_SL_UNITASK is defined or ENABLE_UNITASK is not defined,
            // UniTask methods should not exist
            var hasUniTaskMethods = locatorType.GetMethods()
                .Any(m => m.Name.StartsWith("GetServiceUniTask") || m.Name.StartsWith("GetServicesUniTask"));
            
            Assert.IsFalse(hasUniTaskMethods, 
                "GetServiceUniTask methods should not exist when DISABLE_SL_UNITASK is defined or ENABLE_UNITASK is not defined");
            
            var hasUniTaskPromiseMap =
 locatorType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Any(f => f.Name.Contains("UniTaskPromiseMap"));
            
            Assert.IsFalse(hasUniTaskPromiseMap, 
                "UniTaskPromiseMap field should not exist when DISABLE_SL_UNITASK is defined or ENABLE_UNITASK is not defined");
                
            var hasCleanupUniTaskPromises = locatorType.GetMethod("CleanupUniTaskPromises");
            Assert.IsNull(hasCleanupUniTaskPromises, 
                "CleanupUniTaskPromises method should not exist when DISABLE_SL_UNITASK is defined or ENABLE_UNITASK is not defined");
			#endif
		}
	}
}
#endif