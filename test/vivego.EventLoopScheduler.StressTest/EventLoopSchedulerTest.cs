using System;
using System.Reactive.Disposables;

using BenchmarkDotNet.Attributes;

namespace vivego.EventLoopScheduler.StressTest
{
	[MemoryDiagnoser]
	public class EventLoopSchedulerTest
	{
		private readonly System.Reactive.Concurrency.EventLoopScheduler _scheduler =
			new System.Reactive.Concurrency.EventLoopScheduler();

		[Benchmark]
		public void ScheduleSingle()
		{
			_scheduler.Schedule<object>(null, TimeSpan.FromMilliseconds(1), (s, o) => Disposable.Empty);
		}
	}
}