using System;

using Grpc.Core;

using vivego.core;

using Vivego.PublishSubscribe.Grpc;

namespace vivego.PublishSubscribe
{
	public class PubSubServer : DisposableBase
	{
		public const string HeartbeatTopic = "heartbeat";

		private readonly Server _server;

		public PubSubServer(int port)
		{
			_server = new Server(new[]
			{
				new ChannelOption("grpc.default_stream_compression_algorithm", 1),
				new ChannelOption("grpc.default_stream_compression_level", 1),
				new ChannelOption(ChannelOptions.MaxSendMessageLength, int.MaxValue),
				new ChannelOption(ChannelOptions.MaxReceiveMessageLength, int.MaxValue),
				new ChannelOption(ChannelOptions.MaxConcurrentStreams, 100)
			})
			{
				Services =
				{
					PubSub.BindService(new PubSubImpl(CancellationToken))
				},
				Ports = { new ServerPort("0.0.0.0", port, ServerCredentials.Insecure) }
			};
			_server.Start();
		}

		protected override void Cleanup()
		{
			_server.ShutdownAsync().Wait(TimeSpan.FromSeconds(1));
		}
	}
}