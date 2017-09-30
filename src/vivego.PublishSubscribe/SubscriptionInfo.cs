using System;

namespace vivego.PublishSubscribe
{
	public class SubscriptionInfo
	{
		private const char WildCardChar = '*';
		private const char FullWildCardChar = '>';

		private readonly Predicate<string> _matchPredicate;

		/// <summary>
		///     Creates a subscription info object.
		/// </summary>
		/// <param name="subject">The subject name to subscribe to</param>
		/// <param name="queueGroup">If specified, the subscriber will join this queue group</param>
		/// <param name="maxMessages">Number of messages to wait for before automatically unsubscribing</param>
		public SubscriptionInfo(string subject, string queueGroup = null, int? maxMessages = null)
		{
			int wildCardPos = subject.IndexOf(WildCardChar);
			int fullWildCardPos = subject.IndexOf(FullWildCardChar);

			if (wildCardPos > -1 && fullWildCardPos > -1)
			{
				throw new ArgumentException("Subject can not contain both the wildcard and full wildcard character.",
					nameof(subject));
			}

			Id = Guid.NewGuid().ToString("N");
			Subject = subject;
			QueueGroup = queueGroup;
			MaxMessages = maxMessages;

			HasWildcardSubject = wildCardPos > -1 || fullWildCardPos > -1;
			bool matchAllSubjects = Subject[0] == FullWildCardChar;
			string[] subjectParts = wildCardPos > -1
				? subject.Split('.')
				: new string[0];

			if (matchAllSubjects)
			{
				_matchPredicate = _ => true;
			}
			else if (!HasWildcardSubject)
			{
				_matchPredicate = _ => Subject.Equals(_, StringComparison.Ordinal);
			}
			else if (fullWildCardPos > -1)
			{
				_matchPredicate = _ =>
				{
					string prefix = Subject.Substring(0, fullWildCardPos);
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

		public string Id { get; }

		/// <summary>
		///     Gets the subject name the subscriber is subscribed to.
		/// </summary>
		public string Subject { get; }

		/// <summary>
		///     Gets the optionally specified queue group that the subscriber will join.
		/// </summary>
		public string QueueGroup { get; }

		/// <summary>
		///     Gets the number of messages the subscriber will wait before automatically unsubscribing.
		/// </summary>
		public int? MaxMessages { get; }

		public bool HasWildcardSubject { get; }

		public bool Matches(string testSubject)
		{
			return _matchPredicate(testSubject);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as SubscriptionInfo);
		}

		protected bool Equals(SubscriptionInfo other) => string.Equals(Id, other?.Id);

		public override int GetHashCode() => Id.GetHashCode();
	}
}