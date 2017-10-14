using Proto;

using vivego.Proto.PubSub.Messages;

namespace vivego.Proto.PubSub.Route
{
	public interface IRouteSelector
	{
		PID Select(Message message, string group, PID[] pids);
	}
}