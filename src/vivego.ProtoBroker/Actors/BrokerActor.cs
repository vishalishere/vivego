using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Google.Protobuf;

using Proto;
using Proto.Cluster;

using vivego.ProtoBroker.Messages;

using PID = Proto.PID;

namespace vivego.ProtoBroker.Actors
{
	public class BrokerMasterActor : IActor
	{
		private readonly HashSet<PID> _brokers = new HashSet<PID>();
		
		public async Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Started _:
					Console.Out.WriteLine("Started: " + context.Self.Id);

					Actor.EventStream.Subscribe<ClusterTopologyEvent>(clusterTopologyEvent =>
					{
						Console.Out.WriteLine("Got inner ClusterTopologyEvent");
						context.Self.Tell(clusterTopologyEvent);
					});
					break;
					
				case PID brokerPid:
					_brokers.Add(brokerPid);
					context.Watch(brokerPid);
					break;

				case Terminated terminated:
					_brokers.Remove(terminated.Who);
					context.Unwatch(terminated.Who);
					break;

				case ClusterTopologyEvent clusterTopologyEvent:
					Console.Out.WriteLine("clusterTopologyEvent");
					_brokers.Clear();
					foreach (MemberStatus memberStatus in clusterTopologyEvent.Statuses)
					{
						if (memberStatus.Alive)
						{
							PID remoteBrokerActorPid = new PID(memberStatus.Address, context.Self.Id);
							GetPublisherPiDsResponse getPublisherPiDsResponse = await remoteBrokerActorPid
								.RequestAsync<GetPublisherPiDsResponse>(new GetPublisherPiDs())
								.ConfigureAwait(false);
						}
					}

					break;
			}
		}
	}
	
	public class BrokerActor : IActor
	{
		private readonly PID _brokerMasterActor;
		private readonly HashSet<PID> _publisherActors = new HashSet<PID>();
		private readonly HashSet<Subscription> _subscriptions = new HashSet<Subscription>();
		private Subscription<object> _clusterTopologyChangedSubscription;

		public BrokerActor(PID brokerMasterActor)
		{
			_brokerMasterActor = brokerMasterActor;
		}

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Started _:
					_brokerMasterActor.Tell(context.Self);

					Props props = Actor.FromProducer(() => new PublisherActor());
					foreach (int _ in Enumerable.Range(0, Environment.ProcessorCount))
					{
						PID publisher = context.SpawnPrefix(props, nameof(PublisherActor));
						context.Watch(publisher);
						_publisherActors.Add(publisher);
					}

					_clusterTopologyChangedSubscription = Actor.EventStream.Subscribe<ClusterTopologyEvent>(clusterTopologyEvent =>
					{
						_brokerMasterActor.Tell(context.Self);
					});

					break;

				case Stopped _:
					_clusterTopologyChangedSubscription?.Unsubscribe();

					foreach (Subscription subscription in _subscriptions)
					{
						TellAll(new Unsubscribe { SubscriptionId = subscription.SubscriptionId });
					}

					break;

				case GetPublisherPiDs _:
					GetPublisherPiDsResponse response = new GetPublisherPiDsResponse();
					response.PIds.AddRange(_publisherActors);
					context.Respond(response);
					break;

				case Subscribe subscribe:
					Subscription newSubscription = new Subscription
					{
						SubscriptionId = ByteString.CopyFrom(Guid.NewGuid().ToByteArray()),
						Subscriber = subscribe
					};
					_subscriptions.Add(newSubscription);
					TellAll(newSubscription);
					break;
				case Unsubscribe unsubscribe:
					_subscriptions.RemoveWhere(subscription => unsubscribe.SubscriptionId == subscription.SubscriptionId);
					TellAll(unsubscribe);
					break;

				case Terminated terminated:
					_publisherActors.Remove(terminated.Who);
					context.Unwatch(terminated.Who);
					break;
			}

			return Task.CompletedTask;
		}

		private void TellAll(object message)
		{
			foreach (PID publisher in _publisherActors)
			{
				publisher.Tell(message);
			}
		}
	}
}