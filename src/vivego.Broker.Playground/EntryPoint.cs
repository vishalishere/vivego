using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading;

using Microsoft.Extensions.Logging;

using Proto;

using vivego.core;
using vivego.Discovery.Abstactions;
using vivego.Discovery.DotNetty;
using vivego.Proto;
using vivego.PublishSubscribe;
using vivego.PublishSubscribe.Grpc;
using vivego.Serializer.Abstractions;
using vivego.Serializer.MessagePack;
using vivego.Serializer.Wire;

namespace ProtoBroker.Playground
{
	public class PubSubAutoConfig
	{
		private static IPEndPoint[] QueryEndPointsUdp(string clusterId, IPAddress ipAddress, int serverPort)
		{
			IPEndPoint protoActorServerEndpoint = new IPEndPoint(ipAddress, serverPort);
			new UdpBroadcastServer(protoActorServerEndpoint, clusterId).Run();
			UdpDiscoverClientFactory clientFactory =
				new UdpDiscoverClientFactory(clusterId, replyWaitTimeout: TimeSpan.FromSeconds(2));
			IPEndPoint[] endPoints = clientFactory.DiscoverEndPoints();
			return endPoints;
		}

		private static IPEndPoint[] QueryEndPointsMulticast(string clusterId, IPAddress ipAddress, int serverPort)
		{
			IPEndPoint protoActorServerEndpoint = new IPEndPoint(ipAddress, serverPort);
			new DiscoverMulticastServer(protoActorServerEndpoint, clusterId).Run();
			MulticastDiscoverClientFactory clientFactory =
				new MulticastDiscoverClientFactory(clusterId, replyWaitTimeout: TimeSpan.FromSeconds(2));
			IPEndPoint[] endPoints = clientFactory.DiscoverEndPoints();
			return endPoints;
		}

		public static IPublishSubscribe Auto(string clusterId)
		{
			ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Debug);
			Log.SetLoggerFactory(loggerFactory);
			ISerializer<byte[]> serializer = new MessagePackSerializer();
			IPublishSubscribe pubSub = new PublishSubscribeBuilder(new WireSerializer())
				.SetClusterName(clusterId)
				.SetSerializer(serializer)
				.SetLoggerFactory(loggerFactory)
				.GrpcPublishSubscribe(new IPEndPoint(IPAddress.Loopback, 35000), Observable.Return(new[] {new DnsEndPoint("localhost", 35000)}))
				.Build();
			ClusterBuilder.RunSeededLocalCluster(clusterId);

			return pubSub;
		}
	}

	public class EntryPoint
	{
		public static void Main(string[] args)
		{
			long counter = 0;
			IPublishSubscribe publishSubscribe1 = PubSubAutoConfig.Auto("unique1");
			using (publishSubscribe1
				.Observe<object>("*")
				.Subscribe(_ =>
				{
					long c = Interlocked.Increment(ref counter);
					Console.Out.WriteLine(c);
				}))
			{
				while (true)
				{
					Console.Out.WriteLine("ready");
					Console.ReadLine();
					foreach (int i in Enumerable.Range(0, 1))
					{
						publishSubscribe1.Publish("Hello", "World");
					}
				}
			}
		}
	}
}