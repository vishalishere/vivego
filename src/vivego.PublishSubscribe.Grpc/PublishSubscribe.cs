using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using Google.Protobuf;

using Grpc.Core;
using Grpc.Core.Utils;

using Microsoft.Extensions.Logging;

using vivego.core;
using vivego.PublishSubscribe.Topic;
using vivego.Serializer.Abstractions;

namespace vivego.PublishSubscribe.Grpc
{
	public class PublishSubscribe : DisposableBase, IPublishSubscribe
	{
		private readonly ISerializer<byte[]> _serializer;
		private readonly ISubject<IEnumerable<PubSubService.PubSubServiceClient>> _clientsSource = 
			new ReplaySubject<IEnumerable<PubSubService.PubSubServiceClient>>(1);
		private readonly ISubject<Message> _publishSubject = Subject.Synchronize(new Subject<Message>());

		public PublishSubscribe(
			IPEndPoint serverEndPoint,
			IObservable<IEnumerable<DnsEndPoint>> seedsEndpointObservable,
			ILoggerFactory loggerFactory,
			ISerializer<byte[]> serializer,
			ChannelCredentials channelCredentials = null)
		{
			_serializer = serializer;

			PublishSubscribeServerRouter publishSubscribeServerRouter = new PublishSubscribeServerRouter(loggerFactory, topic => new DefaultTopicFilter(topic));
			RegisterDisposable(publishSubscribeServerRouter);
			Server server = new Server
			{
				Services = {PubSubService.BindService(publishSubscribeServerRouter)},
				Ports = {new ServerPort(serverEndPoint.Address.ToString(), serverEndPoint.Port, ServerCredentials.Insecure)}
			};
			server.Start();

			seedsEndpointObservable
				.Select(seedEndPoints => seedEndPoints
					.Select(dnsEndPoint => new Channel(dnsEndPoint.Host, dnsEndPoint.Port, channelCredentials ?? ChannelCredentials.Insecure))
					.Select(channel => new PubSubService.PubSubServiceClient(channel)))
				.Subscribe(_clientsSource, CancellationToken);

			ILogger<PublishSubscribe> logger = loggerFactory.CreateLogger<PublishSubscribe>();
			_clientsSource
				.SelectMany(clients => clients.Select(client => client.Publish().RequestStream))
				.CombineLatest(_publishSubject, (streamWriter, message) =>
				{
					try
					{
						streamWriter.WriteAsync(message).GetAwaiter().GetResult();
					}
					catch (Exception e)
					{
						logger.LogError(e, "Error while sending message");
					}

					return Unit.Default;
				})
				.Subscribe(CancellationToken);
		}

		public void Publish<T>(string topic, T t)
		{
			Message message = new Message
			{
				Topic = topic,
				Data = ByteString.CopyFrom(_serializer.Serialize(t))
			};
			_publishSubject.OnNext(message);
		}

		public IObservable<(string Topic, T Data)> Observe<T>(string topic, string group = null, bool hashBy = false)
		{
			return Observable.Create<(string Topic, T Data)>(async (observer, cancellationToken) =>
			{
				try
				{
					Subscription subscription = new Subscription
					{
						Topic = topic,
						Group = group ?? string.Empty,
						HashBy = hashBy
					};
					_clientsSource
						.SelectMany(clients => clients)
						.Select(client =>
						{
							AsyncServerStreamingCall<Message> streamingCall = client.Listen(subscription);
							return streamingCall.ResponseStream.ForEachAsync(message =>
							{
								T t = _serializer.Deserialize<T>(message.Data.ToByteArray());
								observer.OnNext((message.Topic, t));
								return Task.CompletedTask;
							});
						})
						.Subscribe(cancellationToken);

					using (var cancellationTokenTaskSource = new CancellationTokenTaskSource<object>(cancellationToken))
					{
						await Task
							.WhenAny(CancellationTokenTask, cancellationTokenTaskSource.Task)
							.ConfigureAwait(false);
					}

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