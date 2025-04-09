#if !DISABLE_SL_UNITASK && ENABLE_UNITASK

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
       [NonSerialized]
       protected readonly Dictionary<Type, List<UniTaskCompletionSource<object>>> UniTaskPromiseMap = new();

       /// <summary>
       ///     Cancels and removes all unfulfilled UniTask promises.
       /// </summary>
       public virtual void CleanupUniTaskPromises()
       {
          lock (Lock)
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

       /// <summary>
       ///     Rejects all pending UniTask promises for a specific service type with an exception.
       ///     Use this if a service registration fails after it was requested.
       /// </summary>
       public virtual void RejectServiceUniTask<T>(Exception exception) where T : class
       {
          if (exception == null)
          {
             throw new ArgumentNullException(nameof(exception));
          }

          lock (Lock)
          {
             var serviceType = typeof(T);
             if (!UniTaskPromiseMap.TryGetValue(serviceType, out var taskList))
             {
                return;
             }

             foreach (var tcs in taskList.ToList())
             {
                tcs.TrySetException(exception);
             }

             UniTaskPromiseMap.Remove(serviceType);
          }
       }

       // Renamed from GetServiceUniTask to match Async naming convention
       /// <summary>
       ///     Asynchronously retrieves a service of the specified type using UniTask.
       /// </summary>
       /// <typeparam name="T">The type of service to retrieve.</typeparam>
       /// <param name="cancellation">Optional cancellation token.</param>
       /// <returns>A UniTask that resolves to the requested service instance.</returns>
       public virtual async UniTask<T> GetServiceAsync<T>(CancellationToken cancellation = default) where T : class
       {
          var serviceType = typeof(T);
          UniTaskCompletionSource<object> taskCompletion;

          lock (Lock)
          {
             if (ServiceMap.TryGetValue(serviceType, out var service))
             {
                // Service already registered, return immediately.
                return (T)service;
             }

             // Service not found, create or get a UniTaskCompletionSource.
             taskCompletion = new();
             if (!UniTaskPromiseMap.TryGetValue(serviceType, out var taskList))
             {
                taskList = new();
                UniTaskPromiseMap[serviceType] = taskList;
             }

             taskList.Add(taskCompletion);
          }

          // Handle cancellation outside the lock.
          CancellationTokenRegistration cancellationRegistration = default;
          if (cancellation.CanBeCanceled)
          {
             cancellationRegistration = cancellation.Register(() =>
             {
                lock (Lock)
                {
                    // Check if the task completion source still exists and is pending.
                   if (UniTaskPromiseMap.TryGetValue(serviceType, out var taskList) &&
                       taskList.Contains(taskCompletion))
                   {
                      taskCompletion.TrySetCanceled(cancellation);
                      taskList.Remove(taskCompletion);
                      if (taskList.Count == 0)
                      {
                         UniTaskPromiseMap.Remove(serviceType);
                      }
                   }
                }
             });
          }

          try
          {
             return (T)await taskCompletion.Task;
          }
          finally
          {
             // Dispose of the cancellation registration if it was created.
             #if CSHARP_7_3_OR_NEWER // Or appropriate version check for Dispose
             await cancellationRegistration.DisposeAsync();
             #else
             cancellationRegistration.Dispose();
             #endif

              // Clean up the list entry after the task completes (successfully, faulted, or cancelled by external source other than token)
              // This handles cases where the task completes before the cancellation token is triggered.
              lock (Lock)
              {
                  if (UniTaskPromiseMap.TryGetValue(serviceType, out var taskList) && taskList.Contains(taskCompletion))
                  {
                      taskList.Remove(taskCompletion);
                      if (taskList.Count == 0)
                      {
                          UniTaskPromiseMap.Remove(serviceType);
                      }
                  }
              }
          }
       }

       /// <summary>
       ///     Asynchronously retrieves two services of the specified types using UniTask.
       /// </summary>
       public virtual UniTask<(T1, T2)> GetServiceAsync<T1, T2>(CancellationToken cancellation = default)
          where T1 : class
          where T2 : class
       {
          return GetServicesAsync<T1, T2>(cancellation);
       }

       // Renamed from GetServicesUniTask to match Async naming convention
       /// <summary>
       ///     Implementation for asynchronously retrieving two services using UniTask.
       /// </summary>
       public virtual async UniTask<(T1, T2)> GetServicesAsync<T1, T2>(CancellationToken cancellation = default)
          where T1 : class
          where T2 : class
       {
          return await UniTask.WhenAll(
               GetServiceAsync<T1>(cancellation),
               GetServiceAsync<T2>(cancellation)
          );
       }

       /// <summary>
       ///     Asynchronously retrieves three services of the specified types using UniTask.
       /// </summary>
       public virtual UniTask<(T1, T2, T3)> GetServiceAsync<T1, T2, T3>(CancellationToken cancellation = default)
          where T1 : class
          where T2 : class
          where T3 : class
       {
          return GetServicesAsync<T1, T2, T3>(cancellation);
       }

       /// <summary>
       ///     Implementation for asynchronously retrieving three services using UniTask.
       /// </summary>
       public virtual async UniTask<(T1, T2, T3)> GetServicesAsync<T1, T2, T3>(
          CancellationToken cancellation = default)
          where T1 : class
          where T2 : class
          where T3 : class
       {
          return await UniTask.WhenAll(
              GetServiceAsync<T1>(cancellation),
              GetServiceAsync<T2>(cancellation),
              GetServiceAsync<T3>(cancellation)
          );
       }

       /// <summary>
       ///     Asynchronously retrieves four services of the specified types using UniTask.
       /// </summary>
       public virtual UniTask<(T1, T2, T3, T4)> GetServiceAsync<T1, T2, T3, T4>(
          CancellationToken cancellation = default)
          where T1 : class
          where T2 : class
          where T3 : class
          where T4 : class
       {
          return GetServicesAsync<T1, T2, T3, T4>(cancellation);
       }

       /// <summary>
       ///     Implementation for asynchronously retrieving four services using UniTask.
       /// </summary>
       public virtual async UniTask<(T1, T2, T3, T4)> GetServicesAsync<T1, T2, T3, T4>(
          CancellationToken cancellation = default)
          where T1 : class
          where T2 : class
          where T3 : class
          where T4 : class
       {
          return await UniTask.WhenAll(
              GetServiceAsync<T1>(cancellation),
              GetServiceAsync<T2>(cancellation),
              GetServiceAsync<T3>(cancellation),
              GetServiceAsync<T4>(cancellation)
          );
       }

       // Renamed from GetServiceUniTask to match Async naming convention
       /// <summary>
       ///     Asynchronously retrieves five services of the specified types using UniTask.
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
       ///     Implementation for asynchronously retrieving five services using UniTask.
       /// </summary>
       public virtual async UniTask<(T1, T2, T3, T4, T5)> GetServicesAsync<T1, T2, T3, T4, T5>(
          CancellationToken cancellation = default)
          where T1 : class
          where T2 : class
          where T3 : class
          where T4 : class
          where T5 : class
       {
          return await UniTask.WhenAll(
              GetServiceAsync<T1>(cancellation),
              GetServiceAsync<T2>(cancellation),
              GetServiceAsync<T3>(cancellation),
              GetServiceAsync<T4>(cancellation),
              GetServiceAsync<T5>(cancellation)
          );
       }

       /// <summary>
       ///     Asynchronously retrieves six services of the specified types using UniTask.
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
       ///     Implementation for asynchronously retrieving six services using UniTask.
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
          return await UniTask.WhenAll(
              GetServiceAsync<T1>(cancellation),
              GetServiceAsync<T2>(cancellation),
              GetServiceAsync<T3>(cancellation),
              GetServiceAsync<T4>(cancellation),
              GetServiceAsync<T5>(cancellation),
              GetServiceAsync<T6>(cancellation)
          );
       }
    }
}
#endif // !DISABLE_SL_UNITASK && ENABLE_UNITASK