using System;
using System.Net;
using System.Reactive.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Proto;
using Proto.Cluster;

using vivego.core;
using vivego.Orleans.Providers;
using vivego.Proto.ClusterProvider;
using vivego.Proto.PubSub;
using vivego.Serializer.Abstractions;
using vivego.Serializer.Wire;

namespace vivego.Orleans.Playground
{
	public class OrleansStartup : IOrleansStartup
	{
		public IPublishSubscribe PublishSubscribe { get; set; }

		public OrleansStartup(string clusterName)
		{
			ILoggerFactory loggerFactory = new NullLoggerFactory();
			ISerializer<byte[]> serializer = new WireSerializer();
			PublishSubscribe = new Proto.PubSub.PublishSubscribe(clusterName, serializer, loggerFactory);

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