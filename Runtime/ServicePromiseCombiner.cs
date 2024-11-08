using System;
using System.Threading;

namespace Nonatomic.ServiceLocator
{
	public class ServicePromiseCombiner
	{
		private static IServicePromise<TResult> CombinePromisesInternal<TResult>(
			IServicePromise<object>[] promises,
			Func<object[], TResult> resultSelector)
		{
			var resultPromise = new ServicePromise<TResult>();
			var results = new object[promises.Length];
			var resolvedCount = 0;
			var isRejected = false;
			var lockObj = new object();

			for (var i = 0; i < promises.Length; i++)
			{
				var index = i;
				promises[i]
					.Then(s =>
					{
						results[index] = s;
						CheckAllResolved();
					})
					.Catch(ex =>
					{
						lock (lockObj)
						{
							if (!isRejected)
							{
								isRejected = true;
								resultPromise.Reject(ex);
							}
						}
					});
			}

			return resultPromise;

			void CheckAllResolved()
			{
				if (Interlocked.Increment(ref resolvedCount) == promises.Length)
				{
					try
					{
						var finalResult = resultSelector(results);
						resultPromise.Resolve(finalResult);
					}
					catch (Exception ex)
					{
						resultPromise.Reject(ex);
					}
				}
			}
		}

		public static IServicePromise<(T1, T2)> CombinePromises<T1, T2>(
			IServicePromise<T1> p1,
			IServicePromise<T2> p2)
		{
			return CombinePromisesInternal(
				new IServicePromise<object>[] { p1.AsObjectPromise(), p2.AsObjectPromise() },
				results => ((T1)results[0], (T2)results[1])
			);
		}

		public static IServicePromise<(T1, T2, T3)> CombinePromises<T1, T2, T3>(
			IServicePromise<T1> p1,
			IServicePromise<T2> p2,
			IServicePromise<T3> p3)
		{
			return CombinePromisesInternal(
				new IServicePromise<object>[] { p1.AsObjectPromise(), p2.AsObjectPromise(), p3.AsObjectPromise() },
				results => ((T1)results[0], (T2)results[1], (T3)results[2])
			);
		}

		public static IServicePromise<(T1, T2, T3, T4)> CombinePromises<T1, T2, T3, T4>(
			IServicePromise<T1> p1,
			IServicePromise<T2> p2,
			IServicePromise<T3> p3,
			IServicePromise<T4> p4)
		{
			return CombinePromisesInternal(
				new IServicePromise<object>[] { p1.AsObjectPromise(), p2.AsObjectPromise(), p3.AsObjectPromise(), p4.AsObjectPromise() },
				results => ((T1)results[0], (T2)results[1], (T3)results[2], (T4)results[3])
			);
		}
		
		public static IServicePromise<(T1, T2, T3, T4, T5)> CombinePromises<T1, T2, T3, T4, T5>(
			IServicePromise<T1> p1,
			IServicePromise<T2> p2,
			IServicePromise<T3> p3,
			IServicePromise<T4> p4,
			IServicePromise<T5> p5)
		{
			return CombinePromisesInternal(
				new IServicePromise<object>[] { p1.AsObjectPromise(), p2.AsObjectPromise(), p3.AsObjectPromise(), p4.AsObjectPromise(), p5.AsObjectPromise() },
				results => ((T1)results[0], (T2)results[1], (T3)results[2], (T4)results[3], (T5)results[4])
			);
		}
		
		public static IServicePromise<(T1, T2, T3, T4, T5, T6)> CombinePromises<T1, T2, T3, T4, T5, T6>(
			IServicePromise<T1> p1,
			IServicePromise<T2> p2,
			IServicePromise<T3> p3,
			IServicePromise<T4> p4,
			IServicePromise<T5> p5,
			IServicePromise<T6> p6)
		{
			return CombinePromisesInternal(
				new IServicePromise<object>[] { p1.AsObjectPromise(), p2.AsObjectPromise(), p3.AsObjectPromise(), p4.AsObjectPromise(), p5.AsObjectPromise(), p6.AsObjectPromise() },
				results => ((T1)results[0], (T2)results[1], (T3)results[2], (T4)results[3], (T5)results[4], (T6)results[5])
			);
		}
	}
}