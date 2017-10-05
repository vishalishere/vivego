using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;

using Microsoft.Extensions.Logging;

using Proto;
using Proto.Cluster;
using Proto.Cluster.Consul;

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

		public static IPublishSubscribe Auto(string clusterId)
		{
			IPAddress ipAddress = GetMyIpAddress().First();
			int serverPort = PortUtils.FindAvailablePortIncrementally(35100);
			IPEndPoint[] seedsEndpoints = {
				new IPEndPoint(ipAddress, 35100),
				//new IPEndPoint(ipAddress, 35101),
				//new IPEndPoint(ipAddress, 35102),
				//new IPEndPoint(ipAddress, 35103)
			};

			ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Warning);
			//loggerFactory
			//	.CreateLogger("Auto")
			//	.LogDebug("Seed endpoints: {0}", string.Join(";", seedsEndpoints.Select(endPoint => endPoint.ToString())));

			ISerializer<byte[]> serializer = new MessagePackSerializer();
			IPublishSubscribe pubSub = new PublishSubscribe(serializer, loggerFactory);

			Cluster.Start(clusterId, ipAddress.ToString(), serverPort, new SeededLocalClusterProvider(Observable.Return(seedsEndpoints)));
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
		public static void Main(string[] args)
		{
			Actor.EventStream.Subscribe<ClusterTopologyEvent>(clusterTopologyEvent =>
			{
				Console.Out.WriteLine("Topology: " + string.Join(";", clusterTopologyEvent.Statuses.Select(memberStatus => $"{memberStatus.Address}-{memberStatus.Alive}")));
				Console.Out.WriteLine("");
			});

			IPublishSubscribe pubSub = PubSubAutoConfig.Auto("unique");
			using (pubSub
				.Observe<object>("*")
				.Subscribe(_ => { Console.Out.WriteLine(_); }))
			{
				while (true)
				{
					Console.ReadLine();
					pubSub.Publish("00000000-0000-0000-0000-000000000000_AgentPresence_DictionaryGrainDictionary", "Hello");
				}
			}
		}
	}
}