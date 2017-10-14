using System;

namespace vivego.Proto.PubSub
{
	public interface IPublishSubscribe
	{
		void Publish<T>(string topic, T t, string hashBy = null);
		IObservable<(string Topic, T Data)> Observe<T>(string topic, string group = null);
	}
}