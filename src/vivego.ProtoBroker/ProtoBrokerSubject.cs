using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using Proto;

using vivego.core;
using vivego.ProtoBroker.Actors;
using vivego.ProtoBroker.Messages;

namespace vivego.ProtoBroker
{
	internal class ProtoBrokerSubject<T> : ISubject<T>
	{
		private readonly IObservable<T> _observable;
		private readonly PID _publisherPid;
		private readonly string _topic;

		public ProtoBrokerSubject(PID brokerPid, 
			string topic, 
			string consumerGroup)
		{
			if (brokerPid == null)
			{
				throw new ArgumentNullException(nameof(brokerPid));
			}

			_topic = topic ?? throw new ArgumentNullException(nameof(topic));

			GetPublisherPiDsResponse getPublisherPiDsResponse = brokerPid.RequestAsync<GetPublisherPiDsResponse>(new GetPublisherPiDs()).Result;
			PID[] publishers = getPublisherPiDsResponse.PIds.ToArray();
			int hash = Math.Abs(topic.GetHashCode());
			_publisherPid = publishers[hash % publishers.Length];

			_observable = Observable.Create<T>(async (observer, cancellationToken) =>
				{
					try
					{
						Props props = Actor.FromProducer(() => new ConsumerActor<T>(observer));
						PID consumerActor = Actor.SpawnPrefix(props, $"Consumer_{topic}_{consumerGroup}");
						Subscribe addSubscription = new Subscribe
						{
							ConsumerActor = consumerActor,
							Topic = topic,
							Type = typeof(T).FullName
						};

						List<SubscribeAck> subscriptions = new List<SubscribeAck>();
						foreach (PID publisher in publishers.EmptyIfNull())
						{
							SubscribeAck subscribeAck = await publisher
								.RequestAsync<SubscribeAck>(addSubscription)
								.ConfigureAwait(false);
							subscriptions.Add(subscribeAck);
						}

						try
						{
							await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
						}
						finally
						{
							foreach (SubscribeAck subscription in subscriptions)
							{
								Unsubscribe removeSubscription = new Unsubscribe
								{
									SubscriptionId = subscription.SubscriptionId
								};
								await Task.WhenAll(publishers
										.Select(publisher => publisher.RequestAsync<UnsubscribeAck>(removeSubscription)))
									.ConfigureAwait(false);
							}

							consumerActor.Stop();
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
				})
				.Retry()
				.Publish()
				.RefCount();
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			return _observable.Subscribe(observer);
		}

		public void OnCompleted()
		{
		}

		public void OnError(Exception error)
		{
		}

		public void OnNext(T value)
		{
			_publisherPid.Tell(new Publish
			{
				//Message = value,
				Topic = _topic
			});
		}
	}
}