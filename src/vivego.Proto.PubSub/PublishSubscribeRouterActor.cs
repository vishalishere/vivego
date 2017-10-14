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
using vivego.Proto.PubSub.TopicFilter;

namespace vivego.Proto.PubSub
{
	internal class PublishSubscribeRouterActor : DisposableBase
	{
		private readonly Func<Subscription, ITopicFilter> _topicFilterFactory;
		private readonly ILogger<PublishSubscribeRouterActor> _logger;
		private readonly Dictionary<string, PID[]> _lookupCache = new Dictionary<string, PID[]>();

		private readonly Dictionary<PID, (ITopicFilter, Subscription)> _subscriptions =
			new Dictionary<PID, (ITopicFilter, Subscription)>();

		private readonly Dictionary<string, (Counter Counter, Dictionary<PID, (ITopicFilter, Subscription)> Subscriptions)>
			_groupSubscriptions = new Dictionary<string, (Counter, Dictionary<PID, (ITopicFilter, Subscription)>)>();

		private readonly Subscription<object> _topologySubscription;
		private PID[] _pubSubRouters = new PID[0];
		private readonly string _publishSubscribeRouterActorName;

		public PublishSubscribeRouterActor(
			string clusterName,
			ILoggerFactory loggerFactory,
			Func<Subscription, ITopicFilter> topicFilterFactory)
		{
			if (loggerFactory == null)
			{
				throw new ArgumentNullException(nameof(loggerFactory));
			}

			_topicFilterFactory = topicFilterFactory ?? throw new ArgumentNullException(nameof(topicFilterFactory));

			_logger = loggerFactory.CreateLogger<PublishSubscribeRouterActor>();
			_publishSubscribeRouterActorName = $"{clusterName}_{typeof(PublishSubscribeRouterActor).FullName}";
			Props props = Actor.FromFunc(ReceiveAsync)
				.WithMailbox(() => BoundedMailbox.Create(8192));
			PubSubRouterActorPid = Actor.SpawnNamed(props, _publishSubscribeRouterActorName);
			_topologySubscription = Actor.EventStream
				.Subscribe<ClusterTopologyEvent>(clusterTopologyEvent =>
				{
					PubSubRouterActorPid.Tell(clusterTopologyEvent);
				});
		}

		public PID PubSubRouterActorPid { get; }

		private PID[] Lookup(string cacheKey, Message message, Dictionary<PID, (ITopicFilter TopicFilter, Subscription subscription)> subscriptions)
		{
			if (!_lookupCache.TryGetValue(cacheKey, out PID[] cachedSubscriptions))
			{
				cachedSubscriptions = subscriptions
					.Where(pair => pair.Value.TopicFilter.Matches(message.Topic))
					.Select(pair => pair.Key)
					.ToArray();
			}

			return cachedSubscriptions;
		}

		private IEnumerable<PID> LookupGroups(Message message)
		{
			foreach (KeyValuePair<string, (Counter Counter, Dictionary<PID, (ITopicFilter, Subscription)> Subscriptions)> valuePair in _groupSubscriptions)
			{
				PID[] matches = Lookup(valuePair.Key, message, valuePair.Value.Subscriptions);
				Counter counter = valuePair.Value.Counter;
				yield return matches[counter.Next() % matches.Length];
			}
		}

		private IEnumerable<PID> Lookup(Message message)
		{
			return Lookup("__nonGroups", message, _subscriptions)
				.Concat(LookupGroups(message));
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
						.Select(tuple => tuple.Value.Item2)
						.Concat(_groupSubscriptions.SelectMany(pair => pair.Value.Subscriptions.Select(valuePair => valuePair.Value.Item2)))
						.DistinctBy(tuple => tuple.PID)
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
					Dictionary<PID, (ITopicFilter, Subscription)> dictionary;
					if (string.IsNullOrEmpty(subscription.Group))
					{
						dictionary = _subscriptions;
					}
					else
					{
						if (!_groupSubscriptions.TryGetValue(subscription.Group,
							out (Counter Counter, Dictionary<PID, (ITopicFilter, Subscription)> Subscriptions) subscriptions))
						{
							subscriptions = (new Counter(), new Dictionary<PID, (ITopicFilter, Subscription)>());
							_groupSubscriptions.Add(subscription.Group, subscriptions);
						}

						dictionary = subscriptions.Subscriptions;
					}

					ITopicFilter topicFilter = _topicFilterFactory(subscription);
					if (dictionary.TryAdd(subscription.PID, (topicFilter, subscription)))
					{
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
					if (_subscriptions.TryGetValue(terminated.Who, out (ITopicFilter, Subscription Subscription) tuple))
					{
						_logger.LogDebug("Removed subscription with PID: '{0}'; Topic: {1}", tuple.Subscription.PID, tuple.Subscription.Topic);
					}

					foreach (var groupSubscription in _groupSubscriptions)
					{
						if (groupSubscription.Value.Subscriptions.TryGetValue(terminated.Who, out (ITopicFilter, Subscription Subscription) tuple2))
						{
							_logger.LogDebug("Removed subscription with PID: '{0}'; Topic: {1}; Group: {2}", tuple2.Subscription.PID, tuple2.Subscription.Topic, tuple2.Subscription.Group);
						}
					}

					_lookupCache.Clear();
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