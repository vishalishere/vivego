using System;
using System.Threading.Tasks;

using Proto;

using vivego.ProtoBroker.Messages;

namespace vivego.ProtoBroker.Actors
{
	public class ConsumerActor<T> : IActor
	{
		private readonly IObserver<T> _observer;

		public ConsumerActor(IObserver<T> observer)
		{
			_observer = observer;
		}

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Publish publish:
					Console.Out.WriteLine(DateTime.UtcNow + " - " + publish.Topic);
					//_observer.OnNext((T) publish.Message);
					return Task.CompletedTask;
				default:
					return Task.CompletedTask;
			}
		}
	}
}