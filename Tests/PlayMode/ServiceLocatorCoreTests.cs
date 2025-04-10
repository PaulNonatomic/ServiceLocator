using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
	
	public class ServiceLocatorCoreTests
	{
		private TestServiceLocator _serviceLocator;

		[SetUp]
		public void Setup()
		{
			UnitySynchronizationContext.Initialize();
			_serviceLocator = ScriptableObject.CreateInstance<TestServiceLocator>();
		}
		
		#if !DISABLE_SL_SCENE_TRACKING
		[UnityTest, Timeout(5000)] // Add a 5-second timeout
		public IEnumerator SceneUnload_RemovesSceneSpecificServices()
		{
			// Mock scene unloading by directly calling the handler
			var service = new TestService();
			_serviceLocator.Register(service);

			// Get the scene name from the service map
			var sceneName = _serviceLocator.GetSceneNameForService(typeof(TestService));

			// Simulate scene unloading
			_serviceLocator.UnregisterServicesFromScene(sceneName);

			// Check that the service is unregistered
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService _),
				"Service should be unregistered when its scene is unloaded");

			// This is important to end the coroutine
			yield return null;
		}
		#endif
		
		// Helper class to expose protected methods for testing
		private class TestServiceLocator : BaseServiceLocator
		{
			public new void OnEnable()
			{
				base.OnEnable();
			}

			public new void OnDisable()
			{
				base.OnDisable();
			}

			public void ForceInitialize()
			{
				Initialize();
				IsInitialized = true;
			}

			public void ForceDeInitialize()
			{
				DeInitialize();
				IsInitialized = false;
			}

			// Override to prevent automatic initialization
			protected override void Initialize()
			{
				// Do nothing, allowing manual control in tests
			}
		}
	}
}