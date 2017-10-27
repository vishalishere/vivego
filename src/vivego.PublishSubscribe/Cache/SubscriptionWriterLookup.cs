using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using vivego.PublishSubscribe.Topic;

namespace vivego.PublishSubscribe.Cache
{
	public class SubscriptionWriterLookup<TWriter> : ISubscriptionWriterLookup<TWriter>
	{
		private readonly Dictionary<string, ConcurrentDictionary<TWriter, (Subscription subscription, ITopicFilter topicFilter)>> _db =
			new Dictionary<string, ConcurrentDictionary<TWriter, (Subscription subscription, ITopicFilter topicFilter)>>();

		public bool Add(TWriter destination, Subscription subscription, ITopicFilter topicFilter)
		{
			string group = string.IsNullOrEmpty(subscription.Group) ? Guid.NewGuid().ToString() : subscription.Group;
			if (!_db.TryGetValue(group, out ConcurrentDictionary<TWriter, (Subscription subscription, ITopicFilter topicFilter)> subscriptions))
			{
				subscriptions = new ConcurrentDictionary<TWriter, (Subscription subscription, ITopicFilter topicFilter)>();
				_db.Add(group, subscriptions);
			}

			return subscriptions.TryAdd(destination, (subscription, topicFilter));
		}

		public IEnumerable<Subscription> Remove(TWriter destination)
		{
			foreach (KeyValuePair<string, ConcurrentDictionary<TWriter, (Subscription subscription, ITopicFilter topicFilter)>> keyValuePair in _db)
			{
				if (keyValuePair.Value.TryRemove(destination, out (Subscription subscription, ITopicFilter topicFilter) tuple))
				{
					yield return tuple.subscription;
				}
			}
		}

		public (string Group, bool HashBy, TWriter[] Writer)[] Lookup(Message message)
		{
			return _db
				.SelectMany(pair =>
				{
					return pair
						.Value
						.Where(tuple => tuple.Value.topicFilter.Matches(message.Topic))
						.GroupBy(tuple => tuple.Value.subscription.HashBy)
						.Select(grouping => (pair.Key, grouping.Key, grouping.Select(valuePair => valuePair.Key).ToArray()));
				})
				.Where(dests => dests.Item3.Length > 0)
				.ToArray();
		}

		public TWriter[] GetAll()
		{
			return _db
				.SelectMany(pair => pair.Value.Select(valuePair => valuePair.Key))
				.ToArray();
		}
	}
}