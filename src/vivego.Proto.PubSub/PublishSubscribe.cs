using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Google.Protobuf;

using Microsoft.Extensions.Logging;

using Proto;
using Proto.Cluster;
using Proto.Remote;

using vivego.core;
using vivego.Proto.PubSub.Messages;
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

		private PublishSubscribe(PID localRouter,
			ISerializer<byte[]> serializer)
		{
			_localRouter = localRouter;
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		}

		public static PublishSubscribe StartCluster(
			string clusterName,
			string address,
			int port,
			IClusterProvider clusterProvider,
			ISerializer<byte[]> serializer,
			ILoggerFactory loggerFactory)
		{
			Cluster.Start(clusterName, address, port, clusterProvider);
			PubSubRouterActor routerActor = new PubSubRouterActor(loggerFactory);
			return new PublishSubscribe(routerActor.PubSubRouterActorPid, serializer);
		}

		protected override void Cleanup()
		{
			Cluster.Shutdown();
		}

		public void Publish<T>(string topic, T t, string group = null)
		{
			byte[] serialized = _serializer.Serialize(t);
			_localRouter.Tell(new Message
			{
				Topic = topic,
				Group = group ?? string.Empty,
				Data = ByteString.CopyFrom(serialized)
			});
		}

		public IObservable<T> Observe<T>(string topic, string group = null)
		{
			if (string.IsNullOrEmpty(topic))
			{
				throw new ArgumentNullException(nameof(topic));
			}

			return Observable.Create<T>(observer =>
			{
				Props props = Actor.FromFunc(context =>
				{
					switch (context.Message)
					{
						case Message message:
							T deserialized = _serializer.Deserialize<T>(message.Data.ToByteArray());
							observer.OnNext(deserialized);
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