using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;

namespace vivego.Discovery.Abstactions
{
    public class DnsDiscoverClientFactory : IDiscoverClientFactory
	{
		private readonly string _hostName;
		private readonly TimeSpan _updateInterval;
		private readonly int _port;

		public DnsDiscoverClientFactory(string hostName,
			TimeSpan updateInterval,
			int port)
		{
			if (string.IsNullOrEmpty(hostName))
			{
				throw new ArgumentException("message", nameof(hostName));
			}

			if (port <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(port));
			}

			_hostName = hostName;
			_updateInterval = updateInterval;
			_port = port;
		}

		public IObservable<IPEndPoint[]> MakeObservable()
		{
			return Observable
				.Interval(_updateInterval)
				.Select(_ =>
				{
					return System.Net.Dns.GetHostAddresses(_hostName)
						.Select(ipaddress => new IPEndPoint(ipaddress, _port))
						.ToArray();
				})
				.DistinctUntilChanged();
		}
	}
}
