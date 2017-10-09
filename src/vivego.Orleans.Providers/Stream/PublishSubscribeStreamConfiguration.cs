using System;

using Orleans.Providers;

using vivego.Orleans.Providers.Common;

namespace vivego.Orleans.Providers.Stream
{
	public class PublishSubscribeStreamConfiguration
	{
		// Names
		private const string InMemoryCacheSizeParam = "InMemoryCacheSize";

		private const string NumberOfQueuesParam = "NumberOfQueues";

		// Default values
		private const int DefaultInMemoryCacheSizeParam = 1024;

		private const int DefaultNumOfQueues = 6;

		public PublishSubscribeStreamConfiguration(IProviderConfiguration config)
		{
			Config = config;

			InMemoryCacheSize = config?.GetOptionalParamInt(InMemoryCacheSizeParam, DefaultInMemoryCacheSizeParam) ??
				throw new ArgumentNullException(nameof(config));
			NumberOfQueues = config.GetOptionalParamInt(NumberOfQueuesParam, DefaultNumOfQueues);
		}

		public IProviderConfiguration Config { get; }
		public int NumberOfQueues { get; set; }
		public int InMemoryCacheSize { get; set; }
	}
}