﻿using System;
using System.Linq;

namespace vivego.PublishSubscribe.Topic
{
	/// <summary>
	///     From https://github.com/xamarin/mqtt
	/// </summary>
	public class MqttTopicFilter : ITopicFilter
	{
		private readonly MqttTopicEvaluator _mqttTopicEvaluator;

		public MqttTopicFilter(Subscription subscription)
		{
			_mqttTopicEvaluator = new MqttTopicEvaluator(subscription);
		}

		public bool Matches(string topic)
		{
			return _mqttTopicEvaluator.Matches(topic);
		}
	}

	/// <summary>
	///     From https://github.com/xamarin/mqtt
	///     Represents an evaluator for MQTT topics
	///     according to the rules defined in the protocol specification
	/// </summary>
	/// See
	/// <a href="http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/mqtt-v3.1.1.html#_Toc442180919">Topic Names and Topic Filters</a>
	/// for more details on the topics specification
	public class MqttTopicEvaluator
	{
		/// <summary>
		///     Character that defines the single level topic wildcard, which is '+'
		/// </summary>
		public const string SingleLevelTopicWildcard = "+";

		/// <summary>
		///     Character that defines the multi level topic wildcard, which is '#'
		/// </summary>
		public const string MultiLevelTopicWildcard = "#";

		private readonly Subscription _subscription;

		/// <summary>
		///     Initializes a new instance of the <see cref="MqttTopicEvaluator" /> class,
		///     specifying the configuration to use
		/// </summary>
		public MqttTopicEvaluator(Subscription subscription)
		{
			_subscription = subscription;

			if (!IsValidTopicFilter(subscription.Topic))
			{
				throw new ArgumentException("");
			}
		}

		/// <summary>
		///     Determines if a topic filter is valid according to the protocol specification
		/// </summary>
		/// <param name="topicFilter">Topic filter to evaluate</param>
		/// <returns>A boolean value that indicates if the topic filter is valid or not</returns>
		public bool IsValidTopicFilter(string topicFilter)
		{
			if (topicFilter.Contains(SingleLevelTopicWildcard) ||
				topicFilter.Contains(MultiLevelTopicWildcard))
			{
				return false;
			}

			if (string.IsNullOrEmpty(topicFilter))
			{
				return false;
			}

			if (topicFilter.Length > 65536)
			{
				return false;
			}

			string[] topicFilterParts = topicFilter.Split('/');

			if (topicFilterParts.Count(s => s == "#") > 1)
			{
				return false;
			}

			if (topicFilterParts.Any(s => s.Length > 1 && s.Contains("#")))
			{
				return false;
			}

			if (topicFilterParts.Any(s => s.Length > 1 && s.Contains("+")))
			{
				return false;
			}

			if (topicFilterParts.Any(s => s == "#") &&
				topicFilter.IndexOf("#", StringComparison.Ordinal) < topicFilter.Length - 1)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		///     Determines if a topic name is valid according to the protocol specification
		/// </summary>
		/// <param name="topicName">Topic name to evaluate</param>
		/// <returns>A boolean value that indicates if the topic name is valid or not</returns>
		public bool IsValidTopicName(string topicName)
		{
			return !string.IsNullOrEmpty(topicName) &&
				topicName.Length <= 65536 &&
				!topicName.Contains("#") &&
				!topicName.Contains("+");
		}

		/// <summary>
		///     Evaluates if a topic name applies to a specific topic filter
		///     If a topic name matches a filter, it means that the Server will
		///     successfully dispatch incoming messages of that topic name
		///     to the subscribers of the topic filter
		/// </summary>
		/// <param name="topicName">Topic name to evaluate</param>
		/// <returns>A boolean value that indicates if the topic name matches with the topic filter</returns>
		public bool Matches(string topicName)
		{
			string topicFilter = _subscription.Topic;
			if (!IsValidTopicName(topicName))
			{
				string message = $"The topic name {topicName} is invalid according to the protocol rules";

				throw new ArgumentException(message);
			}

			string[] topicFilterParts = topicFilter.Split('/');
			string[] topicNameParts = topicName.Split('/');

			if (topicNameParts.Length > topicFilterParts.Length && topicFilterParts[topicFilterParts.Length - 1] != "#")
			{
				return false;
			}

			if (topicFilterParts.Length - topicNameParts.Length > 1)
			{
				return false;
			}

			if (topicFilterParts.Length - topicNameParts.Length == 1 && topicFilterParts[topicFilterParts.Length - 1] != "#")
			{
				return false;
			}

			if ((topicFilterParts[0] == "#" || topicFilterParts[0] == "+") && topicNameParts[0].StartsWith("$"))
			{
				return false;
			}

			bool matches = true;

			for (int i = 0; i < topicFilterParts.Length; i++)
			{
				string topicFilterPart = topicFilterParts[i];

				if (topicFilterPart == "#")
				{
					break;
				}

				if (topicFilterPart == "+")
				{
					if (i == topicFilterParts.Length - 1 && topicNameParts.Length > topicFilterParts.Length)
					{
						matches = false;
						break;
					}

					continue;
				}

				if (topicFilterPart != topicNameParts[i])
				{
					matches = false;
					break;
				}
			}

			return matches;
		}
	}
}