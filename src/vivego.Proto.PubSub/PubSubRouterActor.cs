using System;
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
		private readonly Dictionary<string, PID[]> _lookupCache = new Dictionary<string, PID[]>();

		private readonly Dictionary<string, (Counter Counter, Dictionary<PID, SubscriptionInfo> Subscriptions)>
			_subscriptions = new Dictionary<string, (Counter, Dictionary<PID, SubscriptionInfo>)>();

		private readonly Subscription<object> _topologySubscription;
		private PID[] _pubSubRouters = new PID[0];

		public PubSubRouterActor(ILoggerFactory loggerFactory)
		{
			_logger = loggerFactory.CreateLogger<PubSubRouterActor>();

			PubSubRouterActorPid = Actor.SpawnNamed(Actor.FromFunc(ReceiveAsync), typeof(PubSubRouterActor).FullName);
			_topologySubscription = Actor.EventStream.Subscribe<ClusterTopologyEvent>(clusterTopologyEvent =>
			{
				PubSubRouterActorPid.Tell(clusterTopologyEvent);
			});
		}

		public PID PubSubRouterActorPid { get; }

		private static string MakeCacheKey(string topic, string group)
		{
			return $"__{topic}:{group}";
		}

		private PID[] Lookup(Message message, Dictionary<PID, SubscriptionInfo> subscriptionDictionary)
		{
			string cacheKey = MakeCacheKey(message.Topic, message.Group);
			if (!_lookupCache.TryGetValue(cacheKey, out PID[] subscriptions))
			{
				subscriptions = subscriptionDictionary.Where(pair => pair.Value.Matches(message.Topic)).Select(pair => pair.Key)
					.ToArray();
				_lookupCache.Add(cacheKey, subscriptions);
			}

			return subscriptions;
		}

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
						foreach (KeyValuePair<string, (Counter Counter, Dictionary<PID, SubscriptionInfo> Subscriptions)> pair in
							_subscriptions)
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
						PID[] pids = Lookup(message, subscriptionDictionary.Subscriptions);
						if (string.IsNullOrEmpty(message.Group))
						{
							foreach (PID pid in pids)
							{
								pid.Tell(message);
							}
						}
						else
						{
							int counter = subscriptionDictionary.Counter.Next();
							PID pid = pids[counter % pids.Length];
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
						_logger.LogDebug("Added subscription from: '{0}', with topic '{1}' and group: '{2}'", subscription.PID, subscription.Topic, subscription.Group);
						subscriptionInfo = new SubscriptionInfo(subscription);
						subscriptions.Subscriptions.Add(subscription.PID, subscriptionInfo);
						context.Watch(subscription.PID);
						_lookupCache.Clear();
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
							_lookupCache.Clear();
							pair.Value.Subscriptions.Remove(terminated.Who);

							_logger.LogDebug("Removed subscription from: '{0}', with topic '{1}' and group: '{2}'", subscriptionInfo.Subscription.PID, subscriptionInfo.Subscription.Topic, subscriptionInfo.Subscription.Group);
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