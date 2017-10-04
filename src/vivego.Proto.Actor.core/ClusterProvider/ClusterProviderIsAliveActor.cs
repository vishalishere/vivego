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

		private readonly IDictionary<PID, (Alive Alive, DateTime Lastseen)> _watchList =
			new Dictionary<PID, (Alive Alive, DateTime Lastseen)>();

		public ClusterProviderIsAliveActor(IObserver<Alive[]> observer)
		{
			_observer = observer;
		}

		public TimeSpan MaxAllowedNotAlive { get; set; } = TimeSpan.FromHours(1);

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Alive alive:
					bool exists = _watchList.ContainsKey(alive.AlivePID);
					_watchList[alive.AlivePID] = (alive, DateTime.UtcNow);
					if (!exists)
					{
						PublishTopologyChange();

						// Tell everybody about the change
						foreach (KeyValuePair<PID, (Alive Alive, DateTime Lastseen)> keyValuePair in _watchList)
						{
							if (alive.AlivePID.Equals(keyValuePair.Key))
							{
								continue;
							}

							alive.AlivePID.Tell(keyValuePair.Value.Alive);
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
						PublishTopologyChange();
					}

					break;
			}

			return Task.CompletedTask;
		}

		private void PublishTopologyChange()
		{
			DateTime now = DateTime.UtcNow;
			foreach (KeyValuePair<PID, (Alive Alive, DateTime Lastseen)> pair in _watchList.ToArray())
			{
				if (now - pair.Value.Lastseen > MaxAllowedNotAlive)
				{
					_watchList.Remove(pair);
				}
			}

			_observer.OnNext(_watchList.Select(pair => pair.Value.Alive).ToArray());
		}
	}
}