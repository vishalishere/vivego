﻿using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Google.Protobuf;

using Microsoft.Extensions.Logging;

using Proto;
using Proto.Remote;

using vivego.core;
using vivego.PublishSubscribe.ProtoActor.Proto;
using vivego.PublishSubscribe.Topic;
using vivego.Serializer.Abstractions;

namespace vivego.PublishSubscribe.ProtoActor
{
	public class PublishSubscribe : DisposableBase, IPublishSubscribe
	{
		private readonly PID _localRouter;
		private readonly ISerializer<byte[]> _serializer;

		static PublishSubscribe()
		{
			Serialization.RegisterFileDescriptor(Proto.ProtosReflection.Descriptor);
		}

		public PublishSubscribe(string clusterName,
			ISerializer<byte[]> serializer,
			ILoggerFactory loggerFactory) : this(clusterName, serializer, loggerFactory, subscription => new DefaultTopicFilter(subscription))
		{
		}

		public PublishSubscribe(
			string clusterName,
			ISerializer<byte[]> serializer,
			ILoggerFactory loggerFactory,
			Func<Subscription, ITopicFilter> topicFilterFactory)
		{
			_localRouter = new PublishSubscribeRouterActor(clusterName, loggerFactory, topicFilterFactory).PubSubRouterActorPid;
			_serializer = serializer;

			RegisterDisposable(new AnonymousDisposable(() => _localRouter.Stop()));
		}

		public void Publish<T>(string topic, T t)
		{
			byte[] serialized = _serializer.Serialize(t);
			Message message = new Message
			{
				Topic = topic,
				Data = ByteString.CopyFrom(serialized)
			};
			_localRouter.Tell(message);
		}

		public IObservable<(string Topic, T Data)> Observe<T>(string topic, string group = null, bool hashBy = false)
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
				_localRouter.Tell(new ProtoSubscription
				{
					PID = self,
					Subscription = new Subscription
					{
						Topic = topic,
						Group = group ?? string.Empty,
						HashBy = hashBy
					}
				});
				return new AnonymousDisposable(() => { self.Stop(); });
			});
		}
	}
}