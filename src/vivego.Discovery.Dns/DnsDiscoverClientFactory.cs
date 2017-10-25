using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;

using DnsClient;

using vivego.Discovery.Abstactions;

namespace vivego.Discovery.Dns
{
	/// <summary>
	///     static	Autocluster by static node list
	///     mcast Autocluster by UDP Multicast
	///     dns Autocluster by DNS A Record
	///     etcd    Autocluster using etcd
	///     k8s Autocluster on Kubernetes
	/// </summary>
	public class DnsDiscoverClientFactory : IDiscoverClientFactory
	{
		private readonly string _hostName;
		private readonly int _port;
		private readonly TimeSpan _updateInterval;
		private readonly LookupClient _lookup;

		public DnsDiscoverClientFactory(string hostName,
			TimeSpan updateInterval,
			int port,
			params IPEndPoint[] dnsServers)
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

			if (dnsServers == null || dnsServers.Length == 0)
			{
				_lookup = new LookupClient
				{
					EnableAuditTrail = false,
					UseCache = false,
					ThrowDnsErrors = false
				};
			}
			else
			{
				_lookup = new LookupClient(dnsServers)
				{
					EnableAuditTrail = false,
					UseCache = false,
					ThrowDnsErrors = false
				};
			}
		}

		public IObservable<IPEndPoint[]> MakeObservable()
		{
			return Observable
				.Interval(_updateInterval)
				.Select(_ =>
				{
					IDnsQueryResponse queryResult = _lookup.Query(_hostName, QueryType.A);
					return queryResult.Answers
						.ARecords()
						.Select(aRecord => aRecord.Address)
						.Select(ipaddress => new IPEndPoint(ipaddress, _port))
						.ToArray();
				})
				.DistinctUntilChanged();
		}
	}
}