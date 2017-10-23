using System;
using System.Collections.Generic;
using System.Linq;

using vivego.PublishSubscribe.Topic;

namespace vivego.PublishSubscribe
{
	public class SubscriptionWriterLookup<TWriter> : ISubscriptionWriterLookup<TWriter>
	{
		private readonly Dictionary<string, Dictionary<TWriter, (Subscription subscription, ITopicFilter topicFilter)>> _db =
			new Dictionary<string, Dictionary<TWriter, (Subscription subscription, ITopicFilter topicFilter)>>();

		public void Add(TWriter destination, Subscription subscription, ITopicFilter topicFilter)
		{
			string group = string.IsNullOrEmpty(subscription.Group) ? Guid.NewGuid().ToString() : subscription.Group;
			if (!_db.TryGetValue(group, out Dictionary<TWriter, (Subscription subscription, ITopicFilter topicFilter)> subscriptions))
			{
				subscriptions = new Dictionary<TWriter, (Subscription subscription, ITopicFilter topicFilter)>();
				_db.Add(group, subscriptions);
			}

			subscriptions.Add(destination, (subscription, topicFilter));
		}

		public void Remove(TWriter destination)
		{
			foreach (KeyValuePair<string, Dictionary<TWriter, (Subscription subscription, ITopicFilter topicFilter)>> keyValuePair in _db)
			{
				keyValuePair.Value.Remove(destination);
			}
		}

		public TWriter[][] Lookup(Message message)
		{
			bool routeByHash = message.Hash > 0;
			return _db
				.Select(pair =>
				{
					return pair
						.Value
						.Where(tuple => tuple.Value.subscription.HashBy == routeByHash && tuple.Value.topicFilter.Matches(message.Topic))
						.Select(tuple => tuple.Key)
						.ToArray();
				})
				.Where(dests => dests.Length > 0)
				.ToArray();
		}
	}
}