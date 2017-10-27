using System;
using System.Net;
using System.Reactive.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Orleans.Runtime.Configuration;

using Proto;
using Proto.Cluster;

using vivego.core;
using vivego.Orleans.Providers;
using vivego.Proto.ClusterProvider;
using vivego.Proto.PubSub;
using vivego.Serializer.Abstractions;
using vivego.Serializer.MessagePack;

namespace vivego.Orleans.Playground
{
	internal class Program
	{
		private static void Main()
		{
			OrleansStartup orleansStartup = new OrleansStartup("clusterName");
			ClusterConfiguration siloConfig = ClusterConfiguration
				.LocalhostPrimarySilo(PortUtils.FindAvailablePortIncrementally(22222),
					PortUtils.FindAvailablePortIncrementally(40000))
				.AddPublishSubscribeStreamProvider()
				.UseOrleansStartup(orleansStartup);

			ClientConfiguration clientConfiguration = ClientConfiguration
				.LocalhostSilo()
				.AddPublishSubscribeStreamProvider()
				.ConnectTo(siloConfig);

			// Start ProtoActor Cluster
			int serverPort = PortUtils.FindAvailablePortIncrementally(41000);
			Cluster.Start("unique", "127.0.0.1", serverPort, new SeededLocalClusterProvider(Observable.Return(new []{new IPEndPoint(IPAddress.Loopback, 41000) })));

			using (orleansStartup.PublishSubscribe.Observe<object>("*").Subscribe(_ =>
			{
				Console.Out.WriteLine(_);
			}))
			using (siloConfig.Run())
			using (var clusterClient = clientConfiguration.Run(orleansStartup))
			{
				clusterClient.GetGrain<ITestGrain>(Guid.NewGuid()).Run().Wait();
				Console.WriteLine("Press Enter to close.");
				Console.ReadLine();
			}
		}
	}

	public class OrleansStartup : IOrleansStartup
	{
		public IPublishSubscribe PublishSubscribe { get; set; }

		public OrleansStartup(string clusterName)
		{
			ILoggerFactory loggerFactory = new NullLoggerFactory();
			ISerializer<byte[]> serializer = new MessagePackSerializer();
			PublishSubscribe = new Proto.PubSub.PublishSubscribe(clusterName, serializer, loggerFactory);
		}

		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(PublishSubscribe);
			return new DefaultServiceProviderFactory()
				.CreateServiceProvider(services);
		}
	}
}