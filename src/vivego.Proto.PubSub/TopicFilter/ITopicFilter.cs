namespace vivego.Proto.PubSub.TopicFilter
{
	public interface ITopicFilter
	{
		bool Matches(string topic);
	}
}