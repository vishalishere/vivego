using System;

namespace vivego.PublishSubscribe.Topic
{
	internal class DefaultTopicFilter : ITopicFilter
	{
		private const char WildCardChar = '*';
		private const char FullWildCardChar = '>';
		private readonly Predicate<string> _matchPredicate;

		/// <summary>
		///     Creates a subscription info object.
		/// </summary>
		public DefaultTopicFilter(Subscription subscription)
		{
			if (subscription == null)
			{
				throw new ArgumentNullException(nameof(subscription));
			}

			if (string.IsNullOrEmpty(subscription.Topic))
			{
				throw new ArgumentException("Value cannot be null or empty.", nameof(subscription.Topic));
			}

			int wildCardPos = subscription.Topic.IndexOf(WildCardChar);
			int fullWildCardPos = subscription.Topic.IndexOf(FullWildCardChar);

			if (wildCardPos > -1 && fullWildCardPos > -1)
			{
				throw new ArgumentException("Subject can not contain both the wildcard and full wildcard character.",
					nameof(subscription.Topic));
			}

			bool hasWildcardSubject = wildCardPos > -1 || fullWildCardPos > -1;
			bool matchAllSubjects = subscription.Topic[0] == FullWildCardChar;
			string[] subjectParts = wildCardPos > -1
				? subscription.Topic.Split('.')
				: new string[0];

			if (matchAllSubjects)
			{
				_matchPredicate = _ => true;
			}
			else if (!hasWildcardSubject)
			{
				_matchPredicate = _ => subscription.Topic.Equals(_, StringComparison.Ordinal);
			}
			else if (fullWildCardPos > -1)
			{
				_matchPredicate = _ =>
				{
					string prefix = subscription.Topic.Substring(0, fullWildCardPos);
					return _.StartsWith(prefix, StringComparison.Ordinal);
				};
			}
			else if (wildCardPos > -1)
			{
				_matchPredicate = _ =>
				{
					string[] testParts = _.Split('.');
					if (testParts.Length != subjectParts.Length)
					{
						return false;
					}

					for (int i = 0; i < testParts.Length; i++)
					{
						if (!testParts[i].Equals(subjectParts[i], StringComparison.Ordinal) && subjectParts[i] != "*")
						{
							return false;
						}
					}

					return true;
				};
			}
			else
			{
				_matchPredicate = _ => true;
			}
		}

		public bool Matches(string topic)
		{
			return _matchPredicate(topic);
		}
	}
}