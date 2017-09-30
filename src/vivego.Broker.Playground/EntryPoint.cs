﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

using Microsoft.Extensions.Logging;

using vivego.core;
using vivego.Discovery;
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
			int serverPort = PortUtils.FindAvailablePort();
			IPEndPoint[] endPoints = QueryEndPointsMulticast(clusterId, ipAddress, serverPort);
			ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Debug);
			loggerFactory
				.CreateLogger("Auto")
				.LogDebug("Discovered endpoints: {0}", string.Join(";", endPoints.Select(endPoint => endPoint.ToString())));
			ISerializer<byte[]> serializer = new MessagePackSerializer();
			IPublishSubscribe pubSub = PublishSubscribe.StartCluster(clusterId,
				ipAddress.ToString(),
				serverPort,
				new StaticClusterProvider(TimeSpan.FromSeconds(1), endPoints),
				serializer,
				loggerFactory
			);

			return pubSub;
		}
	}

	public class EntryPoint
	{
		public static void Main(string[] args)
		{
			IPublishSubscribe pubSub = PubSubAutoConfig.Auto("I am unique");
			using (pubSub)
			{
				using (pubSub
					.Observe<object>("*")
					.Subscribe(_ => { Console.Out.WriteLine(_); }))
				{
					while (true)
					{
						Console.ReadLine();
						pubSub.Publish("a", "Hello");
					}
				}
			}
		}
	}
}