﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

using Orleans.Serialization;
using Orleans.Streams;

using vivego.PublishSubscribe;

namespace vivego.Orleans.Providers.Stream
{
	public class PublishSubscribeQueueAdapterReceiver : IQueueAdapterReceiver
	{
		private readonly ConcurrentQueue<IBatchContainer> _batchContainers = new ConcurrentQueue<IBatchContainer>();
		private readonly IPublishSubscribe _publishSubscribe;
		private readonly SerializationManager _serializationManager;
		private readonly QueueId _queueId;

		private IDisposable _subscription;

		public PublishSubscribeQueueAdapterReceiver(QueueId queueId,
			IPublishSubscribe publishSubscribe,
			SerializationManager serializationManager)
		{
			_publishSubscribe = publishSubscribe;
			_serializationManager = serializationManager;
			_queueId = queueId;
		}

		public int BufferLength => _batchContainers.Count;

		public Task Initialize(TimeSpan timeout)
		{
			_subscription?.Dispose();
			_subscription = _publishSubscribe
				.Observe<MessageData>(_queueId.ToString(), _queueId.ToString())
				.Subscribe(tuple =>
				{
					switch (tuple.Data)
					{
						case MessageData messageData:
							messageData.SerializationManager = _serializationManager;
							break;
					}

					_batchContainers.Enqueue(tuple.Data);
				});

			return Task.CompletedTask;
		}

		public Task<IList<IBatchContainer>> GetQueueMessagesAsync(int maxCount)
		{
			IList<IBatchContainer> result = new List<IBatchContainer>();
			while (_batchContainers.TryDequeue(out IBatchContainer batch))
			{
				result.Add(batch);
				if (result.Count >= maxCount)
				{
					break;
				}
			}

			return Task.FromResult(result);
		}

		public Task MessagesDeliveredAsync(IList<IBatchContainer> messages)
		{
			return Task.CompletedTask;
		}

		public Task Shutdown(TimeSpan timeout)
		{
			_subscription?.Dispose();
			_subscription = null;
			return Task.CompletedTask;
		}
	}
}