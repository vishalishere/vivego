using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;

using BenchmarkDotNet.Attributes;

namespace vivego.EventLoopScheduler.StressTest
{
	public class EventLoopSchedulerTest2
	{
		[Benchmark]
		public void ScheduleSingleManyTimes()
		{
			Console.Out.WriteLine("ThreadCount Before EventLoopSchedulerAllocation: " + Process.GetCurrentProcess().Threads.Count);
			long counter;
			int numberOfItrations;
			Stopwatch sw;
			using (System.Reactive.Concurrency.EventLoopScheduler scheduler = new System.Reactive.Concurrency.EventLoopScheduler())
			{
				Console.Out.WriteLine("ThreadCount After EventLoopSchedulerAllocation: " + Process.GetCurrentProcess().Threads.Count);

				scheduler.Schedule<object>(null, TimeSpan.FromMilliseconds(1), (s, o) => Disposable.Empty);
				Console.Out.WriteLine("ThreadCount After Warmup: " + Process.GetCurrentProcess().Threads.Count);

				counter = 0;
				numberOfItrations = 1000000;
				sw = Stopwatch.StartNew();
				foreach (int i in Enumerable.Range(0, numberOfItrations))
				{
					scheduler.Schedule<object>(null, TimeSpan.FromMilliseconds(1000), (s, o) =>
					{
						Interlocked.Increment(ref counter);
						return Disposable.Empty;
					});
				}

				Console.Out.WriteLine(sw.Elapsed);
				Console.Out.WriteLine("ThreadCount After mass scheduling: " + Process.GetCurrentProcess().Threads.Count);

				while (Interlocked.Read(ref counter) < numberOfItrations)
				{

				}
			}

			sw.Stop();
			Console.Out.WriteLine(sw.Elapsed);

			Console.Out.WriteLine("ThreadCount after wait: " + Process.GetCurrentProcess().Threads.Count);
		}
	}
}