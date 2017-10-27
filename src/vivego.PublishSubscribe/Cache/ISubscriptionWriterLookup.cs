using System.Collections.Generic;

using vivego.PublishSubscribe.Topic;

namespace vivego.PublishSubscribe.Cache
{
	public interface ISubscriptionWriterLookup<TWriter>
	{
		bool Add(TWriter destination, Subscription subscription, ITopicFilter topicFilter);
		IEnumerable<Subscription> Remove(TWriter destination);
		(string Group, bool HashBy, TWriter[] Writer)[] Lookup(Message message);
		TWriter[] GetAll();
	}
}