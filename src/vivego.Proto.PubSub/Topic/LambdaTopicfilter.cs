using System;

namespace vivego.Proto.PubSub.Topic
{
	public class LambdaTopicfilter : ITopicFilter
	{
		private readonly Predicate<string> _predicate;

		public LambdaTopicfilter(Predicate<string> predicate)
		{
			_predicate = predicate;
		}

		public bool Matches(string topic)
		{
			return _predicate(topic);
		}
	}
}