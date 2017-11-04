using System;

using Microsoft.Extensions.Logging;

using vivego.PublishSubscribe.Topic;
using vivego.Serializer.Abstractions;

namespace vivego.PublishSubscribe
{
	public class PublishSubscribeBuilder
	{
		public PublishSubscribeBuilder()
		{
			Factory(builder => new PublishSubscribe(ClusterName, Port, LoggerFactory, Serializer));
		}

		public int Port { get; private set; } = 35000;
		public string ClusterName { get; private set; } = Environment.MachineName;
		public ILoggerFactory LoggerFactory { get; private set; }
		public Func<PublishSubscribeBuilder, IPublishSubscribe> PublishSubscribeFactory { get; private set; }
		public ISerializer<byte[]> Serializer { get; private set; }
		public Func<Subscription, ITopicFilter> TopicFilterFactory { get; private set; } = _ => new DefaultTopicFilter(_);

		public PublishSubscribeBuilder SetTopicFilterFactory(Func<Subscription, ITopicFilter> factory)
		{
			TopicFilterFactory = factory;
			return this;
		}

		public PublishSubscribeBuilder SetMqttTopicFilterFactory()
		{
			TopicFilterFactory = _ => new MqttTopicFilter(_);
			return this;
		}

		public PublishSubscribeBuilder SetLambdaTopicFilterFactory(Predicate<string> predicate)
		{
			TopicFilterFactory = _ => new LambdaTopicfilter(predicate);
			return this;
		}

		public PublishSubscribeBuilder SetSerializer(ISerializer<byte[]> serializer)
		{
			Serializer = serializer;
			return this;
		}

		public PublishSubscribeBuilder SetPort(int port)
		{
			Port = port;
			return this;
		}

		public PublishSubscribeBuilder SetLoggerFactory(ILoggerFactory loggerFactory)
		{
			LoggerFactory = loggerFactory;
			return this;
		}

		public PublishSubscribeBuilder SetClusterName(string clusterName)
		{
			ClusterName = clusterName;
			return this;
		}

		public PublishSubscribeBuilder Factory(Func<PublishSubscribeBuilder, IPublishSubscribe> func)
		{
			PublishSubscribeFactory = func ?? throw new ArgumentNullException(nameof(func));
			return this;
		}

		public IPublishSubscribe Build()
		{
			return PublishSubscribeFactory(this);
		}
	}
}