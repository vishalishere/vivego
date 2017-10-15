using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Proto;
using Proto.Cluster;
using Proto.Mailbox;

using vivego.core;
using vivego.Proto.PubSub.Messages;
using vivego.Proto.PubSub.Route;
using vivego.Proto.PubSub.Topic;

namespace vivego.Proto.PubSub
{
	internal class PublishSubscribeRouterActor : DisposableBase
	{
		private readonly IRouteSelector _routeSelector;
		private readonly Func<Subscription, ITopicFilter> _topicFilterFactory;
		private readonly ILogger<PublishSubscribeRouterActor> _logger;
		private readonly Dictionary<string, PID[]> _lookupCache = new Dictionary<string, PID[]>();

		private readonly Dictionary<string, Dictionary<PID, (ITopicFilter TopicFilter, Subscription Subscription)>>
			_subscriptions = new Dictionary<string, Dictionary<PID, (ITopicFilter, Subscription)>>();

		private readonly Subscription<object> _topologySubscription;
		private PID[] _pubSubRouters = new PID[0];
		private readonly string _publishSubscribeRouterActorName;

		public PublishSubscribeRouterActor(
			string clusterName,
			ILoggerFactory loggerFactory,
			IRouteSelector routeSelector,
			Func<Subscription, ITopicFilter> topicFilterFactory,
			int sendBufferSize = 8192)
		{
			if (loggerFactory == null)
			{
				throw new ArgumentNullException(nameof(loggerFactory));
			}

			_routeSelector = routeSelector ?? throw new ArgumentNullException(nameof(routeSelector));
			_topicFilterFactory = topicFilterFactory ?? throw new ArgumentNullException(nameof(topicFilterFactory));

			_logger = loggerFactory.CreateLogger<PublishSubscribeRouterActor>();
			_publishSubscribeRouterActorName = $"{clusterName}_{typeof(PublishSubscribeRouterActor).FullName}";
			Props props = Actor.FromFunc(ReceiveAsync)
				.WithMailbox(() => BoundedMailbox.Create(sendBufferSize));
			PubSubRouterActorPid = Actor.SpawnNamed(props, _publishSubscribeRouterActorName);
			_topologySubscription = Actor.EventStream
				.Subscribe<ClusterTopologyEvent>(clusterTopologyEvent =>
				{
					PubSubRouterActorPid.Tell(clusterTopologyEvent);
				});
		}

		public PID PubSubRouterActorPid { get; }

		private PID[] Lookup(string group, Message message, Dictionary<PID, (ITopicFilter TopicFilter, Subscription subscription)> subscriptions)
		{
			string cacheKey = $"{group}_{message.Topic}";
			if (!_lookupCache.TryGetValue(cacheKey, out PID[] cachedSubscriptions))
			{
				cachedSubscriptions = subscriptions
					.Where(pair => pair.Value.TopicFilter.Matches(message.Topic))
					.Select(pair => pair.Key)
					.OrderBy(pid => pid.Address)
					.ThenBy(pid => pid.Id)
					.ToArray();
				_lookupCache.Add(cacheKey, cachedSubscriptions);
			}

			return cachedSubscriptions;
		}

		private IEnumerable<PID> Lookup(Message message)
		{
			return _subscriptions
				.SelectMany(pair =>
				{
					string group = pair.Key;
					PID[] matches = Lookup(group, message, pair.Value);
					return matches.Length > 0
						? _routeSelector.Select(message, group, matches)
						: Enumerable.Empty<PID>();
				});
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
							PID routerPid = new PID(memberStatus.Address, _publishSubscribeRouterActorName);
							return routerPid;
						})
						.ToArray();

					_subscriptions
						.SelectMany(pair => pair.Value.Select(valuePair => valuePair.Value.Subscription))
						.DistinctBy(subscription => subscription.PID)
						.ForEach(subscription =>
						{
							foreach (PID routerPid in _pubSubRouters)
							{
								routerPid.Tell(subscription);
							}
						});

					_logger.LogDebug("Topology changed to: {0}",
						string.Join(";", clusterTopologyEvent
							.Statuses
							.Where(memberStatus => memberStatus.Alive)
							.Select(memberStatus => memberStatus.Address)));

					break;
				case Message message:
					foreach (PID pid in Lookup(message))
					{
						pid.Tell(message);
					}

					break;
				case Subscription subscription:
				{
					if (!_subscriptions.TryGetValue(subscription.Group, out Dictionary<PID, (ITopicFilter, Subscription)> subscriptions))
					{
						subscriptions = new Dictionary<PID, (ITopicFilter, Subscription)>();
						_subscriptions.Add(subscription.Group, subscriptions);
					}

					ITopicFilter topicFilter = _topicFilterFactory(subscription);
					if (!subscriptions.ContainsKey(subscription.PID))
					{
						subscriptions.Add(subscription.PID, (topicFilter, subscription));
						context.Watch(subscription.PID);
						_lookupCache.Clear();
						foreach (PID routerPid in _pubSubRouters)
						{
							routerPid.Tell(subscription);
						}

						_logger.LogDebug("Added subscription from: '{0}', with topic '{1}' and group: '{2}'", subscription.PID, subscription.Topic, subscription.Group);
					}

					break;
				}
				case Terminated terminated:
				{
					foreach (var groupSubscription in _subscriptions)
					{
						if (groupSubscription.Value.TryGetValue(terminated.Who, out (ITopicFilter, Subscription Subscription) tuple2))
						{
							groupSubscription.Value.Remove(terminated.Who);
							_lookupCache.Clear();
							_logger.LogDebug("Removed subscription with PID: '{0}'; Topic: {1}; Group: {2}", tuple2.Subscription.PID, tuple2.Subscription.Topic, tuple2.Subscription.Group);
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