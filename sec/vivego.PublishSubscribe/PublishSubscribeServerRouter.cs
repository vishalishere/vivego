using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Core.Utils;

using Microsoft.Extensions.Logging;

using vivego.core;
using vivego.PublishSubscribe.Topic;

namespace vivego.PublishSubscribe
{
	public class PublishSubscribeServerRouter : PubSubService.PubSubServiceBase, IDisposable
	{
		private readonly TaskCompletionSource<object> _cancellationTaskSource = new TaskCompletionSource<object>();
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private readonly ILogger<PublishSubscribeServerRouter> _logger;
		private readonly ISubscriptionWriterLookup<IServerStreamWriter<Message>> _lookup;
		private readonly Func<Subscription, ITopicFilter> _topicFilterFactory;
		private readonly Empty _empty = new Empty();

		public PublishSubscribeServerRouter(
			ILoggerFactory loggerFactory,
			Func<Subscription, ITopicFilter> topicFilterFactory)
		{
			_topicFilterFactory = topicFilterFactory;
			_logger = loggerFactory.CreateLogger<PublishSubscribeServerRouter>();
			_lookup = new SubscriptionWriterLookupCache<IServerStreamWriter<Message>>(
				new SubscriptionWriterLookup<IServerStreamWriter<Message>>());
		}

		public void Dispose()
		{
			_cancellationTaskSource.TrySetResult(null);
			_cancellationTokenSource.Dispose();
		}

		public override async Task<Empty> Publish(IAsyncStreamReader<Message> requestStream, ServerCallContext context)
		{
			await requestStream
				.ForEachAsync(message =>
				{
					IEnumerable<Task> writerTasks = _lookup
						.Lookup(message)
						.SelectMany(writers => writers.Select(serverStreamWriter => serverStreamWriter.WriteAsync(message)));
					return Task.WhenAll(writerTasks);
				});
			return _empty;
		}

		public override async Task Listen(Subscription subscription,
			IServerStreamWriter<Message> responseStream,
			ServerCallContext context)
		{
			ITopicFilter topicFilter = _topicFilterFactory(subscription);
			_lookup.Add(responseStream, subscription, topicFilter);
			try
			{
				using (CancellationTokenTaskSource<object> cancellationTokenTaskSource = new CancellationTokenTaskSource<object>(context.CancellationToken))
				{
					await Task
						.WhenAny(_cancellationTaskSource.Task, cancellationTokenTaskSource.Task)
						.ConfigureAwait(false);
				}
			}
			catch (Exception exception)
			{
				_logger.LogError(exception, "Error while writing messages to stream with subscription: {0}; {1}; {2}",
					subscription.Topic, subscription.Group, subscription.HashBy);
			}
			finally
			{
				_lookup.Remove(responseStream);
			}
		}
	}
}