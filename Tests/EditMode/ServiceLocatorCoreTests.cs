using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.EditMode
{
	/// <summary>
	///     Core functionality tests for the ServiceLocator that don't depend on specific
	///     access mechanisms (async, promises, coroutines).
	/// </summary>
	[TestFixture]
	public class ServiceLocatorCoreTests
	{
		[SetUp]
		public void Setup()
		{
			UnitySynchronizationContext.Initialize();
			_serviceLocator = ScriptableObject.CreateInstance<TestServiceLocator>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(_serviceLocator);
		}

		private TestServiceLocator _serviceLocator;

		[Test]
		public void Register_And_TryGetService_Success()
		{
			var service = new TestService();
			_serviceLocator.Register(service);

			Assert.IsTrue(_serviceLocator.TryGetService(out TestService testService));
			Assert.AreEqual(service, testService);
		}

		[Test]
		public void TryGetService_Failure_WhenServiceNotRegistered()
		{
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService? testService));
			Assert.IsNull(testService);
		}

		[Test]
		public void Unregister_RemovesService()
		{
			var service = new TestService();
			_serviceLocator.Register(service);
			_serviceLocator.Unregister<TestService>();

			Assert.IsFalse(_serviceLocator.TryGetService(out TestService testService));
		}

		[Test]
		public void GetServiceOrDefault_ReturnsService_WhenRegistered()
		{
			var service = new TestService();
			_serviceLocator.Register(service);

			var testService = _serviceLocator.GetServiceOrDefault<TestService>();
			Assert.AreEqual(service, testService);
		}

		[Test]
		public void GetServiceOrDefault_ReturnsNull_WhenNotRegistered()
		{
			var testService = _serviceLocator.GetServiceOrDefault<TestService>();
			Assert.IsNull(testService);
		}

		[Test]
		public void Register_ReplacesExistingService()
		{
			var service1 = new TestService();
			var service2 = new TestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);

			Assert.IsTrue(_serviceLocator.TryGetService(out TestService? retrievedService));
			Assert.AreEqual(service2, retrievedService);
			Assert.AreNotEqual(service1, retrievedService);
		}

		[Test]
		public void CleanupServiceLocator_ClearsAllServices()
		{
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);

			_serviceLocator.Cleanup();

			Assert.IsFalse(_serviceLocator.TryGetService(out TestService retrievedService1));
			Assert.IsFalse(_serviceLocator.TryGetService(out AnotherTestService retrievedService2));
		}

		[Test]
		public void ServiceLocator_CleanupDisposesServices()
		{
			var disposableService = new DisposableTestService();
			_serviceLocator.Register(disposableService);

			_serviceLocator.Cleanup();

			// Verify that the Dispose method was called
			Assert.IsTrue(disposableService.Disposed, "Service should be disposed during cleanup");
		}

		[Test]
		public void ServiceLocator_RemovesAllServiceReferencesAfterCleanup()
		{
			var service = new TestService();
			_serviceLocator.Register(service);

			_serviceLocator.Cleanup();

			// Test that ServiceMap is empty after cleanup
			var services = _serviceLocator.GetAllServices();
			Assert.AreEqual(0, services.Count, "ServiceMap should be empty after cleanup");

			// Test that we can't retrieve the service anymore
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService _),
				"Service should not be retrievable after cleanup");
		}

		[Test]
		public void InitializeAndDeInitializeServiceLocator_ChangesState()
		{
			// Initially, the ServiceLocator should not be initialized
			Assert.IsFalse(_serviceLocator.IsInitialized);

			_serviceLocator.ForceInitialize();
			// After initialization, scene cleanup should be registered
			Assert.IsTrue(_serviceLocator.IsInitialized);

			_serviceLocator.ForceDeInitialize();
			// After de-initialization, scene cleanup should not be registered
			Assert.IsFalse(_serviceLocator.IsInitialized);
		}

		[Test]
		public void DeInitialize_AndThenInitialize_RestoresServiceLocator()
		{
			_serviceLocator.ForceInitialize();
			_serviceLocator.Register(new TestService());

			_serviceLocator.ForceDeInitialize();
			// Services should be cleared after DeInitialize
			Assert.IsFalse(_serviceLocator.TryGetService(out TestService _));

			// Re-initialize should restore functionality
			_serviceLocator.ForceInitialize();
			_serviceLocator.Register(new TestService());
			Assert.IsTrue(_serviceLocator.TryGetService(out TestService _));
		}

		[Test]
		public void Register_NullService_ThrowsException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				_serviceLocator.Register<TestService>(null)
			);
		}

		[Test]
		public void TryGetService_WhileUnregistering_ReturnsConsistentState()
		{
			var service = new TestService();
			_serviceLocator.Register(service);

			Parallel.Invoke(
				() =>
				{
					for (var i = 0; i < 1000; i++)
					{
						_serviceLocator.TryGetService(out TestService _);
					}
				},
				() =>
				{
					for (var i = 0; i < 1000; i++)
					{
						_serviceLocator.Unregister<TestService>();
					}
				}
			);
		}

		[Test]
		public void MultipleServiceRegistrationsAtOnce_Succeeds()
		{
			Parallel.Invoke(
				() => _serviceLocator.Register(new TestService()),
				() => _serviceLocator.Register(new AnotherTestService()),
				() => _serviceLocator.Register(new ThirdTestService())
			);

			Assert.IsTrue(_serviceLocator.TryGetService(out TestService _));
			Assert.IsTrue(_serviceLocator.TryGetService(out AnotherTestService _));
			Assert.IsTrue(_serviceLocator.TryGetService(out ThirdTestService _));
		}

		[Test]
		public void Register_WithInterface_CanRetrieveByInterface()
		{
			var service = new InterfaceImplementingService();
			_serviceLocator.Register<ITestServiceInterface>(service);

			Assert.IsTrue(_serviceLocator.TryGetService(out ITestServiceInterface retrievedService));
			Assert.AreEqual(service, retrievedService);
		}

		[Test]
		public void Register_ByBaseType_ThenTryGet_ByDerivedType_ShouldFail()
		{
			var baseService = new BaseTestService();
			_serviceLocator.Register(baseService);

			// Should not be able to get a DerivedTestService when a BaseTestService was registered
			Assert.IsFalse(_serviceLocator.TryGetService(out DerivedTestService _));
		}

		#if UNITY_EDITOR
		// These tests verify that the code behaves correctly based on preprocessor directives
		[Test]
		public void ConfiguredFeatures_AreConsistentWithPreprocessorDirectives()
		{
			// Core features always available
			Assert.DoesNotThrow(() => _serviceLocator.Register(new TestService()));
			Assert.DoesNotThrow(() => _serviceLocator.TryGetService(out TestService _));
			Assert.DoesNotThrow(() => _serviceLocator.Unregister<TestService>());
			Assert.DoesNotThrow(() => _serviceLocator.GetServiceOrDefault<TestService>());
			Assert.DoesNotThrow(() => _serviceLocator.GetAllServices());

			// Check if methods exist based on preprocessor directives
			var locatorType = typeof(BaseServiceLocator);

			// Instead of looking for specific methods by name and parameter types,
			// check for the existence of methods with specific patterns

			#if ENABLE_SL_ASYNC || !DISABLE_SL_ASYNC
			// Look for any method that starts with "GetServiceAsync"
			var hasAsyncMethods = locatorType.GetMethods()
				.Any(m => m.Name.StartsWith("GetServiceAsync"));
			Assert.IsTrue(hasAsyncMethods,
				"GetServiceAsync methods should exist when ENABLE_SL_ASYNC is defined or DISABLE_SL_ASYNC is not defined");
			#else
            // Look for any method that starts with "GetServiceAsync"
            var hasAsyncMethods = locatorType.GetMethods()
                .Any(m => m.Name.StartsWith("GetServiceAsync"));
            Assert.IsFalse(hasAsyncMethods, "GetServiceAsync methods should not exist when ENABLE_SL_ASYNC is not defined and DISABLE_SL_ASYNC is defined");
			#endif

			#if ENABLE_SL_UNITASK
			// Look for any method that starts with "GetServiceAsync" and returns UniTask
			var hasUniTaskMethods = locatorType.GetMethods()
				.Any(m => m.Name.StartsWith("GetServiceAsync") && 
				          m.ReturnType.FullName != null && 
				          m.ReturnType.FullName.Contains("UniTask"));
			Assert.IsTrue(hasUniTaskMethods, "GetServiceAsync methods returning UniTask should exist when ENABLE_SL_UNITASK is defined");
			#endif

			#if ENABLE_SL_PROMISES || !DISABLE_SL_PROMISES
			// Look for any GetService method that returns IServicePromise
			var hasPromiseMethods = locatorType.GetMethods()
				.Any(m => m.Name == "GetService" &&
						  m.ReturnType.IsGenericType &&
						  m.ReturnType.GetGenericTypeDefinition().Name.StartsWith("IServicePromise"));
			Assert.IsTrue(hasPromiseMethods,
				"GetService methods returning IServicePromise should exist when ENABLE_SL_PROMISES is defined or DISABLE_SL_PROMISES is not defined");
			#else
			// Look for any GetService method that returns IServicePromise
			var hasPromiseMethods = locatorType.GetMethods()
				.Any(m => m.Name == "GetService" &&
						  m.ReturnType.IsGenericType &&
						  m.ReturnType.GetGenericTypeDefinition().Name.StartsWith("IServicePromise"));
			Assert.IsFalse(hasPromiseMethods,
				"GetService methods returning IServicePromise should not exist when ENABLE_SL_PROMISES is not defined and DISABLE_SL_PROMISES is defined");
			#endif

			#if ENABLE_SL_COROUTINES || !DISABLE_SL_COROUTINES
			// Look for any GetServiceCoroutine method
			var hasCoroutineMethods = locatorType.GetMethods()
				.Any(m => m.Name == "GetServiceCoroutine");
			Assert.IsTrue(hasCoroutineMethods,
				"GetServiceCoroutine methods should exist when ENABLE_SL_COROUTINES is defined or DISABLE_SL_COROUTINES is not defined");
			#else
			// Look for any GetServiceCoroutine method
			var hasCoroutineMethods = locatorType.GetMethods()
				.Any(m => m.Name == "GetServiceCoroutine");
			Assert.IsFalse(hasCoroutineMethods,
				"GetServiceCoroutine methods should not exist when ENABLE_SL_COROUTINES is not defined and DISABLE_SL_COROUTINES is defined");
			#endif

			#if ENABLE_SL_SCENE_TRACKING || !DISABLE_SL_SCENE_TRACKING
			// Look for scene-related methods
			var hasSceneTrackingMethods = locatorType.GetMethods()
				.Any(m => m.Name == "GetSceneNameForService" || m.Name == "UnregisterServicesFromScene");
			Assert.IsTrue(hasSceneTrackingMethods,
				"Scene tracking methods should exist when ENABLE_SL_SCENE_TRACKING is defined or DISABLE_SL_SCENE_TRACKING is not defined");
			#else
            // Look for scene-related methods
            bool hasSceneTrackingMethods = locatorType.GetMethods()
                .Any(m => m.Name == "GetSceneNameForService" || m.Name == "UnregisterServicesFromScene");
            Assert.IsFalse(hasSceneTrackingMethods, "Scene tracking methods should not exist when ENABLE_SL_SCENE_TRACKING is not defined and DISABLE_SL_SCENE_TRACKING is defined");
			#endif
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

		private class TestService
		{
		}

		private class AnotherTestService
		{
		}

		private class ThirdTestService
		{
		}

		private class BaseTestService
		{
		}

		private class DerivedTestService : BaseTestService
		{
		}

		private interface ITestServiceInterface
		{
		}

		private class InterfaceImplementingService : ITestServiceInterface
		{
		}

		private class DisposableTestService : IDisposable
		{
			public bool Disposed { get; private set; }

			public void Dispose()
			{
				Disposed = true;
			}
		}

		#if ENABLE_SL_SCENE_TRACKING || !DISABLE_SL_SCENE_TRACKING
		[UnityTest]
		public IEnumerator SceneUnload_RemovesSceneSpecificServices()
		{
			// Use the scene tracking test helper instead of regular test service locator
			var sceneTrackingLocator = ScriptableObject.CreateInstance<SceneTrackingTestLocator>();

			try
			{
				// Register a service
				var service = new TestService();
				sceneTrackingLocator.Register(service);

				// Manually associate with scene (this is a test-only method)
				const string testSceneName = "TestScene";
				sceneTrackingLocator.AssociateWithScene(typeof(TestService), testSceneName);

				// Verify service is registered
				Assert.IsTrue(sceneTrackingLocator.TryGetService(out TestService _));

				// Simulate scene unloading
				sceneTrackingLocator.UnregisterServicesFromScene(testSceneName);

				// Verify service is removed
				Assert.IsFalse(sceneTrackingLocator.TryGetService(out TestService _),
					"Service should be unregistered when its scene is unloaded");

				yield return null;
			}
			finally
			{
				// Clean up
				Object.DestroyImmediate(sceneTrackingLocator);
			}
		}

		// Special helper class that only exists when scene tracking is enabled
		private class SceneTrackingTestLocator : ServiceLocator
		{
			public void AssociateWithScene(Type serviceType, string sceneName)
			{
				// Direct access to the protected scene map
				lock (Lock)
				{
					ServiceSceneMap[serviceType] = sceneName;
				}
			}
		}
		#endif
	}
}