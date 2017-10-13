using System;
using System.Collections.Generic;

using Orleans.Runtime.Configuration;
using Orleans.Runtime.Host;

using vivego.core;
using vivego.Orleans.Providers.Stream;

namespace vivego.Orleans.Providers
{
	public static class ClusterConfigurationExtensions
	{
		public static ClusterConfiguration AddPublishSubscribeStreamProvider(this ClusterConfiguration clusterConfiguration,
			string providerName = "SMSProvider",
			int inMemoryCacheSize = PublishSubscribeStreamConfiguration.DefaultInMemoryCacheSizeParam,
			int numberOfQueues = PublishSubscribeStreamConfiguration.DefaultNumOfQueues)
		{
			if (clusterConfiguration == null)
			{
				throw new ArgumentNullException(nameof(clusterConfiguration));
			}

			if (string.IsNullOrWhiteSpace(providerName))
			{
				throw new ArgumentNullException(nameof(providerName));
			}

			clusterConfiguration.Globals.RegisterStreamProvider<PublishSubscribeStreamProvider>(providerName,
				new Dictionary<string, string>
				{
					{
						PublishSubscribeStreamConfiguration.InMemoryCacheSizeParam,
						inMemoryCacheSize.ToString()
					},
					{
						PublishSubscribeStreamConfiguration.NumberOfQueuesParam,
						numberOfQueues.ToString()
					}
				});

			return clusterConfiguration;
		}

		public static ClusterConfiguration UseOrleansStartup<T>(this ClusterConfiguration clusterConfiguration)
			where T : IOrleansStartup, new()
		{
			if (clusterConfiguration == null)
			{
				throw new ArgumentNullException(nameof(clusterConfiguration));
			}

			clusterConfiguration.UseStartupType<OrleansStartup>();
			OrleansStartup.Register<T>();
			return clusterConfiguration;
		}

		public static ClusterConfiguration UseOrleansStartup(this ClusterConfiguration clusterConfiguration, IOrleansStartup orleansStartup)
		{
			if (clusterConfiguration == null)
			{
				throw new ArgumentNullException(nameof(clusterConfiguration));
			}

			clusterConfiguration.UseStartupType<OrleansStartup>();
			OrleansStartup.Register(orleansStartup);
			return clusterConfiguration;
		}

		public static IDisposable Run(this ClusterConfiguration clusterConfiguration,
			string siloName = null)
		{
			if (clusterConfiguration == null)
			{
				throw new ArgumentNullException(nameof(clusterConfiguration));
			}

			SiloHost silo = new SiloHost(siloName ?? Guid.NewGuid().ToString(), clusterConfiguration);
			silo.InitializeOrleansSilo();
			silo.StartOrleansSilo();
			return new AnonymousDisposable(() =>
			{
				silo.StopOrleansSilo();
				silo.ShutdownOrleansSilo();
			});
		}
	}
}