using System;
using System.Collections.Generic;

using Grpc.Core;

using vivego.core;

namespace vivego.PublishSubscribe
{
	public class ChannelLeases
	{
		private readonly Dictionary<string, ChannelLease> _channels = new Dictionary<string, ChannelLease>();

		public IDisposable Lease(string target, out Channel channel)
		{
			lock (_channels)
			{
				if (!_channels.TryGetValue(target, out ChannelLease channelLease))
				{
					channel = new Channel(target, ChannelCredentials.Insecure, new[]
					{
						new ChannelOption("grpc.min_reconnect_backoff_ms", 1000),
						new ChannelOption("grpc.max_reconnect_backoff_ms", 5000),
						new ChannelOption("grpc.initial_reconnect_backoff_ms", 3000),
						new ChannelOption("grpc.default_stream_compression_algorithm", 1),
						new ChannelOption("grpc.default_stream_compression_level", 1),
						new ChannelOption(ChannelOptions.MaxSendMessageLength, int.MaxValue),
						new ChannelOption(ChannelOptions.MaxReceiveMessageLength, int.MaxValue),
						new ChannelOption(ChannelOptions.MaxConcurrentStreams, 100)
					});

					_channels.Add(target, new ChannelLease
					{
						Channel = channel,
						ReferenceCount = 0
					});
				}

				channelLease.ReferenceCount++;
				channel = channelLease.Channel;

				return new AnonymousDisposable(() =>
				{
					lock (_channels)
					{
						channelLease.ReferenceCount--;
						if (channelLease.ReferenceCount == 0)
						{
							_channels.Remove(target);
							channelLease.Channel.ShutdownAsync().Wait();
						}
					}
				});
			}
		}

		private class ChannelLease
		{
			public int ReferenceCount { get; set; }
			public Channel Channel { get; set; }
		}
	}
}