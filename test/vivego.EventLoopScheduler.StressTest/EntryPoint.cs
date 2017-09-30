using BenchmarkDotNet.Running;

namespace vivego.EventLoopScheduler.StressTest
{
	public class EntryPoint
	{
		public static void Main()
		{
			//BenchmarkRunner.Run<EventLoopSchedulerTest>();

			BenchmarkRunner.Run<EventLoopSchedulerTest2>();
		}
	}
}