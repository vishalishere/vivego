using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;

using vivego.core;
using vivego.Proto.PubSub;

using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Realm;

namespace vivego.WampSharp.Proto.SubPub.Backplane
{
	public static class RealmExtensions
	{
		public static IDisposable EnableDistributedBackplane(this IWampHostedRealm realm,
			IPublishSubscribe publishSubscribe)
		{
			ConcurrentStack<IDisposable> disposables = new ConcurrentStack<IDisposable>();

			AtomicBoolean disableInternalPublishAtomicBoolean = new AtomicBoolean();
			string forwarderPubSubTopic = $"DistributedRealm_{realm.Name}";
			string selfId = System.Diagnostics.Process.GetCurrentProcess().Id + "_" + Environment.MachineName;
			realm.TopicContainer.TopicCreated += (sender, args) =>
			{
				IDisposable subscription = args.Topic
					.Subscribe(new PubSubForwarderWampRawTopicRouterSubscriber(selfId, forwarderPubSubTopic, args.Topic, publishSubscribe, disableInternalPublishAtomicBoolean));
				disposables.Push(subscription);
			};

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
			disposables.Push(pubSubSubscription);
			return new AnonymousDisposable(() =>
			{
				while (disposables.TryPop(out IDisposable disposable))
				{
					disposable.Dispose();
				}
			});
		}
	}
}