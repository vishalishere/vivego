using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;

using vivego.core;
using vivego.Proto.PubSub;

using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.PubSub;
using WampSharp.V2.Realm;

namespace vivego.WampSharp.Proto.SubPub.Backplane
{
	public static class RealmExtensions
	{
		public static IDisposable EnableDistributedBackplane(this IWampHostedRealm realm,
			IPublishSubscribe publishSubscribe)
		{
			ConcurrentDictionary<string, IDisposable> disposables = new ConcurrentDictionary<string, IDisposable>();

			AtomicBoolean disableInternalPublishAtomicBoolean = new AtomicBoolean();
			string forwarderPubSubTopic = $"DistributedRealm_{realm.Name}";
			string selfId = $"{System.Diagnostics.Process.GetCurrentProcess().Id}_{Environment.MachineName}";

			void TopicCreated(object sender, WampTopicCreatedEventArgs args)
			{
				IDisposable subscription = args.Topic
					.Subscribe(new PubSubForwarderWampRawTopicRouterSubscriber(selfId, forwarderPubSubTopic, args.Topic, publishSubscribe, disableInternalPublishAtomicBoolean));
				disposables.TryAdd(args.Topic.TopicUri, subscription);
			}

			void TopicRemoved(object sender, WampTopicRemovedEventArgs args)
			{
				if (disposables.TryGetValue(args.Topic.TopicUri, out IDisposable subscription))
				{
					subscription.Dispose();
				}
			}

			realm.TopicContainer.TopicCreated += TopicCreated;
			realm.TopicContainer.TopicRemoved += TopicRemoved;

			PublishOptions defaultPublishOptions = new PublishOptions();
			IDisposable pubSubSubscription = publishSubscribe
				.Observe<ForwardedWampMessage>(forwarderPubSubTopic)
				.Where(tuple => !selfId.Equals(tuple.Data.PublisherId))
				.Subscribe(_ =>
				{
					disableInternalPublishAtomicBoolean.FalseToTrue();
					try
					{
						if (_.Data.ArgumentsKeywords == null
							&& _.Data.Arguments == null)
						{
							realm.TopicContainer.Publish(WampObjectFormatter.Value, defaultPublishOptions, _.Data.WampTopic);
						}
						else
						{
							if (_.Data.ArgumentsKeywords == null)
							{
								realm.TopicContainer.Publish(WampObjectFormatter.Value,
									defaultPublishOptions,
									_.Data.WampTopic,
									_.Data.Arguments);
							}
							else
							{
								realm.TopicContainer.Publish(WampObjectFormatter.Value,
									defaultPublishOptions,
									_.Data.WampTopic,
									_.Data.Arguments,
									_.Data.ArgumentsKeywords);
							}
						}
					}
					finally
					{
						disableInternalPublishAtomicBoolean.TrueToFalse();
					}
				});

			return new AnonymousDisposable(() =>
			{
				pubSubSubscription.Dispose();
				realm.TopicContainer.TopicCreated -= TopicCreated;
				realm.TopicContainer.TopicRemoved -= TopicRemoved;
				foreach (KeyValuePair<string, IDisposable> pair in disposables)
				{
					pair.Value.Dispose();
				}
			});
		}
	}
}