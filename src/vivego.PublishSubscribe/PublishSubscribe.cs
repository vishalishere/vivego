﻿using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using Google.Protobuf;

using Grpc.Core;
using Grpc.Core.Utils;

using Microsoft.Extensions.Logging;

using vivego.PublishSubscribe.Topic;
using vivego.Serializer.Abstractions;

namespace vivego.PublishSubscribe
{
	public class PublishSubscribe : IPublishSubscribe, IDisposable
	{
		private readonly PublishSubscribeServerRouter _publishSubscribeServerRouter;
		private readonly PubSubService.PubSubServiceClient _pubSubServiceClient;
		private readonly ISerializer<byte[]> _serializer;
		private readonly ActionBlock<Message> _writer;
		private readonly IClientStreamWriter<Message> _clientStreamWriter;

		public PublishSubscribe(
			string address,
			int port,
			ILoggerFactory loggerFactory,
			ISerializer<byte[]> serializer)
		{
			_serializer = serializer;

			_publishSubscribeServerRouter = new PublishSubscribeServerRouter(loggerFactory,
				topic => new DefaultTopicFilter(topic));
			Server server = new Server
			{
				Services = {PubSubService.BindService(_publishSubscribeServerRouter)},
				Ports = {new ServerPort(address, port, ServerCredentials.Insecure)}
			};
			server.Start();

			//int boundPort = server.Ports.Single().BoundPort;
			//string boundAddr = $"{hostname}:{boundPort}";
			//var addr = $"{config.AdvertisedHostname ?? hostname}:{config.AdvertisedPort ?? boundPort}";

			foreach (ServerPort serverPort in server.Ports)
			{
				_pubSubServiceClient =
					new PubSubService.PubSubServiceClient(new Channel(serverPort.Host, serverPort.Port, ChannelCredentials.Insecure));
			}

			_clientStreamWriter = _pubSubServiceClient.Publish().RequestStream;
			_writer = new ActionBlock<Message>(message => _clientStreamWriter.WriteAsync(message),
				new ExecutionDataflowBlockOptions
				{
					MaxDegreeOfParallelism = 1
				});
		}

		public void Dispose()
		{
			_clientStreamWriter.CompleteAsync().Wait();
			_publishSubscribeServerRouter?.Dispose();
		}

		public void Publish<T>(string topic, T t)
		{
			Message message = new Message
			{
				Topic = topic,
				Data = ByteString.CopyFrom(_serializer.Serialize(t))
			};
			_writer.Post(message);
		}

		public IObservable<(string Topic, T Data)> Observe<T>(string topic, string group = null, bool hashBy = false)
		{
			return Observable.Create<(string Topic, T Data)>(async observer =>
			{
				try
				{
					await _pubSubServiceClient
						.Listen(new Subscription
						{
							Topic = topic,
							Group = group ?? string.Empty,
							HashBy = hashBy
						})
						.ResponseStream
						.ForEachAsync(message =>
						{
							T t = _serializer.Deserialize<T>(message.Data.ToByteArray());
							observer.OnNext((message.Topic, t));
							return Task.CompletedTask;
						})
						.ConfigureAwait(false);
					observer.OnCompleted();
				}
				catch (OperationCanceledException)
				{
					observer.OnCompleted();
				}
				catch (Exception e)
				{
					observer.OnError(e);
				}
			});
		}
	}
}