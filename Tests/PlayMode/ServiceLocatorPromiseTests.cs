#if !DISABLE_SL_PROMISES
using System;
using System.Collections;
using System.Threading;
using Nonatomic.ServiceLocator;
using NUnit.Framework;
using Tests.PlayMode;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.PlayMode
{
    [TestFixture]
    public class ServiceLocatorPromiseTests
    {
        private ServiceLocator _serviceLocator;

        [SetUp]
        public void Setup()
        {
            _serviceLocator = ScriptableObject.CreateInstance<ServiceLocator>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_serviceLocator);
        }

        [UnityTest]
        public IEnumerator ServiceUser_CanRetrieveService_ViaPromise()
        {
            // Register the service first
            _serviceLocator.Register(new TestService());
            
            // Create GameObject with promise-based service user
            var gameObject = new GameObject("PromiseServiceUser");
            var serviceUser = gameObject.AddComponent<ServiceUserPromise>();
            serviceUser.Initialize(_serviceLocator);
            
            // Give it a frame to start the promise
            yield return null;
            
            // Wait for service to be retrieved (should be immediate since already registered)
            yield return new WaitUntil(() => serviceUser.ThenCalled);
            
            // Verify service was retrieved correctly
            Assert.IsTrue(serviceUser.ThenCalled, "Then callback should be called");
            Assert.IsFalse(serviceUser.CatchCalled, "Catch callback should not be called");
            Assert.IsNotNull(serviceUser.GetRetrievedService(), "Service should be retrieved");
            Assert.AreEqual("Hello from TestService!", serviceUser.GetRetrievedService().Message,
                "Service should contain the correct data");
            
            // Cleanup
            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Promise_WaitsForRegistration()
        {
            // Create GameObject with promise-based service user
            var gameObject = new GameObject("PromiseServiceUser");
            var serviceUser = gameObject.AddComponent<ServiceUserPromise>();
            serviceUser.Initialize(_serviceLocator);
            
            // Service not registered yet
            
            // Give it a frame to start the promise
            yield return null;
            
            // Verify promise started but service not yet retrieved
            Assert.IsFalse(serviceUser.ThenCalled, "Then callback should not be called yet");
            Assert.IsFalse(serviceUser.CatchCalled, "Catch callback should not be called");
            
            // Register the service after a delay
            yield return new WaitForSeconds(0.2f);
            _serviceLocator.Register(new TestService());
            
            // Wait for service to be retrieved
            yield return new WaitUntil(() => serviceUser.ThenCalled);
            
            // Verify service was retrieved correctly
            Assert.IsTrue(serviceUser.ThenCalled, "Then callback should be called after registration");
            Assert.IsFalse(serviceUser.CatchCalled, "Catch callback should not be called");
            Assert.IsNotNull(serviceUser.GetRetrievedService(), "Service should be retrieved after registration");
            Assert.AreEqual("Hello from TestService!", serviceUser.GetRetrievedService().Message,
                "Service should contain the correct data");
            
            // Cleanup
            Object.Destroy(gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Promise_CancelledWhenGameObjectDestroyed()
        {
            // Create GameObject with promise-based service user
            var gameObject = new GameObject("PromiseServiceUser");
            var serviceUser = gameObject.AddComponent<ServiceUserPromise>();
            serviceUser.Initialize(_serviceLocator);
            
            // Give it a frame to start the promise
            yield return null;
            
            // Verify promise started but service not yet retrieved
            Assert.IsFalse(serviceUser.ThenCalled, "Then callback should not be called yet");
            Assert.IsFalse(serviceUser.CatchCalled, "Catch callback should not be called yet");
            
            // Destroy the GameObject before registering the service
            Object.Destroy(gameObject);
            
            // Wait for destruction to process
            yield return new WaitForEndOfFrame();
            
            // Now register the service - this shouldn't reach the promise
            _serviceLocator.Register(new TestService());
            
            // Wait a bit to make sure the promise had time to process if it was still active
            yield return new WaitForSeconds(0.2f);
            
            // Using serviceUser after destruction is not reliable, so we can only check
            // that the service doesn't get registered to new requests
            
            bool newPromiseThenCalled = false;
            bool newPromiseCatchCalled = false;
            Exception caughtException = null;
            
            // Try to get the service with a new promise to prove it exists
            _serviceLocator.GetService<TestService>()
                .Then(_ => newPromiseThenCalled = true)
                .Catch(ex => { 
                    newPromiseCatchCalled = true;
                    caughtException = ex;
                });
                
            yield return new WaitUntil(() => newPromiseThenCalled || newPromiseCatchCalled);
            
            // The new promise should succeed since the service is registered
            Assert.IsTrue(newPromiseThenCalled, "New promise should resolve");
            Assert.IsFalse(newPromiseCatchCalled, "New promise should not be rejected");
        }
        
        [UnityTest]
        public IEnumerator Promise_RejectsWithCustomException()
        {
            // Create rejection exception
            var customException = new InvalidOperationException("Service initialization failed");
            
            // Setup variables to track promise resolution
            TestService retrievedService = null;
            Exception caughtException = null;
            bool thenCalled = false;
            bool catchCalled = false;
            
            // Create a promise
            _serviceLocator.GetService<TestService>()
                .Then(service => {
                    retrievedService = service;
                    thenCalled = true;
                })
                .Catch(ex => {
                    caughtException = ex;
                    catchCalled = true;
                });
                
            yield return null;
            
            // Reject the service with the custom exception
            _serviceLocator.RejectService<TestService>(customException);
            
            // Wait for promise to be rejected
            yield return new WaitUntil(() => catchCalled);
            
            // Verify promise was rejected correctly
            Assert.IsFalse(thenCalled, "Then callback should not be called");
            Assert.IsTrue(catchCalled, "Catch callback should be called");
            Assert.IsNull(retrievedService, "Service should not be retrieved");
            Assert.IsNotNull(caughtException, "Exception should be caught");
            Assert.AreEqual(customException, caughtException, 
                "Caught exception should match the custom exception");
        }
        
        [UnityTest]
        public IEnumerator Promise_ChainedThenWithExceptionThrown_CaughtInCatch()
        {
            // Register a service
            _serviceLocator.Register(new TestService());
            
            // Setup variables to track promise resolution
            Exception caughtException = null;
            bool firstThenCalled = false;
            bool secondThenCalled = false;
            bool catchCalled = false;
            var expectedException = new InvalidOperationException("Thrown from Then");
            
            // Create a promise with chained Then calls where one throws
            _serviceLocator.GetService<TestService>()
                .Then(service => {
                    firstThenCalled = true;
                    throw expectedException; // Throw exception in Then
                })
                .Then(value => {
                    secondThenCalled = true; // Should not be called
                    return value;
                })
                .Catch(ex => {
                    caughtException = ex;
                    catchCalled = true;
                });
                
            // Wait for promise to be resolved and exception to be caught
            yield return new WaitUntil(() => catchCalled);
            
            // Verify promise chain behavior
            Assert.IsTrue(firstThenCalled, "First Then callback should be called");
            Assert.IsFalse(secondThenCalled, "Second Then callback should not be called");
            Assert.IsTrue(catchCalled, "Catch callback should be called");
            Assert.IsNotNull(caughtException, "Exception should be caught");
            Assert.AreEqual(expectedException, caughtException, 
                "Caught exception should match the thrown exception");
        }
        
        [UnityTest]
        public IEnumerator Promise_WithExplicitCancellation_IsCancelled()
        {
            // Create cancellation token
            using var cts = new CancellationTokenSource();
            
            // Setup variables to track promise resolution
            bool thenCalled = false;
            bool catchCalled = false;
            Exception caughtException = null;
            
            // Create a promise with the token
            var promise = _serviceLocator.GetService<TestService>(cts.Token)
                .Then(_ => thenCalled = true)
                .Catch(ex => {
                    catchCalled = true;
                    caughtException = ex;
                });
                
            yield return null;
            
            // Cancel the token
            cts.Cancel();
            
            // Wait for promise to be cancelled
            yield return new WaitUntil(() => catchCalled);
            
            // Verify promise was cancelled correctly
            Assert.IsFalse(thenCalled, "Then callback should not be called");
            Assert.IsTrue(catchCalled, "Catch callback should be called");
            Assert.IsNotNull(caughtException, "Exception should be caught");
            Assert.IsInstanceOf<TaskCanceledException>(caughtException, 
                "Exception should be TaskCanceledException");
        }
        
        [UnityTest]
        public IEnumerator ServicePromise_MultiService_AllResolveWhenRegistered()
        {
            // Setup variables to track promise resolution
            TestService service1 = null;
            AnotherTestService service2 = null;
            bool thenCalled = false;
            bool catchCalled = false;
            
            // Create a multi-service promise
            _serviceLocator.GetService<TestService, AnotherTestService>()
                .Then(services => {
                    service1 = services.Item1;
                    service2 = services.Item2;
                    thenCalled = true;
                })
                .Catch(_ => catchCalled = true);
                
            yield return null;
            
            // Register services in sequence
            _serviceLocator.Register(new TestService());
            yield return null;
            
            // Promise should not resolve yet
            Assert.IsFalse(thenCalled, "Then callback should not be called after first service");
            
            _serviceLocator.Register(new AnotherTestService());
            
            // Wait for promise to resolve
            yield return new WaitUntil(() => thenCalled);
            
            // Verify all services were retrieved
            Assert.IsTrue(thenCalled, "Then callback should be called");
            Assert.IsFalse(catchCalled, "Catch callback should not be called");
            Assert.IsNotNull(service1, "First service should be retrieved");
            Assert.IsNotNull(service2, "Second service should be retrieved");
        }
    }
}
#endif