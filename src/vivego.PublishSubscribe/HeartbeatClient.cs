using System;
using System.Reactive.Linq;

using Grpc.Core;

using vivego.core;
using vivego.PublishSubscribe.ExtensionMethods;

using Vivego.PublishSubscribe.Grpc;

namespace vivego.PublishSubscribe
{
	public class HeartbeatClient : DisposableBase, IObservable<PubSub.PubSubClient>
	{
		private readonly IObservable<PubSub.PubSubClient> _observable;

		public HeartbeatClient(PubSub.PubSubClient pubSubClient)
		{
			Subscription heartbeatSubscription = new Subscription
			{
				Topic = "heartbeat"
			};
			AsyncServerStreamingCall<Message> asyncServerStreamingCall = pubSubClient.Listen(heartbeatSubscription);
			_observable = asyncServerStreamingCall
				.ToObservable()
				.Retry()
				.Publish()
				.RefCount()
				.Select(_ => pubSubClient);
			_observable.Subscribe(_ => LastSeen = DateTime.UtcNow, CancellationToken);
		}

		public DateTime LastSeen { get; set; }

		public IDisposable Subscribe(IObserver<PubSub.PubSubClient> observer)
		{
			return _observable.Subscribe(observer);
		}
	}
}