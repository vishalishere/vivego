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

using Grpc.Core;

using Microsoft.Extensions.Logging;

using Proto;
using Proto.Cluster;
using Proto.Cluster.Consul;
using Proto.Remote;
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
		public static IEnumerable<IPAddress> GetMyIpAddress()
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
			Log.SetLoggerFactory(loggerFactory);

			//loggerFactory
			//	.CreateLogger("Auto")
			//	.LogDebug("Seed endpoints: {0}", string.Join(";", seedsEndpoints.Select(endPoint => endPoint.ToString())));

			ISerializer<byte[]> serializer = new MessagePackSerializer();
			IPublishSubscribe pubSub = new PublishSubscribe(clusterId, serializer, loggerFactory);

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

	public class DirectoryMicroService<T, TKey> : DisposableBase
	{
		public DirectoryMicroService(IPublishSubscribe publishSubscribe,
			Func<T, TKey> group)
		{
			publishSubscribe
				.Observe<T>("AgentPresence", hashBy: true)
				.GroupBy(_ => group(_.Data))
				.Subscribe(groupedObservable =>
				{
					TKey groupId = groupedObservable.Key;
					// Read initial state for group
					groupedObservable.Subscribe(tuple =>
					{
						T t = tuple.Data;
					}, CancellationToken);
				}, CancellationToken);
		}
	}

	public abstract class SingletonMicroService<T> : DisposableBase
	{
		private readonly string _topic;
		private readonly IPublishSubscribe _publishSubscribe;

		protected SingletonMicroService(string topic, IPublishSubscribe publishSubscribe)
		{
			_topic = topic;
			_publishSubscribe = publishSubscribe;
			publishSubscribe
				.Observe<SingletonHashMessage<T>>(topic, hashBy: true)
				.Subscribe(tuple => Process(tuple.Data.Value), CancellationToken);
		}

		protected abstract void Process(T t);

		public void Update(T t)
		{
			_publishSubscribe.Publish(_topic, new SingletonHashMessage<T>
			{
				Value = t
			});
		}
	}

	public class SingletonHashMessage<T> : IHashable
	{
		public T Value { get; set; }

		public string HashBy()
		{
			return "0";
		}
	}

	[DataContract(Name = "session")]
	public class Session
	{
		[DataMember(Name = "id")]
		public Guid Id { get; set; }
	}

	public class SessionActor : IActor
	{
		public async Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Guid g:
					Console.Out.WriteLine("Got guid");
					//(PID, ResponseStatusCode) tuple = await Cluster.GetAsync("SessionList", "SessionList");
					//if (tuple.Item2 == ResponseStatusCode.OK)
					//{
					//	tuple.Item1.Tell(new Session
					//	{
					//		Id = g
					//	});
					//}

					break;
			}
		}
	}

	public class SessionsActor : IActor
	{
		private readonly IPublishSubscribe _publishSubscribe;
		List<Session> _sessions = new List<Session>();

		public SessionsActor(IPublishSubscribe publishSubscribe)
		{
			_publishSubscribe = publishSubscribe;
		}

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Session session:
					Console.Out.WriteLine("Got session");
					_sessions.Add(session);
					_publishSubscribe.Publish("Sessions", _sessions);
					break;
			}

			return Task.CompletedTask;
		}
	}

	public class EntryPoint
	{
		public static async Task Main(string[] args)
		{
			long counter = 0;
			ISerializer<byte[]> serializer = new MessagePackSerializer();
			ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Debug);

			IPAddress ipAddress = PubSubAutoConfig.GetMyIpAddress().First();
			int serverPort = PortUtils.FindAvailablePortIncrementally(35100);
			IPEndPoint[] seedsEndpoints = { new IPEndPoint(ipAddress, 35100), new IPEndPoint(ipAddress, serverPort), new IPEndPoint(ipAddress, serverPort + 1) };

			//IPublishSubscribe publishSubscribe1 = PubSubAutoConfig.Auto("unique1");
			var publishSubscribe1 = new vivego.PublishSubscribe.PublishSubscribe(serverPort, loggerFactory, serializer);
			using (publishSubscribe1
				.Observe<object>("*")
				.Subscribe(_ =>
				{
					long c = Interlocked.Increment(ref counter);
					//Console.Out.WriteLine(_);
					if (c % 1000 == 0)
					{
						Console.Out.WriteLine(c);
					}
				}))
			{
				while (true)
				{
					Console.Out.WriteLine("ready");
					Console.ReadLine();
					foreach (int i in Enumerable.Range(0, 100000000))
					{
						publishSubscribe1.Publish("Hello", "World");
					}
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