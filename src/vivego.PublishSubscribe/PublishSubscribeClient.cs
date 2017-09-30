using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using Google.Protobuf;

using Grpc.Core;

using vivego.core;
using vivego.PublishSubscribe.ClientSelector;
using vivego.PublishSubscribe.ExtensionMethods;

using Vivego.PublishSubscribe.Grpc;

namespace vivego.PublishSubscribe
{
	public class PublishSubscribeClient : DisposableBase, IPublishSubscribe
	{
		private readonly ISubject<Message> _messages = Subject.Synchronize(new Subject<Message>());
		private readonly ISerializer<byte[]> _serializer;
		private readonly IObservable<IClientSelector> _subscriberSelectorChangeObservable;

		public PublishSubscribeClient(
			ISerializer<byte[]> serializer,
			IObservable<IClientSelector> publisherSelectorObservable,
			IObservable<IClientSelector> subscriberSelectorChangeObservable)
		{
			if (publisherSelectorObservable == null)
			{
				throw new ArgumentNullException(nameof(publisherSelectorObservable));
			}

			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			_subscriberSelectorChangeObservable = subscriberSelectorChangeObservable ??
				throw new ArgumentNullException(nameof(subscriberSelectorChangeObservable));

			IDisposable subscription = publisherSelectorObservable
				.Select(selector =>
				{
					return _messages
						.GroupBy(message => (Topic: message.Topic, Group: message.Group))
						.Select(groupedObservable =>
						{
							AsyncClientStreamingCall<Message, Empty>[] publishers = selector
								.Select(groupedObservable.Key.Topic, groupedObservable.Key.Group)
								.Select(client => client.Publish())
								.ToArray();

							return groupedObservable.Subscribe(message =>
							{
								IEnumerable<Task> publishTasks = publishers.Select(call => call.RequestStream.WriteAsync(message));
								Task.WaitAll(publishTasks.ToArray(), CancellationToken);
							});
						})
						.Subscribe();
				})
				.Scan((IDisposable) new AnonymousDisposable(() => { }), (previousSubscription, innerSubscription) =>
				{
					using (previousSubscription)
					{
						return innerSubscription;
					}
				})
				.SubscribeOn(ThreadPoolScheduler.Instance)
				.Subscribe();
			RegisterDisposable(subscription);
		}

		public void Publish<T>(string topic, T t, string group = null)
		{
			byte[] serialized = _serializer.Serialize(t);
			Message message = new Message
			{
				Data = ByteString.CopyFrom(serialized),
				Group = group ?? string.Empty,
				Topic = topic ?? string.Empty
			};
			_messages.OnNext(message);
		}

		public IObservable<(string Topic, string Group, T Data)> AsObservable<T>(string topic, string group = null)
		{
			return Observable.Create<(string Topic, string Group, T Data)>(observer =>
			{
				return _subscriberSelectorChangeObservable
					.Select(clientSelector => clientSelector
						.Select(topic, group)
						.Select(client => Observe<T>(topic, group, client))
						.Merge()
						.Subscribe(observer))
					.Scan((IDisposable) new AnonymousDisposable(() => { }), (previousSubscription, subscription) =>
					{
						using (previousSubscription)
						{
							return subscription;
						}
					})
					.Subscribe();
			});
		}

		private IObservable<(string Topic, string Group, T Data)> Observe<T>(string topic, string group,
			PubSub.PubSubClient client)
		{
			return Observable.Create<(string Topic, string Group, T Data)>((observer, cancellationToken) =>
			{
				AsyncServerStreamingCall<Message> listenStream = client.Listen(new Subscription
				{
					Topic = topic,
					Group = group ?? string.Empty
				}, cancellationToken: cancellationToken);

				IDisposable innerSubscription = listenStream
					.ToObservable()
					.Select(message =>
					{
						byte[] byteArray = message.Data.ToByteArray();
						T t = _serializer.Deserialize<T>(byteArray);
						return (message.Topic, message.Group, t);
					})
					.Subscribe(observer);
				return Task.FromResult(innerSubscription);
			}).Retry();
		}
	}
}