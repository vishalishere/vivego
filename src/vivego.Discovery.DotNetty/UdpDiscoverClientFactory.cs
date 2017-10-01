using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using DotNetty.Buffers;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

using vivego.Discovery.Abstactions;

namespace vivego.Discovery.DotNetty
{
	public class UdpDiscoverClientFactory : IDiscoverClientFactory
	{
		private readonly AddressFamily _addressFamily;
		private readonly ConcurrentStack<IPEndPoint> _endPoints = new ConcurrentStack<IPEndPoint>();
		private readonly string _group;
		private readonly TimeSpan _pollingInterval = TimeSpan.FromMilliseconds(500);
		private readonly TimeSpan _replyWaitTimeout = TimeSpan.FromMilliseconds(500);
		private readonly int _disconveryServerPort;

		public UdpDiscoverClientFactory(
			string group = "Default",
			int disconveryServerPort = UdpBroadcastServer.DefaultServerPort,
			TimeSpan? replyWaitTimeout = null,
			TimeSpan? pollingInterval = null,
			AddressFamily addressFamily = AddressFamily.InterNetwork)
		{
			if (string.IsNullOrEmpty(group))
			{
				throw new ArgumentException("message", nameof(group));
			}

			_group = group;
			_disconveryServerPort = disconveryServerPort;
			_addressFamily = addressFamily;

			if (replyWaitTimeout.HasValue)
			{
				_replyWaitTimeout = replyWaitTimeout.Value;
			}

			if (pollingInterval.HasValue)
			{
				_pollingInterval = pollingInterval.Value;
			}
		}

		public IObservable<IPEndPoint[]> MakeObservable()
		{
			return Observable.Create<IPEndPoint[]>(async (observer, cancellationToken) =>
			{
				try
				{
					Bootstrap clientBootstrap = new Bootstrap();
					MultithreadEventLoopGroup clientGroup = new MultithreadEventLoopGroup(1);
					try
					{
						clientBootstrap
							.Group(clientGroup)
							.ChannelFactory(() => new SocketDatagramChannel(_addressFamily))
							.Option(ChannelOption.Allocator, PooledByteBufferAllocator.Default)
							.Option(ChannelOption.SoBroadcast, true)
							.Option(ChannelOption.IpMulticastLoopDisabled, false)
							.Handler(new ActionChannelInitializer<IChannel>(channel =>
							{
								channel.Pipeline.AddLast(new SimpleChannelInboundHandler(_endPoints));
							}));

						SocketDatagramChannel clientChannel = (SocketDatagramChannel)await clientBootstrap
							.BindAsync(IPAddress.Any, IPEndPoint.MinPort)
							.ConfigureAwait(false);
						try
						{
							byte[] groupBytes = Encoding.UTF8.GetBytes(_group);
							IByteBuffer message = PooledByteBufferAllocator.Default
								.Buffer()
								.WriteInt(groupBytes.Length)
								.WriteBytes(groupBytes);
							DatagramPacket dataGram = new DatagramPacket(message, new IPEndPoint(IPAddress.Broadcast, _disconveryServerPort));
							while (!cancellationToken.IsCancellationRequested)
							{
								_endPoints.Clear();
								await clientChannel
									.WriteAndFlushAsync(dataGram.Copy())
									.ConfigureAwait(false);
								await Task
									.Delay(_replyWaitTimeout, cancellationToken)
									.ConfigureAwait(false);
								observer.OnNext(_endPoints.ToArray());
								await Task
									.Delay(_pollingInterval, cancellationToken)
									.ConfigureAwait(false);
							}
						}
						finally
						{
							await clientChannel
								.CloseAsync()
								.ConfigureAwait(false);
						}
					}
					finally
					{
						await clientGroup
							.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1))
							.ConfigureAwait(false);
					}

					observer.OnCompleted();
				}
				catch (OperationCanceledException)
				{
					observer.OnCompleted();
				}
				catch (Exception exception)
				{
					observer.OnError(exception);
				}
			})
				.DistinctUntilChanged(endPoints => string.Join(";", endPoints.Select(endPoint => endPoint.ToString())));
		}

		private class SimpleChannelInboundHandler : SimpleChannelInboundHandler<DatagramPacket>
		{
			private readonly ConcurrentStack<IPEndPoint> _endPoints;

			public SimpleChannelInboundHandler(ConcurrentStack<IPEndPoint> endPoints) : base(true)
			{
				_endPoints = endPoints;
			}

			protected override void ChannelRead0(IChannelHandlerContext ctx, DatagramPacket msg)
			{
				byte[] ipaddresBytes = msg.Content.ReadBytes(4).ToArray();
				int port = msg.Content.ReadInt();

				_endPoints.Push(new IPEndPoint(new IPAddress(ipaddresBytes), port));
			}
		}
	}
}