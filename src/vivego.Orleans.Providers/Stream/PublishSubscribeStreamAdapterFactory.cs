using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Orleans.Providers;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Serialization;
using Orleans.Streams;

using vivego.Proto.PubSub;

namespace vivego.Orleans.Providers.Stream
{
	public class PublishSubscribeStreamAdapterFactory : IQueueAdapterFactory, IQueueAdapter, IStreamFailureHandler
	{
		private SerializationManager _serializationManager;
		private IPublishSubscribe _publishSubscribe;

		private readonly ConcurrentDictionary<string, PublishSubscribeQueueAdapterReceiver> _queueAdapterReceivers =
			new ConcurrentDictionary<string, PublishSubscribeQueueAdapterReceiver>();

		private IQueueAdapterCache _adapterCache;
		private HashRingBasedStreamQueueMapper _streamQueueMapper;

		public Task QueueMessageBatchAsync<T>(Guid streamGuid,
			string streamNamespace,
			IEnumerable<T> events,
			StreamSequenceToken token,
			Dictionary<string, object> requestContext)
		{
			if (_publishSubscribe == null)
			{
				return Task.CompletedTask;
			}

			QueueId queueId = _streamQueueMapper.GetQueueForStream(streamGuid, streamNamespace);
			string queueidString = queueId.ToString();
			foreach (T @event in events)
			{
				MessageData messageData = new MessageData
				{
					Payload = _serializationManager.SerializeToByteArray(@event),
					StreamGuid = streamGuid,
					StreamNamespace = streamNamespace,
					SequenceNumber = DateTime.UtcNow.Ticks,
					RequestContext = requestContext
				};

				_publishSubscribe.Publish(queueidString, messageData);
			}

			return Task.CompletedTask;
		}

		public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
		{
			int maxRetry = 3;
			while (maxRetry-->0)
			{
				if (!_queueAdapterReceivers.TryGetValue(queueId.ToString(), out PublishSubscribeQueueAdapterReceiver adapterReceiver))
				{
					adapterReceiver = new PublishSubscribeQueueAdapterReceiver(_publishSubscribe, queueId);
					if (!_queueAdapterReceivers.TryAdd(queueId.ToString(), adapterReceiver))
					{
						continue;
					}
				}

				return adapterReceiver;
			}

			throw new Exception("Internal exception, should not happen!");
		}

		public string Name { get; set; }
		public bool IsRewindable => true;
		public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

		public void Init(IProviderConfiguration config, string providerName, IServiceProvider serviceProvider)
		{
			Name = providerName;

			_publishSubscribe = serviceProvider.GetRequiredService<IPublishSubscribe>();
			_serializationManager = serviceProvider.GetRequiredService<SerializationManager>();
			ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

			PublishSubscribeStreamConfiguration publishSubscribeStreamConfiguration = new PublishSubscribeStreamConfiguration(config);
			_adapterCache = new SimpleQueueAdapterCache(publishSubscribeStreamConfiguration.InMemoryCacheSize, providerName, loggerFactory);
			_streamQueueMapper = new HashRingBasedStreamQueueMapper(publishSubscribeStreamConfiguration.NumberOfQueues, providerName);
		}

		public Task<IQueueAdapter> CreateAdapter()
		{
			IQueueAdapter adapter = this;
			return Task.FromResult(adapter);
		}

		public IQueueAdapterCache GetQueueAdapterCache()
		{
			return _adapterCache;
		}

		public IStreamQueueMapper GetStreamQueueMapper()
		{
			return _streamQueueMapper;
		}

		public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
		{
			IStreamFailureHandler streamFailureHandler = this;
			return Task.FromResult(streamFailureHandler);
		}

		public Task OnDeliveryFailure(GuidId subscriptionId, string streamProviderName, IStreamIdentity streamIdentity,
			StreamSequenceToken sequenceToken)
		{
			return Task.CompletedTask;
		}

		public Task OnSubscriptionFailure(GuidId subscriptionId,
			string streamProviderName,
			IStreamIdentity streamIdentity,
			StreamSequenceToken sequenceToken)
		{
			return Task.CompletedTask;
		}

		public bool ShouldFaultSubsriptionOnError => false;
	}
}