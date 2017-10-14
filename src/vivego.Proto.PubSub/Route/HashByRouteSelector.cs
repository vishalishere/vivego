using Proto;
using Proto.Router;

using vivego.Proto.PubSub.Messages;

namespace vivego.Proto.PubSub.Route
{
	public class HashByRouteSelector : RoundRobinRouteSelector
	{
		public override PID Select(Message message, string group, PID[] pids)
		{
			if (string.IsNullOrEmpty(message.HashBy))
			{
				return base.Select(message, group, pids);
			}

			uint next = MD5Hasher.Hash(message.HashBy);
			return pids[next % pids.Length];
		}
	}
}