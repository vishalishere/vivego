using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Proto;

using vivego.core;
using vivego.Proto.Messages;

namespace vivego.Proto.ClusterProvider
{
	internal class ClusterProviderIsAliveActor : DisposableBase, IActor
	{
		private readonly IObserver<(Node Node, bool Alive)[]> _observer;

		private readonly IDictionary<PID, (Node Node, bool Alive, DateTime LastUpdated)> _watchList =
			new Dictionary<PID, (Node Node, bool Alive, DateTime LastUpdated)>();

		public TimeSpan CutOff = TimeSpan.FromSeconds(30);
		public TimeSpan CutOffCheckInterval = TimeSpan.FromSeconds(10);

		public ClusterProviderIsAliveActor(IObserver<(Node Node, bool Alive)[]> observer)
		{
			_observer = observer;
		}

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Started _:
					Observable
						.Interval(CutOffCheckInterval)
						.Subscribe(_ => context.Self.Tell("Cleanup"), CancellationToken);
					break;
				case string s when s.Equals("Cleanup"):
				{
					var now = DateTime.UtcNow;
					foreach (KeyValuePair<PID, (Node Node, bool Alive, DateTime LastUpdated)> tuple in _watchList.ToArray())
					{
						if (tuple.Value.Alive)
						{
							continue;
						}

						TimeSpan sinceSinceUpdate = now - tuple.Value.LastUpdated;
						if (sinceSinceUpdate > CutOff)
						{
							_watchList.Remove(tuple.Key);
						}

						PublishTopologyChange();
					}

					break;
				}
				case Node node:
				{
					if (_watchList.TryGetValue(node.PID, out (Node Node, bool Alive, DateTime LastUpdated) tuple) && tuple.Alive)
					{
						return Task.CompletedTask;
					}

					_watchList[node.PID] = (node, true, DateTime.UtcNow);
					PublishTopologyChange();

					// Tell everybody about the change
					foreach (KeyValuePair<PID, (Node Node, bool Alive, DateTime LastUpdated)> keyValuePair in _watchList.Where(pair =>
						pair.Value.Alive))
					{
						if (node.PID.Equals(keyValuePair.Key))
						{
							continue;
						}

						node.PID.Tell(keyValuePair.Value.Node);
					}

					context.Watch(node.PID);

					break;
				}
				case Terminated terminated:
					if (terminated.AddressTerminated
						&& _watchList.TryGetValue(terminated.Who, out (Node Node, bool Alive, DateTime LastUpdated) terminatedNode))
					{
						_watchList[terminated.Who] = (terminatedNode.Node, false, DateTime.UtcNow);
						PublishTopologyChange();
					}

					break;
			}

			return Task.CompletedTask;
		}

		private void PublishTopologyChange()
		{
			_observer.OnNext(_watchList.Select(pair => (pair.Value.Node, pair.Value.Alive)).ToArray());
		}
	}
}