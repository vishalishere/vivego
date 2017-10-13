using System;

using Orleans.Providers;

using vivego.Orleans.Providers.Common;

namespace vivego.Orleans.Providers.Stream
{
	public class PublishSubscribeStreamConfiguration
	{
		// Names
		public const string InMemoryCacheSizeParam = "InMemoryCacheSize";

		public const string NumberOfQueuesParam = "NumberOfQueues";

		// Default values
		public const int DefaultInMemoryCacheSizeParam = 1024;

		public const int DefaultNumOfQueues = 6;

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