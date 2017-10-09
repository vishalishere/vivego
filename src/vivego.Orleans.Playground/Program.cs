using System;
using System.ComponentModel.Design;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;

using vivego.Orleans.Providers;
using vivego.Proto.PubSub;
using vivego.Serializer.Abstractions;

namespace vivego.Orleans.Playground
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			ClusterConfiguration siloConfig = ClusterConfiguration
				.LocalhostPrimarySilo()
				.AddPublishSubscribeStreamProvider();
			siloConfig.UseStartupType<OrleansStartup>();
			using (SiloHost silo = new SiloHost("Test Silo", siloConfig))
			{
				silo.InitializeOrleansSilo();
				silo.StartOrleansSilo();

				Console.WriteLine("Press Enter to close.");

				// wait here
				Console.ReadLine();

				// shut the silo down after we are done.
				silo.ShutdownOrleansSilo();
			}
		}
	}

	public class OrleansStartup
	{
		public IServiceProvider ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<ILoggerFactory, LoggerFactory>();
			services.AddSingleton<ISerializer<byte[]>, MessagePackSerializer>();
			services.AddSingleton<IPublishSubscribe, PublishSubscribe>();
			return new DefaultServiceProviderFactory()
				.CreateServiceProvider(services);
		}
	}
}