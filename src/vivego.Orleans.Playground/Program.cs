using System;

using Orleans;
using Orleans.Hosting;
using Orleans.Runtime.Configuration;

using vivego.core;
using vivego.Orleans.Providers;

namespace vivego.Orleans.Playground
{
	internal class Program
	{
		private static void Main()
		{
			OrleansStartup orleansStartup = new OrleansStartup("clusterName");
			ClusterConfiguration siloConfig = ClusterConfiguration
				.LocalhostPrimarySilo(PortUtils.FindAvailablePortIncrementally(22222), PortUtils.FindAvailablePortIncrementally(40000))
				.AddPublishSubscribeStreamProvider();
			siloConfig.AddMemoryStorageProvider("PubSubStore");
			ISiloHost siloHost = new SiloHostBuilder()
				.AddApplicationPart(typeof(TestGrain).Assembly)
				.UseConfiguration(siloConfig)
				.UseServiceProviderFactory(collection => orleansStartup.ConfigureServices(collection))
				.Build();

			ClientConfiguration clientConfiguration = ClientConfiguration
				.LocalhostSilo()
				.AddPublishSubscribeStreamProvider()
				.ConnectTo(siloConfig);
			IClientBuilder clientBuilder = new ClientBuilder()
				.AddApplicationPart(typeof(TestGrain).Assembly)
				.UseConfiguration(clientConfiguration)
				.UseServiceProviderFactory(services => orleansStartup.ConfigureServices(services));

			using (orleansStartup.PublishSubscribe
				.Observe<object>("*")
				.Subscribe(_ => { Console.Out.WriteLine(_); }))
			{
				using (siloHost.Run())
				using (IClusterClientEx clusterClient = clientBuilder.Run())
				{
					clusterClient.GetGrain<ITestGrain>(Guid.NewGuid()).Run().Wait();
					//orleansStartup.PublishSubscribe.Publish("from Console", "Hello From Console");
					Console.WriteLine("Press Enter to close.");
					Console.ReadLine();
				}
			}
		}
	}
}