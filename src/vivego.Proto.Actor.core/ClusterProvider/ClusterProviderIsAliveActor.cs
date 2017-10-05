using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Proto;

using vivego.Proto.Messages;

namespace vivego.Proto.ClusterProvider
{
	internal class ClusterProviderIsAliveActor : IActor
	{
		private readonly IObserver<(Node Node, bool Alive)[]> _observer;

		private readonly IDictionary<PID, (Node Node, bool Alive)> _watchList =
			new Dictionary<PID, (Node Node, bool Alive)>();

		public ClusterProviderIsAliveActor(IObserver<(Node Node, bool Alive)[]> observer)
		{
			_observer = observer;
		}

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Node node:
					if (_watchList.TryGetValue(node.PID, out var tuple) && tuple.Alive)
					{
						return Task.CompletedTask;
					}

					_watchList[node.PID] = (node, true);
					PublishTopologyChange();

					// Tell everybody about the change
					foreach (KeyValuePair<PID, (Node Node, bool Alive)> keyValuePair in _watchList.Where(pair => pair.Value.Alive))
					{
						if (node.PID.Equals(keyValuePair.Key))
						{
							continue;
						}

						node.PID.Tell(keyValuePair.Value.Node);
					}

					context.Watch(node.PID);

					break;
				case Terminated terminated:
					if (terminated.AddressTerminated
						&& _watchList.TryGetValue(terminated.Who, out (Node Node, bool Alive) terminatedNode))
					{
						_watchList[terminated.Who] = (terminatedNode.Node, false);
						PublishTopologyChange();
					}

					break;
			}

			return Task.CompletedTask;
		}

		private void PublishTopologyChange()
		{
			_observer.OnNext(_watchList.Select(pair => pair.Value).ToArray());
		}
	}
}