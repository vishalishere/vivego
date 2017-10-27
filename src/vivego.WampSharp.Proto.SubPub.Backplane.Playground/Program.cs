using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;

using Microsoft.Extensions.Logging;

using Proto.Cluster;

using vivego.core;
using vivego.Discovery.Abstactions;
using vivego.Discovery.DotNetty;
using vivego.Proto.ClusterProvider;
using vivego.Proto.PubSub;
using vivego.Serializer.Abstractions;
using vivego.Serializer.MessagePack;

using WampSharp.V2;
using WampSharp.V2.Core;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.Realm;

namespace vivego.WampSharp.Proto.SubPub.Backplane.Playground
{
	internal class Program
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

		public static IDisposable EnableDistributedBackplane(IWampHostedRealm realm)
		{
			IPAddress ipAddress = GetMyIpAddress().First();
			int serverPort = PortUtils.FindAvailablePort();
			IPEndPoint[] seedsEndpoints = QueryEndPointsMulticast(realm.Name, ipAddress, serverPort);
			ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Debug);
			//loggerFactory
			//	.CreateLogger("Auto")
			//	.LogDebug("Discovered endpoints: {0}", string.Join(";", seedsEndpoints.Select(endPoint => endPoint.ToString())));
			ISerializer<byte[]> serializer = new MessagePackSerializer();
			Cluster.Start(realm.Name, ipAddress.ToString(), serverPort, new SeededLocalClusterProvider(Observable.Return(seedsEndpoints)));
			IPublishSubscribe pubSub = new vivego.Proto.PubSub.PublishSubscribe(realm.Name, serializer, loggerFactory);
			return realm.EnableDistributedBackplane(pubSub);
		}

		private static void Main(string[] args)
		{
			int serverPort = PortUtils.FindAvailablePortIncrementally(18889);
			string serverAddress = "ws://127.0.0.1:" + serverPort + "/ws";
			WampHost host = new DefaultWampHost(serverAddress);
			IWampHostedRealm realm = host.RealmContainer.GetRealmByName("vivego");
			EnableDistributedBackplane(realm);
			host.Open();

			DefaultWampChannelFactory channelFactory = new DefaultWampChannelFactory();
			IWampChannel wampChannel = channelFactory.CreateJsonChannel(serverAddress, "vivego");
			wampChannel.Open().Wait();

			using (wampChannel.RealmProxy.Services
				.GetSubject<object>("vivego")
				.Subscribe(s => { Console.Out.WriteLine(s); }))
			{
				while (true)
				{
					string input = Console.ReadLine();

					realm.TopicContainer.Publish(WampObjectFormatter.Value,
						new PublishOptions(),
						"vivego",
						new object[] { "Helo" });
				}
			}
		}
	}
}