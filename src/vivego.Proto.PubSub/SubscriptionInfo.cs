using System;

using vivego.Proto.PubSub.Messages;

namespace vivego.Proto.PubSub
{
	internal class SubscriptionInfo
	{
		private const char WildCardChar = '*';
		private const char FullWildCardChar = '>';
		private readonly Predicate<string> _matchPredicate;

		/// <summary>
		///     Creates a subscription info object.
		/// </summary>
		public SubscriptionInfo(Subscription subscription)
		{
			Subscription = subscription;
			int wildCardPos = subscription.Topic.IndexOf(WildCardChar);
			int fullWildCardPos = subscription.Topic.IndexOf(FullWildCardChar);

			if (wildCardPos > -1 && fullWildCardPos > -1)
			{
				throw new ArgumentException("Subject can not contain both the wildcard and full wildcard character.",
					nameof(subscription.Topic));
			}

			HasWildcardSubject = wildCardPos > -1 || fullWildCardPos > -1;
			bool matchAllSubjects = Topic[0] == FullWildCardChar;
			string[] subjectParts = wildCardPos > -1
				? Subscription.Topic.Split('.')
				: new string[0];

			if (matchAllSubjects)
			{
				_matchPredicate = _ => true;
			}
			else if (!HasWildcardSubject)
			{
				_matchPredicate = _ => Topic.Equals(_, StringComparison.Ordinal);
			}
			else if (fullWildCardPos > -1)
			{
				_matchPredicate = _ =>
				{
					string prefix = Topic.Substring(0, fullWildCardPos);
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

		public Subscription Subscription { get; }

		/// <summary>
		///     Gets the subject name the subscriber is subscribed to.
		/// </summary>
		public string Topic => Subscription.Topic;

		/// <summary>
		///     Gets the optionally specified queue group that the subscriber will join.
		/// </summary>
		public string Group => Subscription.Group;

		public bool HasWildcardSubject { get; }

		public bool Matches(string testSubject)
		{
			return _matchPredicate(testSubject);
		}
	}
}