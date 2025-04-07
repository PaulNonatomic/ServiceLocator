#if ENABLE_SL_UNITASK && !ENABLE_SL_ASYNC

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Nonatomic.ServiceLocator
{
    public abstract partial class BaseServiceLocator
    {
        [NonSerialized] protected readonly Dictionary<Type, List<UniTaskCompletionSource<object>>> UniTaskPromiseMap = new();

        /// <summary>
        ///     Asynchronously retrieves a service of the specified type using UniTask.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>A UniTask that resolves to the requested service instance.</returns>
        public virtual async UniTask<T> GetServiceAsync<T>(CancellationToken cancellation = default) where T : class
        {
            var serviceType = typeof(T);
            UniTaskCompletionSource<object> taskCompletion;

            lock (Lock)
            {
                if (ServiceMap.TryGetValue(serviceType, out var service))
                {
                    return (T)service;
                }

                taskCompletion = new UniTaskCompletionSource<object>();
                if (!UniTaskPromiseMap.TryGetValue(serviceType, out var taskList))
                {
                    taskList = new();
                    UniTaskPromiseMap[serviceType] = taskList;
                }

                taskList.Add(taskCompletion);
            }

            if (cancellation.CanBeCanceled)
            {
                cancellation.Register(() =>
                {
                    lock (Lock)
                    {
                        if (!UniTaskPromiseMap.TryGetValue(serviceType, out var taskList) ||
                            !taskList.Contains(taskCompletion))
                        {
                            return;
                        }

                        taskCompletion.TrySetCanceled();
                        taskList.Remove(taskCompletion);
                        if (taskList.Count != 0)
                        {
                            return;
                        }

                        UniTaskPromiseMap.Remove(serviceType);
                    }
                });
            }

            return (T)await taskCompletion.Task;
        }

        /// <summary>
        ///     Asynchronously retrieves two services of the specified types.
        /// </summary>
        public virtual UniTask<(T1, T2)> GetServiceAsync<T1, T2>(CancellationToken cancellation = default)
            where T1 : class
            where T2 : class
        {
            return GetServicesAsync<T1, T2>(cancellation);
        }

        /// <summary>
        ///     Asynchronously retrieves two services of the specified types.
        /// </summary>
        public virtual async UniTask<(T1, T2)> GetServicesAsync<T1, T2>(CancellationToken cancellation = default)
            where T1 : class
            where T2 : class
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var linkedToken = linkedCts.Token;

            try
            {
                var task1 = GetServiceAsync<T1>(linkedToken);
                var task2 = GetServiceAsync<T2>(linkedToken);

                await UniTask.WhenAll(task1, task2);
                return (await task1, await task2);
            }
            catch (OperationCanceledException)
            {
                linkedCts.Cancel();
                throw;
            }
        }

        /// <summary>
        ///     Asynchronously retrieves three services of the specified types.
        /// </summary>
        public virtual UniTask<(T1, T2, T3)> GetServiceAsync<T1, T2, T3>(CancellationToken cancellation = default)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            return GetServicesAsync<T1, T2, T3>(cancellation);
        }

        /// <summary>
        ///     Asynchronously retrieves three services of the specified types.
        /// </summary>
        public virtual async UniTask<(T1, T2, T3)> GetServicesAsync<T1, T2, T3>(CancellationToken cancellation = default)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var linkedToken = linkedCts.Token;

            try
            {
                var task1 = GetServiceAsync<T1>(linkedToken);
                var task2 = GetServiceAsync<T2>(linkedToken);
                var task3 = GetServiceAsync<T3>(linkedToken);

                await UniTask.WhenAll(task1, task2, task3);
                return (await task1, await task2, await task3);
            }
            catch (OperationCanceledException)
            {
                linkedCts.Cancel();
                throw;
            }
        }

        /// <summary>
        ///     Asynchronously retrieves four services of the specified types.
        /// </summary>
        public virtual UniTask<(T1, T2, T3, T4)> GetServiceAsync<T1, T2, T3, T4>(CancellationToken cancellation = default)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            return GetServicesAsync<T1, T2, T3, T4>(cancellation);
        }

        /// <summary>
        ///     Asynchronously retrieves four services of the specified types.
        /// </summary>
        public virtual async UniTask<(T1, T2, T3, T4)> GetServicesAsync<T1, T2, T3, T4>(
            CancellationToken cancellation = default)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var linkedToken = linkedCts.Token;

            try
            {
                var task1 = GetServiceAsync<T1>(linkedToken);
                var task2 = GetServiceAsync<T2>(linkedToken);
                var task3 = GetServiceAsync<T3>(linkedToken);
                var task4 = GetServiceAsync<T4>(linkedToken);

                await UniTask.WhenAll(task1, task2, task3, task4);
                return (await task1, await task2, await task3, await task4);
            }
            catch (OperationCanceledException)
            {
                linkedCts.Cancel();
                throw;
            }
        }

        /// <summary>
        ///     Asynchronously retrieves five services of the specified types.
        /// </summary>
        public virtual UniTask<(T1, T2, T3, T4, T5)> GetServiceAsync<T1, T2, T3, T4, T5>(
            CancellationToken cancellation = default)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
        {
            return GetServicesAsync<T1, T2, T3, T4, T5>(cancellation);
        }

        /// <summary>
        ///     Asynchronously retrieves five services of the specified types.
        /// </summary>
        public virtual async UniTask<(T1, T2, T3, T4, T5)> GetServicesAsync<T1, T2, T3, T4, T5>(
            CancellationToken cancellation = default)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var linkedToken = linkedCts.Token;

            try
            {
                var task1 = GetServiceAsync<T1>(linkedToken);
                var task2 = GetServiceAsync<T2>(linkedToken);
                var task3 = GetServiceAsync<T3>(linkedToken);
                var task4 = GetServiceAsync<T4>(linkedToken);
                var task5 = GetServiceAsync<T5>(linkedToken);

                await UniTask.WhenAll(task1, task2, task3, task4, task5);
                return (await task1, await task2, await task3, await task4, await task5);
            }
            catch (OperationCanceledException)
            {
                linkedCts.Cancel();
                throw;
            }
        }

        /// <summary>
        ///     Asynchronously retrieves six services of the specified types.
        /// </summary>
        public virtual UniTask<(T1, T2, T3, T4, T5, T6)> GetServiceAsync<T1, T2, T3, T4, T5, T6>(
            CancellationToken cancellation = default)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
        {
            return GetServicesAsync<T1, T2, T3, T4, T5, T6>(cancellation);
        }

        /// <summary>
        ///     Asynchronously retrieves six services of the specified types.
        /// </summary>
        public virtual async UniTask<(T1, T2, T3, T4, T5, T6)> GetServicesAsync<T1, T2, T3, T4, T5, T6>(
            CancellationToken cancellation = default)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            var linkedToken = linkedCts.Token;

            try
            {
                var task1 = GetServiceAsync<T1>(linkedToken);
                var task2 = GetServiceAsync<T2>(linkedToken);
                var task3 = GetServiceAsync<T3>(linkedToken);
                var task4 = GetServiceAsync<T4>(linkedToken);
                var task5 = GetServiceAsync<T5>(linkedToken);
                var task6 = GetServiceAsync<T6>(linkedToken);

                await UniTask.WhenAll(task1, task2, task3, task4, task5, task6);
                return (await task1, await task2, await task3, await task4, await task5, await task6);
            }
            catch (OperationCanceledException)
            {
                linkedCts.Cancel();
                throw;
            }
        }
        
        /// <summary>
        ///     Rejects all pending promises for a specific service type with an exception.
        /// </summary>
        public virtual void RejectUniTaskService<T>(Exception exception) where T : class
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            lock (Lock)
            {
                var serviceType = typeof(T);
                RejectUniTaskPromises(serviceType, exception);
            }
        }

        // Implementation of helper methods

        /// <summary>
        ///     Resolves promises for a specific service type.
        /// </summary>
        protected override void ResolveUniTaskPromises(Type serviceType, object service)
        {
            if (!UniTaskPromiseMap.TryGetValue(serviceType, out var taskCompletions))
            {
                return;
            }

            foreach (var tcs in taskCompletions.ToList())
            {
                tcs.TrySetResult(service);
            }

            UniTaskPromiseMap.Remove(serviceType);
        }

        /// <summary>
        ///     Rejects promises for a specific service type.
        /// </summary>
        protected override void RejectUniTaskPromises(Type serviceType, Exception? exception = null)
        {
            if (!UniTaskPromiseMap.TryGetValue(serviceType, out var taskList))
            {
                return;
            }

            var ex = exception ?? new ObjectDisposedException(
                serviceType.Name,
                $"Service {serviceType.Name} was unregistered while waiting for it to be available");

            foreach (var tcs in taskList.ToList())
            {
                tcs.TrySetException(ex);
            }

            UniTaskPromiseMap.Remove(serviceType);
        }

        /// <summary>
        ///     Cancels and removes all unfulfilled promises.
        /// </summary>
        protected override void CleanupUniTaskPromises()
        {
            foreach (var promiseList in UniTaskPromiseMap.Values)
            {
                foreach (var promise in promiseList.ToList())
                {
                    promise.TrySetCanceled();
                }
            }

            UniTaskPromiseMap.Clear();
        }
    }
}
#endif