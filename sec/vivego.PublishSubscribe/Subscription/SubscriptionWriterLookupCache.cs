using System.Collections.Generic;

using vivego.PublishSubscribe.Topic;

namespace vivego.PublishSubscribe
{
	public class SubscriptionWriterLookupCache<TWriter> : ISubscriptionWriterLookup<TWriter>
	{
		private readonly ISubscriptionWriterLookup<TWriter> _innerSubscriptionWriterLookup;
		private readonly Dictionary<string, TWriter[][]> _lookupCache = new Dictionary<string, TWriter[][]>();

		public SubscriptionWriterLookupCache(ISubscriptionWriterLookup<TWriter> innerSubscriptionWriterLookup)
		{
			_innerSubscriptionWriterLookup = innerSubscriptionWriterLookup;
		}

		public void Add(TWriter destination, Subscription subscription, ITopicFilter topicFilter)
		{
			_innerSubscriptionWriterLookup.Add(destination, subscription, topicFilter);
			_lookupCache.Clear();
		}

		public void Remove(TWriter destination)
		{
			_innerSubscriptionWriterLookup.Remove(destination);
			_lookupCache.Clear();
		}

		public TWriter[][] Lookup(Message message)
		{
			string cacheKey = $"{message.Topic}";
			if (!_lookupCache.TryGetValue(cacheKey, out TWriter[][] dest))
			{
				dest = _innerSubscriptionWriterLookup.Lookup(message);
				_lookupCache.Add(cacheKey, dest);
			}

			return dest;
		}
	}
}