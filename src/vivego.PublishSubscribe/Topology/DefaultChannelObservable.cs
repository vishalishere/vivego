using System;
using System.Linq;
using System.Reactive.Linq;

using Grpc.Core;

namespace vivego.PublishSubscribe.Topology
{
	public class DefaultChannelObservable : IObservable<Channel[]>
	{
		private readonly IObservable<Channel[]> _channelObservable;

		public DefaultChannelObservable(IObservable<Uri[]> observable)
		{
			_channelObservable = observable
				.Select(uris =>
				{
					return uris.Select(uri =>
						{
							Channel channel = new Channel($"{uri.Host}:{uri.Port}", ChannelCredentials.Insecure, new[]
							{
								//new ChannelOption("grpc.min_reconnect_backoff_ms", 1000),
								//new ChannelOption("grpc.max_reconnect_backoff_ms", 5000),
								//new ChannelOption("grpc.initial_reconnect_backoff_ms", 3000),
								//new ChannelOption("grpc.default_stream_compression_algorithm", 1),
								//new ChannelOption("grpc.default_stream_compression_level", 1),
								//new ChannelOption(ChannelOptions.MaxSendMessageLength, int.MaxValue),
								//new ChannelOption(ChannelOptions.MaxReceiveMessageLength, int.MaxValue),
								new ChannelOption(ChannelOptions.MaxConcurrentStreams, 100)
							});
							channel.ConnectAsync();//.Wait(C);
							return channel;
						})
						.ToArray();
				});
		}

		public IDisposable Subscribe(IObserver<Channel[]> observer)
		{
			return _channelObservable.Subscribe(observer);
		}
	}
}