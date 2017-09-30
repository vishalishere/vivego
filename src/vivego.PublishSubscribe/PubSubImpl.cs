using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using Grpc.Core;

using vivego.PublishSubscribe.ExtensionMethods;

using Vivego.PublishSubscribe.Grpc;

namespace vivego.PublishSubscribe
{
	internal class PubSubImpl : PubSub.PubSubBase
	{
		private readonly CancellationToken _cancellationToken;

		private readonly ConcurrentDictionary<string, List<(SubscriptionInfo SubscriptionInfo, IServerStreamWriter<Message> ResponseStream)>> _groupSubscribers =
			new ConcurrentDictionary<string, List<(SubscriptionInfo SubscriptionInfo, IServerStreamWriter<Message> ResponseStream)>>();

		private readonly ISubject<Message> _groupedSubject = Subject.Synchronize(new Subject<Message>());
		private readonly ISubject<Message> _subject = Subject.Synchronize(new Subject<Message>());

		public PubSubImpl(CancellationToken cancellationToken)
		{
			_cancellationToken = cancellationToken;
			Observable
				.Interval(TimeSpan.FromSeconds(1))
				.Subscribe(_ =>
				{
					_subject.OnNext(new Message
					{
						Topic = PubSubServer.HeartbeatTopic,
						Data = ByteString.Empty
					});
				}, cancellationToken);

			_groupedSubject
				.GroupBy(message => message.Group)
				.Subscribe(groupedObservable =>
				{
					string group = groupedObservable.Key;
					List<(SubscriptionInfo SubscriptionInfo, IServerStreamWriter<Message> ResponseStream)> groupReceivers = GetGroupReceivers(group);
					long counter = 0;
					groupedObservable
						.Subscribe(message =>
						{
							while (true)
							{
								try
								{
									(SubscriptionInfo SubscriptionInfo, IServerStreamWriter<Message> ResponseStream)[] matchesResponseStreams;
									lock (groupReceivers)
									{
										matchesResponseStreams = groupReceivers
											.Where(tuple => tuple.SubscriptionInfo.Matches(message.Topic))
											.ToArray();
									}

									if (matchesResponseStreams.Length > 0)
									{
										(SubscriptionInfo SubscriptionInfo, IServerStreamWriter<Message> ResponseStream) tuple = matchesResponseStreams[Interlocked.Increment(ref counter) % matchesResponseStreams.Length];
										tuple.ResponseStream.WriteAsync(message).GetAwaiter().GetResult();
									}

									return;
								}
								catch (Exception e)
								{
									Console.Out.WriteLine(e);
								}
							}
						}, cancellationToken);
				}, cancellationToken);
		}

		private List<(SubscriptionInfo SubscriptionInfo, IServerStreamWriter<Message> ResponseStream)> GetGroupReceivers(string group)
		{
			List<(SubscriptionInfo SubscriptionInfo, IServerStreamWriter<Message> ResponseStream)> groupReceivers = _groupSubscribers
				.SecureAddOrGet(group, _ => new List<(SubscriptionInfo SubscriptionInfo, IServerStreamWriter<Message> ResponseStream)>());
			return groupReceivers;
		}

		public Task<Empty> Publish(Message request, ServerCallContext context)
		{
			if (string.IsNullOrEmpty(request.Group))
			{
				_subject.OnNext(request);
			}
			else
			{
				_groupedSubject.OnNext(request);
			}

			return Task.FromResult(new Empty());
		}

		public override async Task Listen(Subscription request,
			IServerStreamWriter<Message> responseStream,
			ServerCallContext context)
		{
			SubscriptionInfo subscriptionInfo = new SubscriptionInfo(request.Topic);
			if (string.IsNullOrEmpty(request.Group))
			{
				using (_subject
					.Where(message => subscriptionInfo.Matches(message.Topic))
					.Subscribe(message => responseStream.WriteAsync(message).GetAwaiter().GetResult()))
				{
					await Task
						.WhenAny(Task.FromCanceled(_cancellationToken), Task.FromCanceled(context.CancellationToken))
						.ConfigureAwait(false);
				}

				return;
			}

			List<(SubscriptionInfo SubscriptionInfo, IServerStreamWriter<Message> ResponseStream)> groupReceivers =
				GetGroupReceivers(request.Group);
			(SubscriptionInfo, IServerStreamWriter<Message>) tuple = (subscriptionInfo, responseStream);
			try
			{
				lock (groupReceivers)
				{
					groupReceivers.Add(tuple);
				}

				await Task
					.WhenAny(Task.FromCanceled(_cancellationToken), Task.FromCanceled(context.CancellationToken))
					.ConfigureAwait(false);
			}
			finally
			{
				lock (groupReceivers)
				{
					groupReceivers.Remove(tuple);
				}
			}
		}
	}
}