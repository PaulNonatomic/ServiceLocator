#if ENABLE_SL_COROUTINES
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nonatomic.ServiceLocator
{
    public abstract partial class BaseServiceLocator
    {
        [NonSerialized] protected readonly List<(Type, Action<object>)> PendingCoroutines = new();

        /// <summary>
        ///     Retrieves a service using a coroutine.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <param name="callback">Action to be called with the retrieved service.</param>
        /// <returns>An IEnumerator for use with StartCoroutine.</returns>
        public virtual IEnumerator GetServiceCoroutine<T>(Action<T?> callback) where T : class
        {
            var serviceType = typeof(T);
            var isPending = false;
            Action<object?> wrappedCallback = null!;

            lock (Lock)
            {
                if (ServiceMap.TryGetValue(serviceType, out var service))
                {
                    callback((T)service);
                    yield break;
                }

                wrappedCallback = obj => callback(obj as T);
                PendingCoroutines.Add((serviceType, wrappedCallback));
                isPending = true;
            }

            while (isPending)
            {
                lock (Lock)
                {
                    if (ServiceMap.TryGetValue(serviceType, out var service))
                    {
                        PendingCoroutines.RemoveAll(x => x.Item1 == serviceType && x.Item2 == wrappedCallback);
                        callback((T)service);
                        yield break;
                    }

                    if (!PendingCoroutines.Any(x => x.Item1 == serviceType && x.Item2 == wrappedCallback))
                    {
                        isPending = false;
                        yield break;
                    }
                }

                yield return null;
            }
        }

        /// <summary>
        ///     Retrieves multiple services using a coroutine.
        /// </summary>
        /// <typeparam name="T1">The first service type.</typeparam>
        /// <typeparam name="T2">The second service type.</typeparam>
        /// <param name="callback">Action to be called with the retrieved services.</param>
        /// <returns>An IEnumerator for use with StartCoroutine.</returns>
        public virtual IEnumerator GetServiceCoroutine<T1, T2>(Action<T1?, T2?> callback)
            where T1 : class
            where T2 : class
        {
            T1? service1 = null;
            T2? service2 = null;
            var service1Ready = false;
            var service2Ready = false;

            var coroutine1 = GetServiceCoroutine<T1>(s =>
            {
                service1 = s;
                service1Ready = true;
            });

            var coroutine2 = GetServiceCoroutine<T2>(s =>
            {
                service2 = s;
                service2Ready = true;
            });

            while (!(service1Ready && service2Ready))
            {
                if (!service1Ready)
                {
                    coroutine1.MoveNext();
                }

                if (!service2Ready)
                {
                    coroutine2.MoveNext();
                }

                yield return null;
            }

            callback(service1, service2);
        }

        /// <summary>
        ///     Retrieves multiple services using a coroutine.
        /// </summary>
        /// <typeparam name="T1">The first service type.</typeparam>
        /// <typeparam name="T2">The second service type.</typeparam>
        /// <typeparam name="T3">The third service type.</typeparam>
        /// <param name="callback">Action to be called with the retrieved services.</param>
        /// <returns>An IEnumerator for use with StartCoroutine.</returns>
        public virtual IEnumerator GetServiceCoroutine<T1, T2, T3>(Action<T1?, T2?, T3?> callback)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            T1? service1 = null;
            T2? service2 = null;
            T3? service3 = null;
            var service1Ready = false;
            var service2Ready = false;
            var service3Ready = false;

            var coroutine1 = GetServiceCoroutine<T1>(s =>
            {
                service1 = s;
                service1Ready = true;
            });

            var coroutine2 = GetServiceCoroutine<T2>(s =>
            {
                service2 = s;
                service2Ready = true;
            });

            var coroutine3 = GetServiceCoroutine<T3>(s =>
            {
                service3 = s;
                service3Ready = true;
            });

            while (!(service1Ready && service2Ready && service3Ready))
            {
                if (!service1Ready)
                {
                    coroutine1.MoveNext();
                }

                if (!service2Ready)
                {
                    coroutine2.MoveNext();
                }

                if (!service3Ready)
                {
                    coroutine3.MoveNext();
                }

                yield return null;
            }

            callback(service1, service2, service3);
        }

        // Implementation of helper methods

        /// <summary>
        ///     Resolves pending coroutines for a specific service type.
        /// </summary>
        protected override void ResolvePendingCoroutines(Type serviceType, object service)
        {
            var pendingCoroutines =
 PendingCoroutines.FindAll(pendingCoroutine => pendingCoroutine.Item1 == serviceType);
            foreach (var (_, callback) in pendingCoroutines)
            {
                callback(service);
            }

            PendingCoroutines.RemoveAll(pendingCoroutine => pendingCoroutine.Item1 == serviceType);
        }

        /// <summary>
        ///     Cancels all pending coroutines.
        /// </summary>
        protected override void CancelPendingCoroutines()
        {
            foreach (var (_, callback) in PendingCoroutines)
            {
                callback(null!);
            }

            PendingCoroutines.Clear();
        }
    }
}
#endif