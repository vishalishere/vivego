using System;

namespace vivego.PublishSubscribe
{
	public interface IPublishSubscribe
	{
		void Publish<T>(string topic, T t);
		IObservable<(string Topic, T Data)> Observe<T>(string topic, string group = null, bool hashBy = false);
	}
}