using BenchmarkDotNet.Running;

namespace vivego.SerializerTests
{
    public class Entrypoint
    {
		public static void Main()
		{
			BenchmarkRunner.Run<MessagePackTest>();
			//BenchmarkRunner.Run<WireTest>();
		}
	}
}
