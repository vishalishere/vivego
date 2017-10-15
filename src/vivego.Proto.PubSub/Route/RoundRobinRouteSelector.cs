using System.Collections.Generic;

using Proto;
using Proto.Cluster;

using vivego.core;
using vivego.Proto.PubSub.Messages;

namespace vivego.Proto.PubSub.Route
{
	public class RoundRobinRouteSelector : IRouteSelector
	{
		private readonly IDictionary<string, Counter> _counters = new Dictionary<string, Counter>();

		public virtual IEnumerable<PID> Select(Message message, string group, PID[] pids)
		{
			if (string.IsNullOrEmpty(group))
			{
				return pids;
			}

			if (!_counters.TryGetValue(group, out Counter counter))
			{
				counter = new Counter();
				_counters.Add(group, counter);
			}

			if (pids.Length == 1)
			{
				return pids[0].AsEnumerable();
			}

			int next = counter.Next();
			return pids[next % pids.Length].AsEnumerable();
		}
	}
}