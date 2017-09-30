using System;

namespace vivego.PublishSubscribe
{
	public class Class1
	{
		public static void Main()
		{
			PublishSubscribeFactory
				.Static(55589, new Uri("tcp://0.0.0.0:55589"))
				.AsObservable<string>("hello.world")
				.Subscribe(tuple => { Console.Out.WriteLine(tuple.Data); });
			Console.ReadLine();
		}
	}
}