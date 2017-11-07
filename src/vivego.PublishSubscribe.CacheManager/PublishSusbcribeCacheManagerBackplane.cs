using System;
using System.Reactive.Linq;
using System.Text;

using CacheManager.Core;
using CacheManager.Core.Internal;
using CacheManager.Core.Logging;

using static CacheManager.Core.Utility.Guard;

namespace vivego.PublishSubscribe.CacheManager
{
	public class PublishSusbcribeCacheManagerBackplane : CacheBackplane
	{
		private readonly string _channelName;
		private readonly byte[] _identifier;
		private readonly IPublishSubscribe _publishSubscribe;
		private readonly IDisposable _subscription;

		public PublishSusbcribeCacheManagerBackplane(
			IPublishSubscribe publishSubscribe,
			ILoggerFactory loggerFactory,
			ICacheManagerConfiguration configuration) : base(configuration)
		{
			NotNull(configuration, nameof(publishSubscribe));
			NotNull(loggerFactory, nameof(loggerFactory));
			NotNull(loggerFactory, nameof(loggerFactory));

			_publishSubscribe = publishSubscribe;
			_channelName = configuration.BackplaneChannelName ?? "CacheManagerBackplane";
			_identifier = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString());

			ILogger logger = loggerFactory.CreateLogger(this);
			_subscription = publishSubscribe
				.Observe<byte[]>(_channelName)
				.SelectMany(serializedBackplaneMessage => BackplaneMessage.Deserialize(serializedBackplaneMessage.Data, _identifier))
				.Subscribe(message =>
				{
					if (logger.IsEnabled(LogLevel.Information))
					{
						logger.LogInfo("Backplane got notified with new message.");
					}

					switch (message.Action)
					{
						case BackplaneAction.Clear:
							TriggerCleared();
							break;
						case BackplaneAction.ClearRegion:
							TriggerClearedRegion(message.Region);
							break;
						case BackplaneAction.Changed:
							if (string.IsNullOrWhiteSpace(message.Region))
							{
								TriggerChanged(message.Key, message.ChangeAction);
							}
							else
							{
								TriggerChanged(message.Key, message.Region, message.ChangeAction);
							}

							break;
						case BackplaneAction.Removed:
							if (string.IsNullOrWhiteSpace(message.Region))
							{
								TriggerRemoved(message.Key);
							}
							else
							{
								TriggerRemoved(message.Key, message.Region);
							}

							break;
					}
				});
		}

		protected override void Dispose(bool managed)
		{
			_subscription.Dispose();
			base.Dispose(managed);
		}

		/// <summary>
		///     Notifies other cache clients about a changed cache key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="action">The cache action.</param>
		public override void NotifyChange(string key, CacheItemChangedEventAction action)
		{
			PublishMessage(BackplaneMessage.ForChanged(_identifier, key, action));
		}

		/// <summary>
		///     Notifies other cache clients about a changed cache key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="region">The region.</param>
		/// <param name="action">The cache action.</param>
		public override void NotifyChange(string key, string region, CacheItemChangedEventAction action)
		{
			PublishMessage(BackplaneMessage.ForChanged(_identifier, key, region, action));
		}

		/// <summary>
		///     Notifies other cache clients about a cache clear.
		/// </summary>
		public override void NotifyClear()
		{
			PublishMessage(BackplaneMessage.ForClear(_identifier));
		}

		/// <summary>
		///     Notifies other cache clients about a cache clear region call.
		/// </summary>
		/// <param name="region">The region.</param>
		public override void NotifyClearRegion(string region)
		{
			PublishMessage(BackplaneMessage.ForClearRegion(_identifier, region));
		}

		/// <summary>
		///     Notifies other cache clients about a removed cache key.
		/// </summary>
		/// <param name="key">The key.</param>
		public override void NotifyRemove(string key)
		{
			PublishMessage(BackplaneMessage.ForRemoved(_identifier, key));
		}

		/// <summary>
		///     Notifies other cache clients about a removed cache key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="region">The region.</param>
		public override void NotifyRemove(string key, string region)
		{
			PublishMessage(BackplaneMessage.ForRemoved(_identifier, key, region));
		}

		private void PublishMessage(BackplaneMessage message)
		{
			byte[] serializedMessage = BackplaneMessage.Serialize(message);
			_publishSubscribe.Publish(_channelName, serializedMessage);
		}
	}
}