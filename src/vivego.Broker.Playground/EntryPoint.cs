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

				if (!allNetworkInterface.SupportsMulticast)
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
					if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
					{
						yield return ip.Address;
					}
				}
			}
		}

		public static IPublishSubscribe Auto(string clusterId)
		{
			IPAddress ipAddress = GetMyIpAddress().First();
			int serverPort = PortUtils.FindAvailablePort();
			IPEndPoint protoActorServerEndpoint = new IPEndPoint(ipAddress, serverPort);
			new DiscoverMulticastServer(protoActorServerEndpoint, clusterId).Run();
			MulticastDiscoverClientFactory clientFactory =
				new MulticastDiscoverClientFactory(clusterId, replyWaitTimeout: TimeSpan.FromSeconds(2));
			IPEndPoint[] endPoints = clientFactory.DiscoverEndPoints();

			ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Debug);
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