using System.Collections.Generic;
using System.Linq;

using vivego.PublishSubscribe;

using WampSharp.Core.Serialization;
using WampSharp.V2.Core.Contracts;
using WampSharp.V2.PubSub;

namespace vivego.WampSharp.PublishSubscribe
{
	public class PubSubForwarderWampRawTopicRouterSubscriber : IWampRawTopicRouterSubscriber
	{
		private readonly string _forwarderPubSubTopic;
		private readonly IPublishSubscribe _publishSubscribe;
		private readonly AtomicBoolean _disableInternalPublishAtomicBoolean;
		private readonly string _publisherId;
		private readonly IWampTopic _wampTopic;

		public PubSubForwarderWampRawTopicRouterSubscriber(
			string publisherId,
			string forwarderPubSubTopic,
			IWampTopic wampTopic,
			IPublishSubscribe publishSubscribe,
			AtomicBoolean disableInternalPublishAtomicBoolean)
		{
			_publisherId = publisherId;
			_wampTopic = wampTopic;
			_publishSubscribe = publishSubscribe;
			_disableInternalPublishAtomicBoolean = disableInternalPublishAtomicBoolean;
			_forwarderPubSubTopic = forwarderPubSubTopic;
		}

		public void Event<TMessage>(IWampFormatter<TMessage> formatter, long publicationId, PublishOptions options)
		{
			if (_disableInternalPublishAtomicBoolean.Value)
			{
				return;
			}

			_publishSubscribe.Publish(_forwarderPubSubTopic, new ForwardedWampMessage
			{
				PublisherId = _publisherId,
				WampTopic = _wampTopic.TopicUri
			});
		}

		public void Event<TMessage>(IWampFormatter<TMessage> formatter, 
			long publicationId, 
			PublishOptions options,
			TMessage[] arguments)
		{
			if (_disableInternalPublishAtomicBoolean.Value)
			{
				return;
			}

			_publishSubscribe.Publish(_forwarderPubSubTopic, new ForwardedWampMessage
			{
				PublisherId = _publisherId,
				WampTopic = _wampTopic.TopicUri,
				Arguments = arguments.Cast<object>().ToArray()
			});
		}

		public void Event<TMessage>(IWampFormatter<TMessage> formatter, long publicationId, PublishOptions options,
			TMessage[] arguments,
			IDictionary<string, TMessage> argumentsKeywords)
		{
			if (_disableInternalPublishAtomicBoolean.Value)
			{
				return;
			}

			_publishSubscribe.Publish(_forwarderPubSubTopic, new ForwardedWampMessage
			{
				PublisherId = _publisherId,
				WampTopic = _wampTopic.TopicUri,
				Arguments = arguments.Cast<object>().ToArray(),
				ArgumentsKeywords = argumentsKeywords.ToDictionary(pair => pair.Key, pair => (object) pair.Value)
			});
		}
	}
}