using System.Collections.Generic;
using System.Linq;

using vivego.PublishSubscribe.Topic;

namespace vivego.PublishSubscribe.Cache
{
	public class SubscriptionWriterLookupCache<TWriter> : ISubscriptionWriterLookup<TWriter>
	{
		private readonly ISubscriptionWriterLookup<TWriter> _innerSubscriptionWriterLookup;
		private readonly Dictionary<string, (string Group, bool HashBy, TWriter[] Writer)[]> _lookupCache = 
			new Dictionary<string, (string Group, bool HashBy, TWriter[] Writer)[]>();

		public SubscriptionWriterLookupCache(ISubscriptionWriterLookup<TWriter> innerSubscriptionWriterLookup)
		{
			_innerSubscriptionWriterLookup = innerSubscriptionWriterLookup;
		}

		public bool Add(TWriter destination, Subscription subscription, ITopicFilter topicFilter)
		{
			try
			{
				return _innerSubscriptionWriterLookup.Add(destination, subscription, topicFilter);
			}
			finally
			{
				_lookupCache.Clear();
			}
		}

		public IEnumerable<Subscription> Remove(TWriter destination)
		{
			try
			{
				return _innerSubscriptionWriterLookup.Remove(destination).ToArray();
			}
			finally
			{
				_lookupCache.Clear();
			}
		}

		public (string Group, bool HashBy, TWriter[] Writer)[] Lookup(Message message)
		{
			string cacheKey = $"{message.Topic}";
			if (!_lookupCache.TryGetValue(cacheKey, out (string Group, bool HashBy, TWriter[] Writer)[] dest))
			{
				dest = _innerSubscriptionWriterLookup.Lookup(message);
				_lookupCache.Add(cacheKey, dest);
			}

			return dest;
		}

		public (TWriter, Subscription)[] GetAll()
		{
			return _innerSubscriptionWriterLookup.GetAll();
		}
	}
}