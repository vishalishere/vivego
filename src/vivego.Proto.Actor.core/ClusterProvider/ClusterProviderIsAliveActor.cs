using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Proto;

using vivego.Proto.Messages;

namespace vivego.Proto.ClusterProvider
{
	internal class ClusterProviderIsAliveActor : IActor
	{
		private readonly IObserver<IDictionary<string, Alive>> _observer;
		private readonly IDictionary<string, Alive> _watchList = new Dictionary<string, Alive>();

		public ClusterProviderIsAliveActor(IObserver<IDictionary<string, Alive>> observer)
		{
			_observer = observer;
		}

		public Task ReceiveAsync(IContext context)
		{
			switch (context.Message)
			{
				case Alive alive:
					string key = $"{alive.Host}:{alive.Port}";
					bool removed = _watchList.Remove(key);
					_watchList.Add(key, alive);
					if (!removed)
					{
						PID pid = new PID($"{alive.Host}:{alive.Port}", typeof(ClusterProviderIsAliveActor).FullName);
						context.Watch(pid);

						// Tell everybody else
						foreach (KeyValuePair<string, Alive> keyValuePair in _watchList)
						{
							pid.Tell(keyValuePair.Value);
						}
					}

					_observer.OnNext(_watchList);
					break;
				case Terminated terminated:
					_watchList.Remove(terminated.Who.Address);
					_observer.OnNext(_watchList);
					break;
			}

			return Task.CompletedTask;
		}
	}
}