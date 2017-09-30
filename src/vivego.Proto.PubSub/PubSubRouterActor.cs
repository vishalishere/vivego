using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Proto;
using Proto.Cluster;

using vivego.core;
using vivego.Proto.PubSub.Messages;

namespace vivego.Proto.PubSub
{
	internal class PubSubRouterActor : DisposableBase
	{
		private readonly ILogger<PubSubRouterActor> _logger;

		private readonly Dictionary<string, (Counter Counter, Dictionary<PID, SubscriptionInfo> Subscriptions)>
			_subscriptions = new Dictionary<string, (Counter, Dictionary<PID, SubscriptionInfo>)>();

		private readonly Subscription<object> _topologySubscription;
		private PID[] _pubSubRouters = new PID[0];

		public PubSubRouterActor(ILoggerFactory loggerFactory)
		{
			_logger = loggerFactory.CreateLogger<PubSubRouterActor>();

			Pid = Actor.SpawnNamed(Actor.FromFunc(ReceiveAsync), typeof(PubSubRouterActor).FullName);
			_topologySubscription = Actor.EventStream.Subscribe<ClusterTopologyEvent>(clusterTopologyEvent =>
			{
				Pid.Tell(clusterTopologyEvent);
			});
		}

		public PID Pid { get; }

		private Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case ClusterTopologyEvent clusterTopologyEvent:
					_pubSubRouters = clusterTopologyEvent
						.Statuses
						.Where(memberStatus => memberStatus.Alive)
						.Select(memberStatus =>
						{
							PID routerPid = new PID(memberStatus.Address, typeof(PubSubRouterActor).FullName);
							return routerPid;
						})
						.ToArray();
					foreach (PID routerPid in _pubSubRouters)
					{
						foreach (var pair in _subscriptions)
						{
							foreach (KeyValuePair<PID, SubscriptionInfo> subscriptionInfo in pair.Value.Subscriptions)
							{
								routerPid.Tell(subscriptionInfo.Value.Subscription);
							}
						}
					}

					_logger.LogDebug("Topology changed to: {0}",
						string.Join(";", clusterTopologyEvent
							.Statuses
							.Where(memberStatus => memberStatus.Alive)
							.Select(memberStatus => memberStatus.Address)));

					break;
				case Message message:
					if (_subscriptions.TryGetValue(message.Group,
						out (Counter Counter, Dictionary<PID, SubscriptionInfo> Subscriptions) subscriptionDictionary))
					{
						if (string.IsNullOrEmpty(message.Group))
						{
							foreach (KeyValuePair<PID, SubscriptionInfo> pair in subscriptionDictionary.Subscriptions)
							{
								pair.Key.Tell(message);
							}
						}
						else
						{
							int counter = subscriptionDictionary.Counter.Next();
							Dictionary<PID, SubscriptionInfo>.KeyCollection keys = subscriptionDictionary.Subscriptions.Keys;
							PID pid = keys.ToArray()[counter % keys.Count];
							pid.Tell(message);
						}
					}

					break;
				case Subscription subscription:
				{
					if (!_subscriptions.TryGetValue(subscription.Group,
						out (Counter Counter, Dictionary<PID, SubscriptionInfo> Subscriptions) subscriptions))
					{
						subscriptions = (new Counter(), new Dictionary<PID, SubscriptionInfo>());
						_subscriptions.Add(subscription.Group, subscriptions);
					}

					if (!subscriptions.Subscriptions.TryGetValue(subscription.PID, out SubscriptionInfo subscriptionInfo))
					{
						_logger.LogDebug("Added subscription from: '{0}', with topic '{1}' and group: '{2}'", subscription.PID,
							subscription.Topic, subscription.Group);
						subscriptionInfo = new SubscriptionInfo(subscription);
						subscriptions.Subscriptions.Add(subscription.PID, subscriptionInfo);
						context.Watch(subscription.PID);
						foreach (PID routerPid in _pubSubRouters)
						{
							routerPid.Tell(subscription);
						}
					}

					break;
				}
				case Terminated terminated:
				{
					foreach (KeyValuePair<string, (Counter Counter, Dictionary<PID, SubscriptionInfo> Subscriptions)> pair in
						_subscriptions.ToArray())
					{
						if (pair.Value.Subscriptions.TryGetValue(terminated.Who, out SubscriptionInfo subscriptionInfo))
						{
							pair.Value.Subscriptions.Remove(terminated.Who);
							_logger.LogDebug("Removed subscription from: '{0}', with topic '{1}' and group: '{2}'",
								subscriptionInfo.Subscription.PID, subscriptionInfo.Subscription.Topic, subscriptionInfo.Subscription.Group);
							if (pair.Value.Subscriptions.Count == 0)
							{
								_subscriptions.Remove(pair.Key);
							}
						}
					}

					break;
				}
			}

			return Task.CompletedTask;
		}

		protected override void Cleanup()
		{
			_topologySubscription.Unsubscribe();
		}
	}
}