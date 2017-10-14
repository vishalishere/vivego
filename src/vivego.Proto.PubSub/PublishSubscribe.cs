using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Google.Protobuf;

using Microsoft.Extensions.Logging;

using Proto;
using Proto.Remote;

using vivego.core;
using vivego.Proto.PubSub.Messages;
using vivego.Proto.PubSub.Route;
using vivego.Proto.PubSub.Topic;
using vivego.Serializer.Abstractions;

namespace vivego.Proto.PubSub
{
	public class PublishSubscribe : DisposableBase, IPublishSubscribe
	{
		private readonly PID _localRouter;
		private readonly ISerializer<byte[]> _serializer;

		static PublishSubscribe()
		{
			Serialization.RegisterFileDescriptor(Messages.ProtosReflection.Descriptor);
		}

		public PublishSubscribe(string clusterName,
			ISerializer<byte[]> serializer,
			ILoggerFactory loggerFactory) : this(clusterName, serializer, loggerFactory, new HashByRouteSelector(), subscription => new DefaultTopicFilter(subscription))
		{
		}

		public PublishSubscribe(
			string clusterName,
			ISerializer<byte[]> serializer,
			ILoggerFactory loggerFactory,
			IRouteSelector routeSelector,
			Func<Subscription, ITopicFilter> topicFilterFactory)
		{
			_localRouter = new PublishSubscribeRouterActor(clusterName, loggerFactory, routeSelector, topicFilterFactory).PubSubRouterActorPid;
			_serializer = serializer;

			RegisterDisposable(new AnonymousDisposable(() => _localRouter.Stop()));
		}

		public void Publish<T>(string topic, T t, string hashBy = null)
		{
			byte[] serialized = _serializer.Serialize(t);
			_localRouter.Tell(new Message
			{
				Topic = topic,
				HashBy = hashBy ?? string.Empty,
				Data = ByteString.CopyFrom(serialized)
			});
		}

		public IObservable<(string Topic, T Data)> Observe<T>(string topic, string group = null)
		{
			if (string.IsNullOrEmpty(topic))
			{
				throw new ArgumentNullException(nameof(topic));
			}

			return Observable.Create<(string Topic, T Data)>(observer =>
			{
				Props props = Actor.FromFunc(context =>
				{
					switch (context.Message)
					{
						case Message message:
							T deserialized = _serializer.Deserialize<T>(message.Data.ToByteArray());
							observer.OnNext((message.Topic, deserialized));
							break;
						case Stopped _:
							observer.OnCompleted();
							break;
					}

					return Task.CompletedTask;
				});
				PID self = Actor.Spawn(props);
				_localRouter.Tell(new Subscription
				{
					Topic = topic,
					Group = group ?? string.Empty,
					PID = self
				});
				return new AnonymousDisposable(() => { self.Stop(); });
			});
		}
	}
}