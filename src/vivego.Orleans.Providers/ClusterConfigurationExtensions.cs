using System;
using System.Collections.Generic;

using Orleans.Hosting;
using Orleans.Runtime.Configuration;

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

		public static IDisposable Run(this ISiloHost siloHost)
		{
			if (siloHost == null)
			{
				throw new ArgumentNullException(nameof(siloHost));
			}

			siloHost.StartAsync().Wait();
			return new AnonymousDisposable(() =>
			{
				if (!siloHost.Stopped.IsCompleted)
				{
					siloHost.StopAsync().Wait();
				}
			});
		}
	}
}