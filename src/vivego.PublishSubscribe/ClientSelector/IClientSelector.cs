using Vivego.PublishSubscribe.Grpc;

namespace vivego.PublishSubscribe.ClientSelector
{
	public interface IClientSelector
	{
		PubSub.PubSubClient[] Select(string topic, string group);
	}
}