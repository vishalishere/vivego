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
		private readonly IObserver<Alive[]> _observer;
		private readonly IDictionary<PID, Alive> _watchList = new Dictionary<PID, Alive>();

		public ClusterProviderIsAliveActor(IObserver<Alive[]> observer)
		{
			_observer = observer;
		}

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Alive alive:
					if (!_watchList.ContainsKey(alive.AlivePID))
					{
						_watchList.Add(alive.AlivePID, alive);
						_observer.OnNext(_watchList.Select(pair => pair.Value).ToArray());

						// Tell everybody about the change
						foreach (KeyValuePair<PID, Alive> keyValuePair in _watchList)
						{
							if (alive.AlivePID.Equals(keyValuePair.Key))
							{
								continue;
							}

							alive.AlivePID.Tell(keyValuePair.Value);
						}

						// Hack: Defer watch, because Terminated is sent incorrectly always when remote reconnecting
						PID self = context.Self;
						Task.Delay(100).ContinueWith(_ => self.Tell(alive.AlivePID));
					}

					break;
				case PID deferWatchPid:
					context.Watch(deferWatchPid);
					break;
				case Terminated terminated:
					if (terminated.AddressTerminated
						&& _watchList.Remove(terminated.Who))
					{
						_watchList.Remove(terminated.Who);
						_observer.OnNext(_watchList.Select(pair => pair.Value).ToArray());
					}

					break;
			}

			return Task.CompletedTask;
		}
	}
}