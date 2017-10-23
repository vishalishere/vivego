using vivego.PublishSubscribe.Topic;

namespace vivego.PublishSubscribe
{
	public interface ISubscriptionWriterLookup<TDest>
	{
		void Add(TDest destination, Subscription subscription, ITopicFilter topicFilter);
		void Remove(TDest destination);
		TDest[][] Lookup(Message message);
	}
}