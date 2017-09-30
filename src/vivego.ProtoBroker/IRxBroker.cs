using System.Reactive.Subjects;

namespace vivego.ProtoBroker
{
	public interface IRxBroker
	{
		ISubject<T> MakeSubject<T>(string topic, string consumerGroup = null);
	}
}