namespace vivego.PublishSubscribe.Topic
{
	public interface ITopicFilter
	{
		bool Matches(string topic);
	}
}