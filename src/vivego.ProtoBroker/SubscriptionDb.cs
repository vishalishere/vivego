using System;
using System.Collections.Generic;
using System.Linq;

using vivego.core;

namespace vivego.ProtoBroker
{
	public class SubscriptionInfoDb<T>
	{
		private readonly List<(SubscriptionInfo SubscriptionInfo, T t)> _subscriptions =
			new List<(SubscriptionInfo SubscriptionInfo, T t)>();

		private ILookup<string, (SubscriptionInfo SubscriptionInfo, T t)> _nonWildcardSubscriptions;

		private (SubscriptionInfo SubscriptionInfo, T t)[] _wildcardSubscriptions =
			new (SubscriptionInfo SubscriptionInfo, T t)[0];

		public SubscriptionInfoDb()
		{
			BuildIndex();
		}

		public IEnumerable<(T, object)> Select(string topic, object value)
		{
			IEnumerable<(SubscriptionInfo SubscriptionInfo, T t)> source = _nonWildcardSubscriptions.Contains(topic)
				? _nonWildcardSubscriptions[topic]
				: Enumerable.Empty<(SubscriptionInfo SubscriptionInfo, T t)>();

			source = source
				.AddToEnd(_wildcardSubscriptions
					.Where(rxBrokerSubject => rxBrokerSubject.SubscriptionInfo.Matches(topic, value)));

			foreach ((SubscriptionInfo SubscriptionInfo, T t) subscription in source)
			{
				yield return (subscription.t, value);
			}
		}

		public void Add(SubscriptionInfo subscriptionInfo, T t)
		{
			lock (_subscriptions)
			{
				_subscriptions.Add((subscriptionInfo, t));
				BuildIndex();
			}
		}

		public bool Remove(Guid subscriptionId)
		{
			lock (_subscriptions)
			{
				int subscriptionCount = _subscriptions.Count;
				_subscriptions.RemoveAll(tuple => tuple.SubscriptionInfo.SubscriptionId == subscriptionId);
				BuildIndex();
				return subscriptionCount > _subscriptions.Count;
			}
		}

		private void BuildIndex()
		{
			lock (_subscriptions)
			{
				_nonWildcardSubscriptions = _subscriptions
					.Where(tuple => !tuple.SubscriptionInfo.WildcardTopic)
					.ToLookup(tuple => tuple.SubscriptionInfo.Topic, tuple => tuple);

				_wildcardSubscriptions = _subscriptions
					.Where(tuple => tuple.SubscriptionInfo.WildcardTopic)
					.ToArray();
			}
		}
	}

	internal class EnumerableExtensions
	{
	}
}