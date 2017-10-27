using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Proto;
using Proto.Cluster;
using Proto.Mailbox;
using Proto.Router;

using vivego.core;
using vivego.Proto.PubSub.Messages;
using vivego.Proto.PubSub.Route;
using vivego.PublishSubscribe;
using vivego.PublishSubscribe.Cache;
using vivego.PublishSubscribe.Topic;

namespace vivego.Proto.PubSub
{
	public class PublishSubscribeRouterActor : DisposableBase
	{
		private readonly ILogger<PublishSubscribeRouterActor> _logger;
		private readonly ISubscriptionWriterLookup<PID> _lookup;
		private readonly Dictionary<string, PID> _lookupCache = new Dictionary<string, PID>();
		private readonly string _publishSubscribeRouterActorName;
		private readonly Func<Subscription, ITopicFilter> _topicFilterFactory;
		private readonly Subscription<object> _topologySubscription;

		public PublishSubscribeRouterActor(
			string clusterName,
			ILoggerFactory loggerFactory,
			Func<Subscription, ITopicFilter> topicFilterFactory,
			int sendBufferSize = 8192)
		{
			if (loggerFactory == null)
			{
				throw new ArgumentNullException(nameof(loggerFactory));
			}

			_topicFilterFactory = topicFilterFactory ?? throw new ArgumentNullException(nameof(topicFilterFactory));

			_lookup = new SubscriptionWriterLookupCache<PID>(new SubscriptionWriterLookup<PID>());

			_logger = loggerFactory.CreateLogger<PublishSubscribeRouterActor>();
			_publishSubscribeRouterActorName = $"{clusterName}_{typeof(PublishSubscribeRouterActor).FullName}";
			Props props = Actor.FromFunc(ReceiveAsync).WithMailbox(() => BoundedMailbox.Create(sendBufferSize));
			PubSubRouterActorPid = Actor.SpawnNamed(props, _publishSubscribeRouterActorName);
			_topologySubscription = Actor.EventStream
				.Subscribe<ClusterTopologyEvent>(clusterTopologyEvent => { PubSubRouterActorPid.Tell(clusterTopologyEvent); });
		}

		public PID PubSubRouterActorPid { get; }

		protected virtual Props MakeRouterProps(Message message, string group, bool hashBy, PID[] pids)
		{
			if (hashBy)
			{
				return new ConsistentHashGroupRouterConfig(MD5Hasher.Hash, 100, pids).Props();
			}

			if (string.IsNullOrEmpty(group))
			{
				return Router.NewBroadcastGroup(pids);
			}

			return Router.NewRoundRobinGroup(pids);
		}

		private PID Lookup(Message message)
		{
			List<PID> routerPids = new List<PID>();
			(string Group, bool HashBy, PID[] Writer)[] pidGroups = _lookup.Lookup(message);
			foreach ((string Group, bool HashBy, PID[] Writer) pidGroup in pidGroups)
			{
				if (pidGroups.Length == 0)
				{
					continue;
				}

				Props routerProds = MakeRouterProps(message, pidGroup.Group, pidGroup.HashBy, pidGroup.Writer);
				PID routerPid = Actor.Spawn(routerProds);
				routerPids.Add(routerPid);
			}

			return Actor.Spawn(Router.NewBroadcastGroup(routerPids.ToArray()));
		}

		private void ClearCache()
		{
			_lookupCache.Clear();
		}

		private PID LookupCached(Message message)
		{
			if (!_lookupCache.TryGetValue(message.Topic, out PID pid))
			{
				pid = Lookup(message);
				_lookupCache.Add(message.Topic, pid);
			}

			return pid;
		}

		private Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case ClusterTopologyEvent clusterTopologyEvent:
					PID[] pubSubRouters = clusterTopologyEvent
						.Statuses
						.Where(memberStatus => memberStatus.Alive)
						.Select(memberStatus =>
						{
							PID routerPid = new PID(memberStatus.Address, _publishSubscribeRouterActorName);
							return routerPid;
						})
						.ToArray();

					_lookup
						.GetAll()
						.ForEach(subscription =>
						{
							foreach (PID routerPid in pubSubRouters)
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
				{
					PID routerPid = LookupCached(message);
					routerPid.Tell(message);
					break;
				}
				case ProtoSubscription subscription:
				{
					bool added = _lookup.Add(subscription.PID, subscription.Subscription, _topicFilterFactory(subscription.Subscription));
					if (added)
					{
						context.Watch(subscription.PID);
						_logger.LogDebug("Added subscription from: '{0}', with topic '{1}' and group: '{2}'", subscription.PID,
							subscription.Subscription.Topic, subscription.Subscription.Group);
						ClearCache();
					}

					break;
				}
				case Terminated terminated:
				{
					ClearCache();
					foreach (Subscription subscription in _lookup.Remove(terminated.Who))
					{
						_logger.LogDebug("Removed subscription with PID: '{0}'; Topic: {1}; Group: {2}", terminated.Who,
							subscription.Topic, subscription.Group);
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