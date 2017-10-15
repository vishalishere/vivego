using System.Collections.Generic;

using Proto;
using Proto.Router;

using vivego.core;
using vivego.Proto.PubSub.Messages;

namespace vivego.Proto.PubSub.Route
{
	public class HashByRouteSelector : RoundRobinRouteSelector
	{
		public override IEnumerable<PID> Select(Message message, string group, PID[] pids)
		{
			if (string.IsNullOrEmpty(message.HashBy))
			{
				return base.Select(message, group, pids);
			}

			if (pids.Length == 1)
			{
				return pids[0].AsEnumerable();
			}

			uint next = MD5Hasher.Hash(message.HashBy);
			return pids[next % pids.Length].AsEnumerable();
		}
	}
}