using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Proto.Cluster;

using vivego.core;
using vivego.Discovery.Abstactions;
using vivego.Discovery.DotNetty;
using vivego.Proto.ClusterProvider;
using vivego.Proto.PubSub;
using vivego.Serializer.Abstractions;
using vivego.Serializer.MessagePack;

namespace ProtoBroker.Playground
{
	public class PubSubAutoConfig
	{
		private static IEnumerable<IPAddress> GetMyIpAddress()
		{
			foreach (NetworkInterface allNetworkInterface in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (allNetworkInterface.OperationalStatus != OperationalStatus.Up)
				{
					continue;
				}

				IPInterfaceProperties ipProperties = allNetworkInterface.GetIPProperties();
				if (ipProperties.GatewayAddresses.Count == 0)
				{
					continue;
				}

				foreach (UnicastIPAddressInformation ip in allNetworkInterface.GetIPProperties().UnicastAddresses)
				{
					if (ip.Address.Equals(IPAddress.Loopback))
					{
						continue;
					}

					if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
					{
						yield return ip.Address;
					}
				}
			}
		}

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

		private static bool clusterIsStarted = false;
		public static IPublishSubscribe Auto(string clusterId)
		{
			IPAddress ipAddress = GetMyIpAddress().First();
			int serverPort = PortUtils.FindAvailablePortIncrementally(35100);
			IPEndPoint[] seedsEndpoints = {
				new IPEndPoint(ipAddress, 35100),
				//new IPEndPoint(ipAddress, 35101),
				//new IPEndPoint(ipAddress, 35102),
				//new IPEndPoint(ipAddress, 35103)B
			};

			ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Debug);
			//loggerFactory
			//	.CreateLogger("Auto")
			//	.LogDebug("Seed endpoints: {0}", string.Join(";", seedsEndpoints.Select(endPoint => endPoint.ToString())));

			ISerializer<byte[]> serializer = new MessagePackSerializer();
			IPublishSubscribe pubSub = new PublishSubscribe(clusterId, serializer, loggerFactory);

			if (!clusterIsStarted)
			{
				clusterIsStarted = true;
				Cluster.Start(clusterId, ipAddress.ToString(), serverPort, new SeededLocalClusterProvider(Observable.Return(seedsEndpoints)));
			}
			//Cluster.Start(clusterId, ipAddress.ToString(), serverPort, new ConsulProvider(new ConsulProviderOptions
			//{
			//	ServiceTtl = TimeSpan.FromSeconds(5),
			//	DeregisterCritical = TimeSpan.FromSeconds(5),
			//	RefreshTtl = TimeSpan.FromSeconds(1),
			//	BlockingWaitTime = TimeSpan.FromSeconds(20)
			//}, c => c.Address = new Uri("http://vngagetest44:8500")));

			return pubSub;
		}
	}

	public class EntryPoint
	{
		public static async Task Main(string[] args)
		{
			long counter = 0;
			IPublishSubscribe publishSubscribe1 = PubSubAutoConfig.Auto("unique1");
			Stopwatch sw = Stopwatch.StartNew();
			using (publishSubscribe1
				.Observe<string>("*")
				.Subscribe(_ =>
				{
					Console.Out.WriteLine(_);
				}))
			using (publishSubscribe1
				.Observe<string>("*", "a")
				.Subscribe(_ =>
				{
					Console.Out.WriteLine("group: " + _);
				}))
			{
				while (true)
				{
					Console.ReadLine();
					publishSubscribe1.Publish("a", "Hello2");
				}
			}
		}
	}
}