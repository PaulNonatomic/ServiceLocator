using System.Collections;
using System.Threading.Tasks;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.EditMode.FluentTests
{
	/// <summary>
	///     Tests for the fluent API extensions to ServiceLocator.
	/// </summary>
	[TestFixture]
	public class ServiceLocatorFluentTests
	{
		[SetUp]
		public void Setup()
		{
			UnitySynchronizationContext.Initialize();
			_serviceLocator = ScriptableObject.CreateInstance<TestServiceLocator>();
			_serviceLocator.ForceInitialize();
		}

		[TearDown]
		public void TearDown()
		{
			_serviceLocator.ForceDeInitialize();
			Object.DestroyImmediate(_serviceLocator);
		}

		private TestServiceLocator _serviceLocator;

		[Test]
		public async Task GetAsync_SingleService_ReturnsCorrectService()
		{
			// Arrange
			var service = new TestService();
			_serviceLocator.Register(service);

			// Act
			var result = await _serviceLocator
				.GetAsync<TestService>()
				.WithCancellation();

			// Assert
			Assert.AreEqual(service, result);
		}

		[Test]
		public async Task GetAsync_TwoServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);

			// Act
			var (result1, result2) = await _serviceLocator
				.GetAsync<TestService>()
				.AndAsync<AnotherTestService>()
				.WithCancellation();

			// Assert
			Assert.AreEqual(service1, result1);
			Assert.AreEqual(service2, result2);
		}

		[Test]
		public async Task GetAsync_ThreeServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);

			// Act
			var (result1, result2, result3) = await _serviceLocator
				.GetAsync<TestService>()
				.AndAsync<AnotherTestService>()
				.AndAsync<ThirdTestService>()
				.WithCancellation();

			// Assert
			Assert.AreEqual(service1, result1);
			Assert.AreEqual(service2, result2);
			Assert.AreEqual(service3, result3);
		}

		[Test]
		public async Task GetAsync_FourServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			var service4 = new TestService(); // Reusing the type, but a different instance
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			_serviceLocator.Register(service4); // Will replace service1

			// Act
			var (result1, result2, result3, result4) = await _serviceLocator
				.GetAsync<TestService>()
				.AndAsync<AnotherTestService>()
				.AndAsync<ThirdTestService>()
				.AndAsync<TestService>() // Same as result1, but should be service4
				.WithCancellation();

			// Assert
			Assert.AreEqual(service4, result1); // service4 replaces service1
			Assert.AreEqual(service2, result2);
			Assert.AreEqual(service3, result3);
			Assert.AreEqual(service4, result4); // Same as result1
		}

		[Test]
		public async Task GetAsync_FiveServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			var service4 = new TestService(); // Reusing the type, but a different instance
			var service5 = new AnotherTestService(); // Reusing the type, but a different instance
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			_serviceLocator.Register(service4); // Will replace service1
			_serviceLocator.Register(service5); // Will replace service2

			// Act
			var (result1, result2, result3, result4, result5) = await _serviceLocator
				.GetAsync<TestService>()
				.AndAsync<AnotherTestService>()
				.AndAsync<ThirdTestService>()
				.AndAsync<TestService>() // Same as result1, but should be service4
				.AndAsync<AnotherTestService>() // Same as result2, but should be service5
				.WithCancellation();

			// Assert
			Assert.AreEqual(service4, result1); // service4 replaces service1
			Assert.AreEqual(service5, result2); // service5 replaces service2
			Assert.AreEqual(service3, result3);
			Assert.AreEqual(service4, result4); // Same as result1
			Assert.AreEqual(service5, result5); // Same as result2
		}

		[Test]
		public async Task GetAsync_SixServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			var service4 = new TestService(); // Reusing the type, but a different instance
			var service5 = new AnotherTestService(); // Reusing the type, but a different instance
			var service6 = new ThirdTestService(); // Reusing the type, but a different instance
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			_serviceLocator.Register(service4); // Will replace service1
			_serviceLocator.Register(service5); // Will replace service2
			_serviceLocator.Register(service6); // Will replace service3

			// Act
			var (result1, result2, result3, result4, result5, result6) = await _serviceLocator
				.GetAsync<TestService>()
				.AndAsync<AnotherTestService>()
				.AndAsync<ThirdTestService>()
				.AndAsync<TestService>() // Same as result1, but should be service4
				.AndAsync<AnotherTestService>() // Same as result2, but should be service5
				.AndAsync<ThirdTestService>() // Same as result3, but should be service6
				.WithCancellation();

			// Assert
			Assert.AreEqual(service4, result1); // service4 replaces service1
			Assert.AreEqual(service5, result2); // service5 replaces service2
			Assert.AreEqual(service6, result3); // service6 replaces service3
			Assert.AreEqual(service4, result4); // Same as result1
			Assert.AreEqual(service5, result5); // Same as result2
			Assert.AreEqual(service6, result6); // Same as result3
		}

		[Test]
		public async Task GetAsync_UnregisteredService_WaitsForRegistration()
		{
			// Arrange
			var task = _serviceLocator
				.GetAsync<TestService>()
				.WithCancellation();

			// Act - Register the service after starting the task
			var service = new TestService();
			_serviceLocator.Register(service);
			var result = await task;

			// Assert
			Assert.AreEqual(service, result);
		}

		#if !DISABLE_SL_COROUTINES
		[UnityTest]
		public IEnumerator GetCoroutine_UnregisteredService_WaitsForRegistration()
		{
			// Arrange
			TestService result = null;
			var registerLater = true;

			// Start coroutine but don't yield it yet
			var coroutine = _serviceLocator
				.GetCoroutine<TestService>()
				.WithCallback(s =>
				{
					result = s;
					registerLater = false;
				});

			// Start a separate coroutine to register the service after a delay
			var service = new TestService();
			MonoBehaviour monoBehaviour = new GameObject().AddComponent<MonoBehaviourHelper>();
			monoBehaviour.StartCoroutine(RegisterAfterDelay(service));

			// Now yield the service coroutine
			yield return coroutine;

			// Assert
			Assert.NotNull(result);
			Assert.AreEqual(service, result);

			// Cleanup - use DestroyImmediate for edit mode
			Object.DestroyImmediate(monoBehaviour.gameObject);

			IEnumerator RegisterAfterDelay(TestService serviceToRegister)
			{
				yield return new WaitForSeconds(0.1f);
				_serviceLocator.Register(serviceToRegister);
			}
		}
		#endif

		#if !DISABLE_SL_PROMISES
		[Test]
		public void Get_SingleService_ReturnsCorrectService()
		{
			// Arrange
			var service = new TestService();
			_serviceLocator.Register(service);
			TestService result = null;

			// Act
			_serviceLocator
				.Get<TestService>()
				.WithCancellation()
				.Then(s =>
				{
					result = s;
					return true;
				});

			// Assert - wait a frame for the promise to resolve
			Assert.NotNull(result);
			Assert.AreEqual(service, result);
		}

		[Test]
		public void Get_TwoServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			TestService result1 = null;
			AnotherTestService result2 = null;

			// Act
			_serviceLocator
				.Get<TestService>()
				.And<AnotherTestService>()
				.WithCancellation()
				.Then(tuple =>
				{
					var (s1, s2) = tuple;
					result1 = s1;
					result2 = s2;
					return true;
				});

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.AreEqual(service1, result1);
			Assert.AreEqual(service2, result2);
		}

		[Test]
		public void Get_ThreeServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			TestService result1 = null;
			AnotherTestService result2 = null;
			ThirdTestService result3 = null;

			// Act
			_serviceLocator
				.Get<TestService>()
				.And<AnotherTestService>()
				.And<ThirdTestService>()
				.WithCancellation()
				.Then(tuple =>
				{
					var (s1, s2, s3) = tuple;
					result1 = s1;
					result2 = s2;
					result3 = s3;
					return true;
				});

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.NotNull(result3);
			Assert.AreEqual(service1, result1);
			Assert.AreEqual(service2, result2);
			Assert.AreEqual(service3, result3);
		}

		[Test]
		public void Get_FourServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			var service4 = new TestService(); // Reusing the type, but a different instance
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			_serviceLocator.Register(service4); // Will replace service1

			TestService result1 = null;
			AnotherTestService result2 = null;
			ThirdTestService result3 = null;
			TestService result4 = null;

			// Act
			_serviceLocator
				.Get<TestService>()
				.And<AnotherTestService>()
				.And<ThirdTestService>()
				.And<TestService>() // Same as result1, but should be service4
				.WithCancellation()
				.Then(tuple =>
				{
					var (s1, s2, s3, s4) = tuple;
					result1 = s1;
					result2 = s2;
					result3 = s3;
					result4 = s4;
					return true;
				});

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.NotNull(result3);
			Assert.NotNull(result4);
			Assert.AreEqual(service4, result1); // service4 replaces service1
			Assert.AreEqual(service2, result2);
			Assert.AreEqual(service3, result3);
			Assert.AreEqual(service4, result4); // Same as result1
		}

		[Test]
		public void Get_FiveServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			var service4 = new TestService(); // Reusing the type, but a different instance
			var service5 = new AnotherTestService(); // Reusing the type, but a different instance
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			_serviceLocator.Register(service4); // Will replace service1
			_serviceLocator.Register(service5); // Will replace service2

			TestService result1 = null;
			AnotherTestService result2 = null;
			ThirdTestService result3 = null;
			TestService result4 = null;
			AnotherTestService result5 = null;

			// Act
			_serviceLocator
				.Get<TestService>()
				.And<AnotherTestService>()
				.And<ThirdTestService>()
				.And<TestService>() // Same as result1, but should be service4
				.And<AnotherTestService>() // Same as result2, but should be service5
				.WithCancellation()
				.Then(tuple =>
				{
					var (s1, s2, s3, s4, s5) = tuple;
					result1 = s1;
					result2 = s2;
					result3 = s3;
					result4 = s4;
					result5 = s5;
					return true;
				});

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.NotNull(result3);
			Assert.NotNull(result4);
			Assert.NotNull(result5);
			Assert.AreEqual(service4, result1); // service4 replaces service1
			Assert.AreEqual(service5, result2); // service5 replaces service2
			Assert.AreEqual(service3, result3);
			Assert.AreEqual(service4, result4); // Same as result1
			Assert.AreEqual(service5, result5); // Same as result2
		}

		[Test]
		public void Get_SixServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			var service4 = new TestService(); // Reusing the type, but a different instance
			var service5 = new AnotherTestService(); // Reusing the type, but a different instance
			var service6 = new ThirdTestService(); // Reusing the type, but a different instance
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			_serviceLocator.Register(service4); // Will replace service1
			_serviceLocator.Register(service5); // Will replace service2
			_serviceLocator.Register(service6); // Will replace service3

			TestService result1 = null;
			AnotherTestService result2 = null;
			ThirdTestService result3 = null;
			TestService result4 = null;
			AnotherTestService result5 = null;
			ThirdTestService result6 = null;

			// Act
			_serviceLocator
				.Get<TestService>()
				.And<AnotherTestService>()
				.And<ThirdTestService>()
				.And<TestService>() // Same as result1, but should be service4
				.And<AnotherTestService>() // Same as result2, but should be service5
				.And<ThirdTestService>() // Same as result3, but should be service6
				.WithCancellation()
				.Then(tuple =>
				{
					var (s1, s2, s3, s4, s5, s6) = tuple;
					result1 = s1;
					result2 = s2;
					result3 = s3;
					result4 = s4;
					result5 = s5;
					result6 = s6;
					return true;
				});

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.NotNull(result3);
			Assert.NotNull(result4);
			Assert.NotNull(result5);
			Assert.NotNull(result6);
			Assert.AreEqual(service4, result1); // service4 replaces service1
			Assert.AreEqual(service5, result2); // service5 replaces service2
			Assert.AreEqual(service6, result3); // service6 replaces service3
			Assert.AreEqual(service4, result4); // Same as result1
			Assert.AreEqual(service5, result5); // Same as result2
			Assert.AreEqual(service6, result6); // Same as result3
		}
		#endif

		#if !DISABLE_SL_COROUTINES
		[UnityTest]
		public IEnumerator GetCoroutine_SingleService_ReturnsCorrectService()
		{
			// Arrange
			var service = new TestService();
			_serviceLocator.Register(service);
			TestService result = null;

			// Act
			yield return _serviceLocator
				.GetCoroutine<TestService>()
				.WithCallback(s => { result = s; });

			// Assert
			Assert.NotNull(result);
			Assert.AreEqual(service, result);
		}

		[UnityTest]
		public IEnumerator GetCoroutine_TwoServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			TestService result1 = null;
			AnotherTestService result2 = null;

			// Act
			yield return _serviceLocator
				.GetCoroutine<TestService>()
				.And<AnotherTestService>()
				.WithCallback((s1, s2) =>
				{
					result1 = s1;
					result2 = s2;
				});

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.AreEqual(service1, result1);
			Assert.AreEqual(service2, result2);
		}

		[UnityTest]
		public IEnumerator GetCoroutine_ThreeServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			TestService result1 = null;
			AnotherTestService result2 = null;
			ThirdTestService result3 = null;

			// Act
			yield return _serviceLocator
				.GetCoroutine<TestService>()
				.And<AnotherTestService>()
				.And<ThirdTestService>()
				.WithCallback((s1, s2, s3) =>
				{
					result1 = s1;
					result2 = s2;
					result3 = s3;
				});

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.NotNull(result3);
			Assert.AreEqual(service1, result1);
			Assert.AreEqual(service2, result2);
			Assert.AreEqual(service3, result3);
		}

		[UnityTest]
		public IEnumerator GetCoroutine_FourServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			var service4 = new TestService(); // Reusing the type, but a different instance
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			_serviceLocator.Register(service4); // Will replace service1

			TestService result1 = null;
			AnotherTestService result2 = null;
			ThirdTestService result3 = null;
			TestService result4 = null;

			// Act
			yield return _serviceLocator
				.GetCoroutine<TestService>()
				.And<AnotherTestService>()
				.And<ThirdTestService>()
				.And<TestService>() // Same as result1, but should be service4
				.WithCallback((s1, s2, s3, s4) =>
				{
					result1 = s1;
					result2 = s2;
					result3 = s3;
					result4 = s4;
				});

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.NotNull(result3);
			Assert.NotNull(result4);
			Assert.AreEqual(service4, result1); // service4 replaces service1
			Assert.AreEqual(service2, result2);
			Assert.AreEqual(service3, result3);
			Assert.AreEqual(service4, result4); // Same as result1
		}

		[UnityTest]
		public IEnumerator GetCoroutine_FiveServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			var service4 = new TestService(); // Reusing the type, but a different instance
			var service5 = new AnotherTestService(); // Reusing the type, but a different instance
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			_serviceLocator.Register(service4); // Will replace service1
			_serviceLocator.Register(service5); // Will replace service2

			TestService result1 = null;
			AnotherTestService result2 = null;
			ThirdTestService result3 = null;
			TestService result4 = null;
			AnotherTestService result5 = null;

			// Act
			yield return _serviceLocator
				.GetCoroutine<TestService>()
				.And<AnotherTestService>()
				.And<ThirdTestService>()
				.And<TestService>() // Same as result1, but should be service4
				.And<AnotherTestService>() // Same as result2, but should be service5
				.WithCallback((s1, s2, s3, s4, s5) =>
				{
					result1 = s1;
					result2 = s2;
					result3 = s3;
					result4 = s4;
					result5 = s5;
				});

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.NotNull(result3);
			Assert.NotNull(result4);
			Assert.NotNull(result5);
			Assert.AreEqual(service4, result1); // service4 replaces service1
			Assert.AreEqual(service5, result2); // service5 replaces service2
			Assert.AreEqual(service3, result3);
			Assert.AreEqual(service4, result4); // Same as result1
			Assert.AreEqual(service5, result5); // Same as result2
		}

		[UnityTest]
		public IEnumerator GetCoroutine_SixServices_ReturnsCorrectServices()
		{
			// Arrange
			var service1 = new TestService();
			var service2 = new AnotherTestService();
			var service3 = new ThirdTestService();
			var service4 = new TestService(); // Reusing the type, but a different instance
			var service5 = new AnotherTestService(); // Reusing the type, but a different instance
			var service6 = new ThirdTestService(); // Reusing the type, but a different instance
			_serviceLocator.Register(service1);
			_serviceLocator.Register(service2);
			_serviceLocator.Register(service3);
			_serviceLocator.Register(service4); // Will replace service1
			_serviceLocator.Register(service5); // Will replace service2
			_serviceLocator.Register(service6); // Will replace service3

			TestService result1 = null;
			AnotherTestService result2 = null;
			ThirdTestService result3 = null;
			TestService result4 = null;
			AnotherTestService result5 = null;
			ThirdTestService result6 = null;

			// Act
			yield return _serviceLocator
				.GetCoroutine<TestService>()
				.And<AnotherTestService>()
				.And<ThirdTestService>()
				.And<TestService>() // Same as result1, but should be service4
				.And<AnotherTestService>() // Same as result2, but should be service5
				.And<ThirdTestService>() // Same as result3, but should be service6
				.WithCallback((s1, s2, s3, s4, s5, s6) =>
				{
					result1 = s1;
					result2 = s2;
					result3 = s3;
					result4 = s4;
					result5 = s5;
					result6 = s6;
				});

			// Assert
			Assert.NotNull(result1);
			Assert.NotNull(result2);
			Assert.NotNull(result3);
			Assert.NotNull(result4);
			Assert.NotNull(result5);
			Assert.NotNull(result6);
			Assert.AreEqual(service4, result1); // service4 replaces service1
			Assert.AreEqual(service5, result2); // service5 replaces service2
			Assert.AreEqual(service6, result3); // service6 replaces service3
			Assert.AreEqual(service4, result4); // Same as result1
			Assert.AreEqual(service5, result5); // Same as result2
			Assert.AreEqual(service6, result6); // Same as result3
		}
		#endif
	}

	// Helper MonoBehaviour for coroutine tests
	public class MonoBehaviourHelper : MonoBehaviour
	{
	}
}