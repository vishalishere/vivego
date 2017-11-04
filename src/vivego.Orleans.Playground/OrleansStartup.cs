using System;
using System.Net;
using System.Reactive.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Proto;
using Proto.Cluster;

using vivego.core;
using vivego.Proto.ClusterProvider;
using vivego.PublishSubscribe;
using vivego.Serializer.Abstractions;
using vivego.Serializer.MessagePack;

namespace vivego.Orleans.Playground
{
	public class OrleansStartup
	{
		public IPublishSubscribe PublishSubscribe { get; set; }

		public OrleansStartup(string clusterName)
		{
			ILoggerFactory loggerFactory = new NullLoggerFactory();
			ISerializer<byte[]> serializer = new MessagePackSerializer();
			PublishSubscribe = new PublishSubscribeBuilder()
				.SetLoggerFactory(loggerFactory)
				.SetSerializer(serializer)
				.Build();

			// Start ProtoActor Cluster
			int serverPort = PortUtils.FindAvailablePortIncrementally(41000);
			Cluster.Start("unique", "127.0.0.1", serverPort,
				new SeededLocalClusterProvider(Observable.Return(new[] { new IPEndPoint(IPAddress.Loopback, 41000) })));
		}

		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton(PublishSubscribe);
			services.AddTransient<ITestGrain, TestGrain>();
			return new DefaultServiceProviderFactory()
				.CreateServiceProvider(services);
		}
	}
}