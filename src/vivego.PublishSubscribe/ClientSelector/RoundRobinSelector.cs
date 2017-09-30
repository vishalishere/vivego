using System;
using System.Linq;
using System.Threading;

using vivego.core;

using Vivego.PublishSubscribe.Grpc;

namespace vivego.PublishSubscribe.ClientSelector
{
	public class RoundRobinSelector : DisposableBase, IClientSelector
	{
		private PubSub.PubSubClient[][] _clients = new PubSub.PubSubClient[0][];
		private long _counter;

		public RoundRobinSelector(IObservable<PubSub.PubSubClient[]> clientsObservable)
		{
			IDisposable subscription = clientsObservable.Subscribe(clients =>
			{
				_clients = clients
					.Select(client => new[] {client})
					.ToArray();
			});
			RegisterDisposable(subscription);
		}

		public PubSub.PubSubClient[] Select(string topic, string group)
		{
			PubSub.PubSubClient[][] clients = _clients;
			return clients[Interlocked.Increment(ref _counter) % clients.Length];
		}
	}
}