using System;
using System.Linq;
using System.Reactive.Linq;

using vivego.PublishSubscribe.ClientSelector;
using vivego.PublishSubscribe.Serializer;
using vivego.PublishSubscribe.Topology;

using Vivego.PublishSubscribe.Grpc;

namespace vivego.PublishSubscribe
{
	public class PublishSubscribeFactory
	{
		public static IPublishSubscribe Automatic(string identifier)
		{
			return null;
		}

		public static IPublishSubscribe Static(
			int serverPort,
			params Uri[] serverUri)
		{
			PubSubServer pubSubServer = new PubSubServer(serverPort);
			DefaultSerializer serializer = new DefaultSerializer();
			StaticClusterTopologyObservable staticClusterTopologyObservable = new StaticClusterTopologyObservable(serverUri);
			DefaultChannelObservable channelObservable = new DefaultChannelObservable(staticClusterTopologyObservable);
			IObservable<PubSub.PubSubClient[]> clientObservable = channelObservable
				.Select(channels => channels.Select(channel => new PubSub.PubSubClient(channel)).ToArray());
			IObservable<PubSub.PubSubClient> heartbeatObservable = clientObservable
				.SelectMany(clients => clients.Select(client => new HeartbeatClient(client)))
				.Merge();
			IObservable<LastSeenSelector> selectorObservable = clientObservable
				.Select(client => new LastSeenSelector(heartbeatObservable));
			return new PublishSubscribeClient(serializer, selectorObservable, selectorObservable);
		}
	}
}