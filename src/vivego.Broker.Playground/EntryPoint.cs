using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Proto;
using Proto.Cluster;
using Proto.Cluster.Consul;
using Proto.Remote;

using vivego.core;
using vivego.Discovery.Abstactions;
using vivego.Discovery.DotNetty;
using vivego.Proto;
using vivego.PublishSubscribe;
using vivego.PublishSubscribe.Grpc;
using vivego.PublishSubscribe.ProtoActor;
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
				.ProtoActorPublishSubscribe()
				//.GrpcPublishSubscribe(seedsEndpointObservable: Observable.Return(new[]
				//{
				//	new DnsEndPoint("localhost", 35000),
				//	new DnsEndPoint("localhost", 35001),
				//	new DnsEndPoint("localhost", 35002),
				//	new DnsEndPoint("localhost", 35003)
				//}))
				.Build();
			//ClusterBuilder.RunSeededLocalCluster(clusterId);
			Cluster.Start("Abe", "localhost", PortUtils.FindAvailablePortIncrementally(36002), new ConsulProvider(new ConsulProviderOptions()));

			return pubSub;
		}
	}

	public class Session : IActor
	{
		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case string s:
					context.Sender.Tell(System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
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
			//IPublishSubscribe publishSubscribe1 = PubSubAutoConfig.Auto("unique1");

			Remote.RegisterKnownKind("Session", Actor.FromProducer(() => new Session()));
			Cluster.Start("CN", "127.0.0.1", 0, new ConsulProvider(new ConsulProviderOptions()));

			while(true)
			foreach (int i in Enumerable.Range(0, 10))
			{
				var session = await Cluster.GetAsync("Session" + i, "Session");
				while (session.Item2 != ResponseStatusCode.OK)
				{
					Console.Out.WriteLine(session.Item2);
					session = await Cluster.GetAsync("Session" + i, "Session");
					await Task.Delay(1000);
				}

				try
				{
					var response = await session.Item1.RequestAsync<string>("Hello " + i);
					Console.Out.WriteLine(i + ":" + response);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}

				await Task.Delay(1000);
			}

			//using (publishSubscribe1
			//	.Observe<object>("*")
			//	.Subscribe(_ =>
			//	{
			//		long c = Interlocked.Increment(ref counter);
			//		Console.Out.WriteLine(c);
			//	}))
			//{
			//	foreach (int i in Enumerable.Range(0, 10))
			//	{
			//		var session = Cluster.GetAsync("Session" + i, "Session").Result;
			//		while (session.Item2 != ResponseStatusCode.OK)
			//		{
			//			Console.Out.WriteLine(session.Item2);
			//			session = await Cluster.GetAsync("Session" + i, "Session");
			//			await Task.Delay(1);
			//		}

			//		session.Item1.Tell("Hello " + i);
			//	}

			//	while (true)
			//	{
			//		Console.Out.WriteLine("ready");
			//		Console.ReadLine();
			//		foreach (int i in Enumerable.Range(0, 1))
			//		{
			//			publishSubscribe1.Publish("Hello", "World");
			//		}
			//	}
			//}
			Console.ReadLine();
			Cluster.Shutdown();
		}
	}
}