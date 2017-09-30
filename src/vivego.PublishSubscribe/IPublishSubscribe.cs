using System;

namespace vivego.PublishSubscribe
{
	public interface IPublishSubscribe
	{
		void Publish<T>(string topic, T t, string group = null);
		IObservable<(string Topic, string Group, T Data)> AsObservable<T>(string topic, string group = null);
	}
}