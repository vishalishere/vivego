using System;

using vivego.core;

using Vivego.PublishSubscribe.Grpc;

namespace vivego.PublishSubscribe.ClientSelector
{
	public class LastSeenSelector : DisposableBase, IClientSelector
	{
		private PubSub.PubSubClient[] _lastSeenClient = new PubSub.PubSubClient[0];

		public LastSeenSelector(IObservable<PubSub.PubSubClient> heartbeatObservable)
		{
			IDisposable subscription = heartbeatObservable.Subscribe(client => _lastSeenClient = new[] {client});
			RegisterDisposable(subscription);
		}

		public PubSub.PubSubClient[] Select(string topic, string group)
		{
			return _lastSeenClient;
		}
	}
}