using System;

namespace vivego.Proto.PubSub
{
	public interface IPublishSubscribe : IDisposable
	{
		void Publish<T>(string topic, T t, string group = null);
		IObservable<T> Observe<T>(string topic, string group = null);
	}
}