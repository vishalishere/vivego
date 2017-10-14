using System.Collections.Generic;

using Proto;
using Proto.Cluster;

using vivego.Proto.PubSub.Messages;

namespace vivego.Proto.PubSub.Route
{
	public class RoundRobinRouteSelector : IRouteSelector
	{
		private readonly IDictionary<string, Counter> _counters = new Dictionary<string, Counter>();

		public virtual PID Select(Message message, string group, PID[] pids)
		{
			if (!_counters.TryGetValue(group, out Counter counter))
			{
				counter = new Counter();
				_counters.Add(group, counter);
			}

			int next = counter.Next();
			return pids[next % pids.Length];
		}
	}
}