using System;
using System.Linq;
using System.Reflection;

namespace vivego.ProtoBroker
{
	public class SubscriptionInfo
	{
		private const char WildCardChar = '*';
		private const char FullWildCardChar = '>';
		private readonly Func<string, bool> _matchDelegate;

		public bool WildcardTopic { get; }

		public SubscriptionInfo(
			Type type,
			string topic)
		{
			Type = type;
			int wildCardPos = topic.IndexOf(WildCardChar);
			int fullWildCardPos = topic.IndexOf(FullWildCardChar);

			if (wildCardPos > -1 && fullWildCardPos > -1)
			{
				throw new ArgumentException("Subject can not contain both the wildcard and full wildcard character.",
					nameof(topic));
			}

			Topic = topic;

			bool matchAllSubjects = Topic[0] == FullWildCardChar;
			if (matchAllSubjects)
			{
				_matchDelegate = _ => true;
				return;
			}

			WildcardTopic = wildCardPos > -1 || fullWildCardPos > -1;
			if (!WildcardTopic)
			{
				_matchDelegate = t => Topic.Equals(t, StringComparison.Ordinal);
				return;
			}

			if (fullWildCardPos > -1)
			{
				string prefix = Topic.Substring(0, fullWildCardPos);
				_matchDelegate = t => t.StartsWith(prefix, StringComparison.Ordinal);
			}

			if (wildCardPos <= -1)
			{
				throw new Exception("Should not have reached this point.");
			}

			string[] subjectParts = wildCardPos > -1
				? topic.Split('.')
				: new string[0];

			_matchDelegate = t =>
			{
				string[] testParts = t.Split('.');
				if (testParts.Length != subjectParts.Length)
				{
					return false;
				}

				return !testParts
					.Where((s, i) => !s.Equals(subjectParts[i], StringComparison.Ordinal) && subjectParts[i] != "*")
					.Any();
			};
		}

		public Guid SubscriptionId { get; } = Guid.NewGuid();
		public Type Type { get; }
		public string Topic { get; }

		public bool Matches(string topic, object value)
		{
			return _matchDelegate(topic) && value.GetType().GetTypeInfo().IsAssignableFrom(Type);
		}
	}
}