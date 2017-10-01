using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Microsoft.Extensions.Logging;

using Proto.Cluster;

using vivego.core;
using vivego.Discovery.Abstactions;
using vivego.Discovery.DotNetty;
using vivego.Proto.ClusterProvider;
using vivego.Proto.PubSub;
using vivego.Serializer.Abstractions;
using vivego.Serializer.MessagePack;

using WampSharp.V2.Realm;

namespace vivego.WampSharp.Proto.SubPub.Backplane
{
	public static class RealmExtensions
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

		public static IDisposable EnableDistributedBackplane(this IWampHostedRealm realm)
		{
			IPAddress ipAddress = GetMyIpAddress().First();
			int serverPort = PortUtils.FindAvailablePort();
			IPEndPoint[] seedsEndpoints = QueryEndPointsMulticast(realm.Name, ipAddress, serverPort);
			ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Debug);
			loggerFactory
				.CreateLogger("Auto")
				.LogDebug("Discovered endpoints: {0}", string.Join(";", seedsEndpoints.Select(endPoint => endPoint.ToString())));
			ISerializer<byte[]> serializer = new MessagePackSerializer();
			Cluster.Start(realm.Name, ipAddress.ToString(), serverPort, new SeededLocalClusterProvider(seedsEndpoints));
			IPublishSubscribe pubSub = new PublishSubscribe(serializer, loggerFactory);

			return EnableDistributedBackplane(realm, pubSub);
		}

		public static IDisposable EnableDistributedBackplane(this IWampHostedRealm realm,
			IPublishSubscribe publishSubscribe)
		{
			ConcurrentBag<IDisposable> disposables = new ConcurrentBag<IDisposable>();
			realm.TopicContainer.TopicCreated += (sender, args) =>
			{
				ISubject<object> topicSubject = realm.Services.GetSubject<object>(args.Topic.TopicUri);
				IDisposable wampSubscription = topicSubject.Subscribe(o => publishSubscribe.Publish(args.Topic.TopicUri, o));
				IDisposable pubSubSubscription = publishSubscribe
					.Observe<object>(args.Topic.TopicUri)
					.Select(tuple => tuple.Data)
					.Subscribe(topicSubject);
				disposables.Add(wampSubscription);
				disposables.Add(pubSubSubscription);
			};

			return new AnonymousDisposable(() =>
			{
				while (disposables.TryTake(out IDisposable disposable))
				{
					disposable.Dispose();
				}
			});
		}
	}
}