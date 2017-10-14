namespace vivego.Proto.PubSub.Topic
{
	public interface ITopicFilter
	{
		bool Matches(string topic);
	}
}