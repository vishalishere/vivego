using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

using vivego.core;
using vivego.PublishSubscribe.DistributedCache.Proto;

namespace vivego.PublishSubscribe.DistributedCache
{
	public class PublishSubscribeDistributedCache : DisposableBase, IDistributedCache
	{
		private readonly ConcurrentDictionary<string, TaskCompletionSource<byte[]>> _completionSources =
			new ConcurrentDictionary<string, TaskCompletionSource<byte[]>>();

		private readonly string _getTopic;
		private readonly IPublishSubscribe _publishSubscribe;
		private readonly string _removeTopic;
		private readonly string _selfTopic;
		private readonly string _setTopic;

		public PublishSubscribeDistributedCache(string cacheGroup,
			IPublishSubscribe publishSubscribe)
		{
			_publishSubscribe = publishSubscribe ?? throw new ArgumentNullException(nameof(publishSubscribe));

			MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions
			{
				CompactionPercentage = 0.1
			});

			_getTopic = cacheGroup + "_get";
			_setTopic = cacheGroup + "_set";
			_removeTopic = cacheGroup + "_remove";

			publishSubscribe
				.Observe<byte[]>(_setTopic)
				.Subscribe(_ =>
				{
					CacheEntry cacheEntry = CacheEntry.Parser.ParseFrom(_.Data);
					memoryCache.Set(cacheEntry.Key, cacheEntry);
				}, CancellationToken);

			publishSubscribe
				.Observe<string>(_removeTopic)
				.Subscribe(_ => { memoryCache.Remove(_.Data); }, CancellationToken);

			publishSubscribe
				.Observe<byte[]>(_getTopic)
				.Subscribe(_ =>
				{
					CacheRequest cacheRequest = CacheRequest.Parser.ParseFrom(_.Data);
					CacheEntry value = memoryCache.Get<CacheEntry>(cacheRequest.Key) ?? new CacheEntry
					{
						Key = cacheRequest.Key
					};
					publishSubscribe.Publish(cacheRequest.TopicResponse, value.ToByteArray());
				}, CancellationToken);

			_selfTopic = Guid.NewGuid().ToString();
			publishSubscribe
				.Observe<byte[]>(_selfTopic)
				.Subscribe(_ =>
				{
					CacheEntry cacheEntry = CacheEntry.Parser.ParseFrom(_.Data);
					if (_completionSources.TryRemove(cacheEntry.Key, out TaskCompletionSource<byte[]> taskCompletionSource))
					{
						taskCompletionSource.TrySetResult(cacheEntry.Value.ToByteArray());
					}
				}, CancellationToken);
		}

		public byte[] Get(string key)
		{
			return GetAsync(key).Result;
		}

		public async Task<byte[]> GetAsync(string key, CancellationToken token = new CancellationToken())
		{
			if (!_completionSources.TryGetValue(key, out TaskCompletionSource<byte[]> taskCompletionSource))
			{
				taskCompletionSource = new TaskCompletionSource<byte[]>();
				if (!_completionSources.TryAdd(key, taskCompletionSource))
				{
					return await GetAsync(key, token).ConfigureAwait(false);
				}
			}

			_publishSubscribe.Publish(_getTopic, new CacheRequest
			{
				Key = key,
				TopicResponse = _selfTopic
			}.ToByteArray());

			if (token.CanBeCanceled)
			{
				await Task.WhenAny(
					taskCompletionSource.Task,
					Task.FromCanceled<object>(token)).ConfigureAwait(false);
				_completionSources.TryRemove(key, out var _);
				token.ThrowIfCancellationRequested();
			}
			else
			{
				await taskCompletionSource.Task.ConfigureAwait(false);
				_completionSources.TryRemove(key, out var _);
			}

			return taskCompletionSource.Task.Result;
		}

		public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
		{
			_publishSubscribe.Publish(_setTopic, new CacheEntry
			{
				Key = key,
				Value = ByteString.CopyFrom(value)
			}.ToByteArray());
		}

		public Task SetAsync(string key,
			byte[] value,
			DistributedCacheEntryOptions options,
			CancellationToken token = new CancellationToken())
		{
			Set(key, value, options);
			return Task.CompletedTask;
		}

		public void Refresh(string key)
		{
		}

		public Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
		{
			return Task.CompletedTask;
		}

		public void Remove(string key)
		{
			_publishSubscribe.Publish(_removeTopic, key);
		}

		public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
		{
			Remove(key);
			return Task.CompletedTask;
		}
	}
}