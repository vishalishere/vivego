using System;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using vivego.core;
using vivego.PublishSubscribe.Topic;
using vivego.Serializer.Abstractions;

namespace vivego.PublishSubscribe
{
	public class PublishSubscribeBuilder
	{
		private int _port;

		public PublishSubscribeBuilder(ISerializer<byte[]> serializer)
		{
			Serializer = serializer;
			Port = 35000;
			//Factory(builder => new PublishSubscribe(ClusterName, Port, LoggerFactory, Serializer));
		}

		public bool AllowIncrementalPort { get; set; } = true;

		public int Port
		{
			get => AllowIncrementalPort ? PortUtils.FindAvailablePortIncrementally(_port) : _port;
			private set => _port = value;
		}

		public string ClusterName { get; private set; } = Environment.MachineName;
		public ILoggerFactory LoggerFactory { get; private set; } = new NullLoggerFactory();
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