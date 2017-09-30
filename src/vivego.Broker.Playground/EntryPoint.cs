using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading;

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
		public static IPublishSubscribe Auto(string clusterId)
		{
			//int serverPort = PortUtils.FindAvailablePortIncrementally(35000);
			//string address = Dns.GetHostAddresses(Dns.GetHostName())
			//	.First(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork && !Equals(ipAddress, IPAddress.Loopback)).ToString();

			int serverPort = PortUtils.FindAvailablePort();
			IPEndPoint protoActorServerEndpoint = new IPEndPoint(IPAddress.Loopback, serverPort);
			new DiscoverMulticastServer(protoActorServerEndpoint, clusterId).Run();
			MulticastDiscoverClientFactory clientFactory = new MulticastDiscoverClientFactory(clusterId, replyWaitTimeout: TimeSpan.FromSeconds(2));
			IPEndPoint[] endPoints = clientFactory.DiscoverEndPoints();

			ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Debug);
			ISerializer<byte[]> serializer = new MessagePackSerializer();
			IPublishSubscribe pubSub = PublishSubscribe.StartCluster(clusterId,
				"127.0.0.1",
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
				long counter=0;
				Stopwatch sw = Stopwatch.StartNew();
				var elapsed = sw.Elapsed;
				Observable
					.Interval(TimeSpan.FromSeconds(1))
					.Subscribe(_ =>
					{
						var c = Interlocked.Exchange(ref counter, 0);
						elapsed = sw.Elapsed;
						if (c > 0)
						{
							//Console.Out.WriteLine(c / elapsed.TotalSeconds);
						}
					});

				using (pubSub
					.Observe<object>("*", "group")
					.Subscribe(_ =>
					{
						Console.Out.WriteLine(_);
						Interlocked.Increment(ref counter);
					}))
				{
					while (true)
					{
						Console.ReadLine();
						sw.Restart();
						foreach (int i in Enumerable.Range(0, 1))
						{
							pubSub.Publish("a", "Hello", "group");
						}

						Console.Out.WriteLine(sw.Elapsed);
					}
				}
			}
		}
	}
}