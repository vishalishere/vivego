using System;

using Orleans.Runtime.Configuration;

using vivego.Orleans.Providers.Stream;

namespace vivego.Orleans.Providers
{
	public static class ClusterConfigurationExtensions
	{
		public static ClusterConfiguration AddPublishSubscribeStreamProvider(this ClusterConfiguration clusterConfiguration,
			string providerName = "SMSProvider")
		{
			if (clusterConfiguration == null)
			{
				throw new ArgumentNullException(nameof(clusterConfiguration));
			}

			if (string.IsNullOrWhiteSpace(providerName))
			{
				throw new ArgumentNullException(nameof(providerName));
			}

			clusterConfiguration.Globals.RegisterStreamProvider<PublishSubscribeStreamProvider>(providerName);

			return clusterConfiguration;
		}
	}
}