using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Proto;
using Proto.Cluster;
using Proto.Router;

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

			ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Warning);
			Log.SetLoggerFactory(loggerFactory);

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

	public class CacheActor : IActor
	{
		private byte[] _value;
		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case byte[] b:
					_value = b;
					break;
				case string s when s.Equals("get"):
					context.Sender.Tell(_value);
					break;
			}

			return Task.CompletedTask;
		}
	}

	public class Cache
	{
		private static PID GetCacheActor(string key)
		{
			(PID pid, bool ok) tuple = ProcessRegistry.Instance.TryGet(key);
			if (tuple.ok)
			{
				return tuple.pid;
			}

			//Cluster.GetAsync("")

			Props props = Actor.FromProducer(() => new CacheActor());
			return Actor.SpawnNamed(props, key);
		}

		public void Set(string key, byte[] value)
		{
			PID cacheActor = GetCacheActor(key);
			cacheActor.Tell(value);
		}

		public Task<byte[]> Get(string key)
		{
			PID cacheActor = GetCacheActor(key);
			return cacheActor.RequestAsync<byte[]>("get");
		}
	}

	public class EntryPoint
	{
		public static void Main(string[] args)
		{
			long counter = 0;
			long hashByCounter = 0;
			IPublishSubscribe publishSubscribe1 = PubSubAutoConfig.Auto("unique1");
			using (publishSubscribe1
				.Observe<object>("*", hashBy: true)
				.Subscribe(_ =>
				{
					long c = Interlocked.Increment(ref hashByCounter);
					Console.Out.WriteLine("hash: " + c);
				}))
			using (publishSubscribe1
				.Observe<object>("*", hashBy: false)
				.Subscribe(_ =>
				{
					long c = Interlocked.Increment(ref counter);
					Console.Out.WriteLine(c);
				}))
			{
				Console.ReadLine();
				while (true)
				{
					publishSubscribe1.Publish("a", new Test());
					Thread.Sleep(1000);
				}
			}
		}

		[DataContract]
		public class Test : IHashable
		{
			[DataMember(Name = "test")]
			public string Testr { get; set; } = "A";

			public string HashBy()
			{
				return System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
			}
		}
	}
}