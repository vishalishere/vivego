using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

using vivego.Discovery.Abstactions;

namespace vivego.Discovery.DotNetty
{
	public class UdpBroadcastServer : SimpleChannelInboundHandler<DatagramPacket>, IDiscoverServer
	{
		public const int DefaultServerPort = 38999;

		private readonly AddressFamily _addressFamily;
		private readonly string _group;
		private readonly int _discoveryServerPort;
		private readonly IPEndPoint _endPointToPublish;

		public UdpBroadcastServer(
			IPEndPoint endPointToPublish,
			string group = "Default",
			int discoveryServerPort = DefaultServerPort,
			AddressFamily addressFamily = AddressFamily.InterNetwork) : base(true)
		{
			if (string.IsNullOrEmpty(group))
			{
				throw new ArgumentException("message", nameof(group));
			}

			_group = group;
			_discoveryServerPort = discoveryServerPort;
			_endPointToPublish = endPointToPublish ?? throw new ArgumentNullException(nameof(endPointToPublish));
			_addressFamily = addressFamily;

			switch (addressFamily)
			{
				case AddressFamily.InterNetwork:
				case AddressFamily.InterNetworkV6:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(addressFamily), addressFamily, null);
			}
		}

		public IDisposable Run()
		{
			ManualResetEvent resetEvent = new ManualResetEvent(false);
			var subscription = Observable
				.Create<object>(async (observer, cancellationToken) =>
				{
					try
					{
						Bootstrap serverBootstrap = new Bootstrap();
						MultithreadEventLoopGroup serverGroup = new MultithreadEventLoopGroup(1);
						try
						{
							serverBootstrap
								.Group(serverGroup)
								.ChannelFactory(() => new SocketDatagramChannel(_addressFamily))
								.Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
								.Option(ChannelOption.SoReuseaddr, true)
								.Option(ChannelOption.IpMulticastLoopDisabled, false)
								.Handler(new ActionChannelInitializer<IChannel>(channel => { channel.Pipeline.AddLast(this); }));

							SocketDatagramChannel serverChannel = (SocketDatagramChannel)await serverBootstrap
								.BindAsync(IPAddress.Any, _discoveryServerPort)
								.ConfigureAwait(false);

							try
							{
								TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
								using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), false))
								{
									resetEvent.Set();
									await tcs.Task.ConfigureAwait(false);
								}
							}
							finally
							{
								await serverChannel
									.CloseAsync()
									.ConfigureAwait(false);
							}
						}
						finally
						{
							await serverGroup
								.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))
								.ConfigureAwait(false);
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
				.Subscribe();
			resetEvent.WaitOne();
			return subscription;
		}

		public static IPAddress DefaultMulticastAddress(AddressFamily addressFamily = AddressFamily.InterNetwork)
		{
			switch (addressFamily)
			{
				case AddressFamily.InterNetwork:
					return IPAddress.Parse("230.0.0.1");
				case AddressFamily.InterNetworkV6:
					return IPAddress.Parse("ff12::1");
				default:
					throw new ArgumentOutOfRangeException(nameof(addressFamily), addressFamily, null);
			}
		}

		protected override void ChannelRead0(IChannelHandlerContext ctx, DatagramPacket datagramPacket)
		{
			int length = datagramPacket.Content.ReadInt();
			byte[] byteBuffer = new byte[length];
			datagramPacket.Content.ReadBytes(byteBuffer);
			string group = Encoding.UTF8.GetString(byteBuffer);
			if (_group.Equals(group, StringComparison.Ordinal))
			{
				IByteBuffer message = PooledByteBufferAllocator.Default
					.Buffer()
					.WriteBytes(_endPointToPublish.Address.GetAddressBytes())
					.WriteInt(_endPointToPublish.Port);
				DatagramPacket dataGram = new DatagramPacket(message, datagramPacket.Sender);
				ctx.WriteAndFlushAsync(dataGram);
			}
		}
	}
}