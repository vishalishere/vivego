using System;
using System.Linq;

using vivego.core;

using Vivego.PublishSubscribe.Grpc;

namespace vivego.PublishSubscribe.ClientSelector
{
	public class HashRingSelector : DisposableBase, IClientSelector
	{
		private PubSub.PubSubClient[][] _clients = new PubSub.PubSubClient[0][];

		public HashRingSelector(IObservable<PubSub.PubSubClient[]> clientsObservable)
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
			int hash = topic.GetHashCode();
			PubSub.PubSubClient[][] clients = _clients;
			return clients[hash % clients.Length];
		}
	}
}